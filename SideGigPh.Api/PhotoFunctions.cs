using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Work photos and profile photos, stored in Azure Blob Storage as photos/{sha256}.jpg.
/// Anyone can view a photo by its URL (the id is an unguessable hash); only its owner adds or deletes it.
///
/// Anti-fraud rule 2: the server hashes the bytes itself and keeps one blob per hash, owned by the
/// account that uploaded it first. Another account uploading the same photo gets 409 photo-in-use.
/// </summary>
public sealed partial class PhotoFunctions(BlobContainerClient photos, FirebaseTokenValidator auth, CallerLimits limits)
{
    public const string Container = "photos";

    // What may be sent: an iPhone HEIC or a PNG screenshot can be several MB before it's converted.
    private const int MaxBytes = 10 * 1024 * 1024;
    private const string OwnerKey = "owner";

    // Set once the container is known to exist, so later uploads in this process skip the check.
    private static volatile bool containerReady;

    [Function("UploadPhoto")]
    public async Task<IActionResult> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "photos")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var uid = await auth.GetUidAsync(request, cancellationToken);
        if (uid is null)
        {
            return new UnauthorizedResult();
        }

        // Each distinct photo is a new blob, so uploads are limited per account.
        if (!limits.TryTake(Limited.PhotoUpload, uid))
        {
            return ApiResults.TooMany();
        }

        var bytes = await ReadAtMostAsync(request.Body, MaxBytes + 1, cancellationToken);
        if (bytes.Length > MaxBytes)
        {
            return new ObjectResult(new { error = "too-large" }) { StatusCode = StatusCodes.Status413PayloadTooLarge };
        }

        if (PhotoImage.Detect(bytes) is not { } kind)
        {
            return new BadRequestObjectResult(new { error = "unsupported-format" });
        }

        // iPhone HEIC and PNG become JPEG; the id is the hash of what is stored. A photo bigger than any phone camera
        // takes is refused from its header, before its pixels are decoded.
        try
        {
            if (PhotoImage.IsTooBig(bytes, kind))
            {
                return new ObjectResult(new { error = "too-large" }) { StatusCode = StatusCodes.Status413PayloadTooLarge };
            }

            bytes = PhotoImage.ToStoredJpeg(bytes, kind);
        }
        catch (MagickException)
        {
            return new BadRequestObjectResult(new { error = "broken-image" });
        }

        await EnsureContainerAsync(cancellationToken);

        var id = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var blob = photos.GetBlobClient($"{id}.jpg");
        try
        {
            await blob.UploadAsync(BinaryData.FromBytes(bytes), new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/jpeg",
                    // Same id = same bytes forever, so phones can cache it for good.
                    CacheControl = "public, max-age=31536000, immutable",
                },
                Metadata = new Dictionary<string, string> { [OwnerKey] = uid },
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
            }, cancellationToken);
        }
        catch (RequestFailedException e) when (e.Status == StatusCodes.Status409Conflict)
        {
            // Already stored: fine if it's this account's own photo (saving the same bytes again).
            if (await OwnerOfAsync(blob, cancellationToken) != uid)
            {
                return new ConflictObjectResult(new { error = "photo-in-use" });
            }
        }

        return new OkObjectResult(new PhotoDto(id, blob.Uri.ToString()));
    }

    [Function("DeletePhoto")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "photos/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        var uid = await auth.GetUidAsync(request, cancellationToken);
        if (uid is null)
        {
            return new UnauthorizedResult();
        }

        if (!PhotoId().IsMatch(id))
        {
            return new BadRequestObjectResult(new { error = "bad-id" });
        }

        var blob = photos.GetBlobClient($"{id}.jpg");
        var owner = await OwnerOfAsync(blob, cancellationToken);
        if (owner is null)
        {
            return new NoContentResult();
        }

        if (owner != uid)
        {
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        return new NoContentResult();
    }

    /// <summary>The uid that uploaded the photo, or null when there's no such photo.</summary>
    private static async Task<string?> OwnerOfAsync(BlobClient blob, CancellationToken cancellationToken)
    {
        try
        {
            var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value.Metadata.TryGetValue(OwnerKey, out var owner) ? owner : string.Empty;
        }
        catch (RequestFailedException e) when (e.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Makes sure the photos container exists and is readable by URL (blob-level public read, not listable),
    /// the first time in this process. A container that exists but is private (made by hand, or reset by a
    /// policy) is switched to public read here, because the app shows photos straight from their URLs.
    /// </summary>
    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (containerReady)
        {
            return;
        }

        try
        {
            if (!await photos.ExistsAsync(cancellationToken))
            {
                await photos.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            }
            else if ((await photos.GetAccessPolicyAsync(cancellationToken: cancellationToken)).Value.BlobPublicAccess != PublicAccessType.Blob)
            {
                await photos.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            }
        }
        catch (RequestFailedException e) when (e.ErrorCode == "PublicAccessNotPermitted")
        {
            throw new InvalidOperationException(
                $"Can't make the '{Container}' container readable by URL: turn on 'Allow Blob anonymous access' " +
                "(storage account > Settings > Configuration), or run azure/cloudshell-setup.sh. If it turns itself " +
                "off again, an Azure Policy on the subscription is blocking public blobs.", e);
        }

        containerReady = true;
    }

    private static async Task<byte[]> ReadAtMostAsync(Stream body, int limit, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while (buffer.Length < limit
               && (read = await body.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - buffer.Length)), cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex PhotoId();
}
