using System.Collections.Concurrent;
using SideGigPh.Api;
using SideGigPh.AzureSQL;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>The private "verification" container, in memory: what is stored now, by name.</summary>
public sealed class InMemoryPrivateFiles : IPrivateFiles
{
    public ConcurrentDictionary<string, byte[]> Files { get; } = new();

    public Task SaveAsync(string name, byte[] jpeg, CancellationToken cancellationToken)
    {
        Files[name] = jpeg;
        return Task.CompletedTask;
    }

    public Task<byte[]?> ReadAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Files.TryGetValue(name, out var bytes) ? bytes : null);

    public Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        Files.TryRemove(name, out _);
        return Task.CompletedTask;
    }
}

/// <summary>Azure AI Document Intelligence stand-in: every read answers <see cref="Next"/> and is counted.</summary>
public sealed class FakeIdReader : IIdDocumentReader
{
    public IdScan Next { get; set; } = IdScan.None;

    public int Reads;

    public Task<IdScan> ReadAsync(byte[] jpeg, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Reads);
        return Task.FromResult(Next);
    }
}
