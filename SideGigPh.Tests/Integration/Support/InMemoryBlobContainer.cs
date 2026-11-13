using System.Collections.Concurrent;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// Azure Blob Storage's "photos" container, in memory. It overrides only the calls <c>PhotoFunctions</c> makes, and
/// behaves like the real one where it matters: uploading an existing name with "if none match" fails with 409, a
/// missing blob is 404, and each blob keeps its metadata (the owner uid).
/// </summary>
public sealed class InMemoryBlobContainer : BlobContainerClient
{
    public static readonly Uri ContainerUri = new("https://teststorage.blob.core.windows.net/photos");

    public ConcurrentDictionary<string, (byte[] Bytes, IDictionary<string, string> Metadata)> Blobs { get; } = new();

    public PublicAccessType Access { get; private set; } = PublicAccessType.None;

    public bool Created { get; private set; }

    /// <summary>Like a storage account with "Allow Blob anonymous access" turned off: making the container public fails.</summary>
    public bool BlockPublicAccess { get; set; }

    /// <summary>The container exists but is private, e.g. made by hand in the portal or reset by a policy.</summary>
    public void MakePrivate()
    {
        Created = true;
        Access = PublicAccessType.None;
    }

    public override Uri Uri => ContainerUri;

    public override string Name => "photos";

    public override BlobClient GetBlobClient(string blobName) => new Blob(this, blobName);

    public override Task<Response<bool>> ExistsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Response.FromValue(Created, FakeResponse.Ok));

    public override Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(
        PublicAccessType publicAccessType = PublicAccessType.None,
        IDictionary<string, string>? metadata = null,
        BlobContainerEncryptionScopeOptions? encryptionScopeOptions = null,
        CancellationToken cancellationToken = default)
    {
        Created = true;
        Access = publicAccessType;
        return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobContainerInfo(ETag.All, DateTimeOffset.UtcNow), FakeResponse.Ok));
    }

    public override Task<Response<BlobContainerAccessPolicy>> GetAccessPolicyAsync(
        BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Response.FromValue(BlobsModelFactory.BlobContainerAccessPolicy(Access, ETag.All, DateTimeOffset.UtcNow, []), FakeResponse.Ok));

    public override Task<Response<BlobContainerInfo>> SetAccessPolicyAsync(
        PublicAccessType accessType = PublicAccessType.None,
        IEnumerable<BlobSignedIdentifier>? permissions = null,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default)
    {
        if (BlockPublicAccess && accessType != PublicAccessType.None)
        {
            throw new RequestFailedException(409, "Public access is not permitted on this storage account.", "PublicAccessNotPermitted", null);
        }

        Access = accessType;
        return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobContainerInfo(ETag.All, DateTimeOffset.UtcNow), FakeResponse.Ok));
    }

    private sealed class Blob(InMemoryBlobContainer container, string name) : BlobClient
    {
        public override Uri Uri => new($"{ContainerUri}/{name}");

        public override string Name => name;

        public override Task<Response<BlobContentInfo>> UploadAsync(BinaryData content, BlobUploadOptions options, CancellationToken cancellationToken = default)
        {
            if (!container.Blobs.TryAdd(name, (content.ToArray(), options.Metadata ?? new Dictionary<string, string>())))
            {
                throw new RequestFailedException(409, "The specified blob already exists.", "BlobAlreadyExists", null);
            }

            return Task.FromResult(Response.FromValue(
                BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, null, null, 0), FakeResponse.Ok));
        }

        public override Task<Response<BlobProperties>> GetPropertiesAsync(BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
        {
            if (!container.Blobs.TryGetValue(name, out var blob))
            {
                throw new RequestFailedException(404, "The specified blob does not exist.", "BlobNotFound", null);
            }

            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobProperties(metadata: blob.Metadata), FakeResponse.Ok));
        }

        public override Task<Response<bool>> DeleteIfExistsAsync(
            DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
            BlobRequestConditions? conditions = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Response.FromValue(container.Blobs.TryRemove(name, out _), FakeResponse.Ok));
    }

    /// <summary>The raw HTTP answer the Azure SDK wraps every result in; these calls never read it.</summary>
    private sealed class FakeResponse : Response
    {
        public static readonly FakeResponse Ok = new();

        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = string.Empty;

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<Azure.Core.HttpHeader> EnumerateHeaders() => [];

        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = [];
            return false;
        }
    }
}
