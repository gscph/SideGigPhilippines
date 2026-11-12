using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// Search results kept for <see cref="SearchFor"/> in this API instance, so a burst of Seekers searching the same
/// trade and area costs one database read. A publish, removal or review on this instance clears it at once; other
/// instances catch up within <see cref="SearchFor"/>, which is as stale as a search can be.
/// </summary>
public sealed class ListingCache(IMemoryCache cache)
{
    public static readonly TimeSpan SearchFor = TimeSpan.FromSeconds(30);

    private CancellationTokenSource _reset = new();

    public async Task<IReadOnlyList<ListingDto>> SearchAsync(string categoryId, string? areaId, Func<Task<IReadOnlyList<ListingDto>>> read) =>
        (await cache.GetOrCreateAsync($"search:{categoryId}:{areaId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = SearchFor;
            entry.AddExpirationToken(new CancellationChangeToken(_reset.Token));
            return read();
        }))!;

    /// <summary>A listing or its reviews changed: the next search reads the database.</summary>
    public void Clear()
    {
        var old = Interlocked.Exchange(ref _reset, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }
}
