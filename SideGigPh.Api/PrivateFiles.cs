using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SideGigPh.Api;

/// <summary>
/// Files nobody can read by URL: the ID photos, certificates and proofs workers send for verification. Only the API
/// reads them, for an operator, and each read is logged (<c>VerificationStore.ViewDocumentAsync</c>).
/// </summary>
public interface IPrivateFiles
{
    Task SaveAsync(string name, byte[] jpeg, CancellationToken cancellationToken);

    /// <summary>The file's bytes, or null when there's no such file.</summary>
    Task<byte[]?> ReadAsync(string name, CancellationToken cancellationToken);

    Task DeleteAsync(string name, CancellationToken cancellationToken);
}

/// <summary>
/// The private <c>verification</c> container in the photo storage account (made by
/// <c>azure/cloudshell-document-intelligence.sh</c>, or here on the first upload). Azure Storage encrypts it at rest.
/// </summary>
public sealed class BlobPrivateFiles(BlobContainerClient container) : IPrivateFiles
{
    public const string Container = "verification";

    private volatile bool _ready;

    public async Task SaveAsync(string name, byte[] jpeg, CancellationToken cancellationToken)
    {
        if (!_ready)
        {
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            _ready = true;
        }

        await container.GetBlobClient(name).UploadAsync(
            BinaryData.FromBytes(jpeg), new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" } }, cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            return (await container.GetBlobClient(name).DownloadContentAsync(cancellationToken)).Value.Content.ToArray();
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken) =>
        container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: cancellationToken);
}
