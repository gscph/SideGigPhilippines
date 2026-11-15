using System.Collections.Concurrent;
using SideGigPh.Api;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>Stands in for the Firebase Admin SDK: remembers which sign-in accounts the API deleted.</summary>
public sealed class RecordingAccountDeleter : IAccountDeleter
{
    private readonly ConcurrentBag<string> _deleted = [];

    public IReadOnlyCollection<string> Deleted => _deleted;

    public Task<bool> DeleteAsync(string uid, CancellationToken cancellationToken)
    {
        _deleted.Add(uid);
        return Task.FromResult(true);
    }
}
