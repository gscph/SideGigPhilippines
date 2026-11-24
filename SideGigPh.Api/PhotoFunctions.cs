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
/// Work photos and profile photos, stored in Azure Blob Storage as photos/{id}.jpg. Every upload is a new blob with a
/// random id (64 hex characters), so the same picture sent twice, or by two accounts, is simply two photos.
/// <c>POST photos</c> (work photos, post photos, a household's photo) re-encodes every upload (<see cref="PhotoImage"/>);
/// <c>POST photos/selfie</c> (a worker's profile photo, made small on the phone) is only checked (<see cref="SelfieJpeg"/>).
/// Anyone can view a photo by its URL (the id is unguessable); only the account that uploaded it deletes it.
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

        // Each upload is a new blob, so uploads are limited per account.
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

        // iPhone HEIC and PNG become JPEG. A photo bigger than any phone camera takes is refused from its header, before
        // its pixels are decoded.
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

        return await StoreAsync(bytes, uid, cancellationToken);
    }

    /// <summary>
    /// A worker's selfie, their profile photo. The phone already made it small (<see cref="SelfieJpeg"/>), so it is
    /// checked and stored as sent, with its metadata taken out, never resized or re-encoded. Refused with
    /// <see cref="ApiErrors.SelfieNotJpeg"/> (415), <see cref="ApiErrors.SelfieTooBig"/> (413) or
    /// <see cref="ApiErrors.SelfieBroken"/> (400), which the app shows as a photo it can't use.
    /// </summary>
    [Function("UploadSelfie")]
    public async Task<IActionResult> UploadSelfie(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "photos/selfie")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var uid = await auth.GetUidAsync(request, cancellationToken);
        if (uid is null)
        {
            return new UnauthorizedResult();
        }

        if (!limits.TryTake(Limited.PhotoUpload, uid))
        {
            return ApiResults.TooMany();
        }

        var bytes = await ReadAtMostAsync(request.Body, SelfieJpeg.MaxBytes + 1, cancellationToken);
        var check = SelfieJpeg.Check(bytes);
        return check.Verdict switch
        {
            SelfieVerdict.Ok => await StoreAsync(check.Jpeg!, uid, cancellationToken),
            SelfieVerdict.NotJpeg => Refused(StatusCodes.Status415UnsupportedMediaType, ApiErrors.SelfieNotJpeg),
            SelfieVerdict.TooBig => Refused(StatusCodes.Status413PayloadTooLarge, ApiErrors.SelfieTooBig),
            _ => Refused(StatusCodes.Status400BadRequest, ApiErrors.SelfieBroken),
        };
    }

    private static ObjectResult Refused(int status, string error) => new(new ApiError(error)) { StatusCode = status };

    /// <summary>Stores the finished JPEG as a new blob with a random id, owned by <paramref name="uid"/>.</summary>
    private async Task<IActionResult> StoreAsync(byte[] bytes, string uid, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var id = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var blob = photos.GetBlobClient($"{id}.jpg");
        await blob.UploadAsync(BinaryData.FromBytes(bytes), new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "image/jpeg",
                // An id is never reused, so phones can cache it for good.
                CacheControl = "public, max-age=31536000, immutable",
            },
            Metadata = new Dictionary<string, string> { [OwnerKey] = uid },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
        }, cancellationToken);

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

    /// <summary>Deletes the stored photos among <paramref name="ids"/> that <paramref name="uid"/> uploaded (Delete account).</summary>
    internal static async Task DeleteOwnedAsync(BlobContainerClient photos, string uid, IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids.Where(id => PhotoId().IsMatch(id)))
        {
            var blob = photos.GetBlobClient($"{id}.jpg");
            if (await OwnerOfAsync(blob, cancellationToken) == uid)
            {
                await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
        }
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

    /// <summary>The body, up to <paramref name="limit"/> bytes: a caller can't make the server buffer more.</summary>
    internal static async Task<byte[]> ReadAtMostAsync(Stream body, int limit, CancellationToken cancellationToken)
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
