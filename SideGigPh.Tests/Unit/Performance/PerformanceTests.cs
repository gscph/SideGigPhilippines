using System.Diagnostics;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Maui.Services.Search;
using SideGigPh.Tests.TestSupport;
using Microsoft.Extensions.Caching.Memory;

namespace SideGigPh.Tests.Unit.Performance;

/// <summary>
/// Guards against changes that make the app slow or chatty. The time limits are deliberately generous (10–50×
/// what the code needs today on a laptop), so they only fail when something becomes dramatically slower, never on a
/// busy machine. The cache tests make sure the app doesn't ask the server again for data it just read: every extra
/// request costs a worker's prepaid mobile data and battery.
/// </summary>
public class PerformanceTests
{
    private static List<ListingDto> ManyWorkers(int count)
    {
        var towns = ReferenceData.Cities.Select(c => c.Id).ToArray();
        var rates = ReferenceData.RateOptions.Select(r => r.Id).ToArray();
        var services = ReferenceData.ServicesByCategory["aircon"].Select(s => s.Id).ToArray();
        return Enumerable.Range(0, count).Select(i => TestData.Dto(
            $"Worker {i}",
            cities: [towns[i % towns.Length]],
            services: [services[i % services.Length], services[(i + 3) % services.Length]],
            rate: rates[i % rates.Length],
            reviews: i % 4 == 0 ? null : new ReviewSummary(3 + (i % 20) / 10.0, i % 50))).ToList();
    }

    [Fact]
    public void Ranking5000Workers_TakesUnderHalfASecond()
    {
        var workers = ManyWorkers(5000);
        var request = new RankRequest("aircon-cleaning", "taytay", "500-1000");
        SearchRanker.Rank(workers, request, TestData.Scales); // warm up

        var clock = Stopwatch.StartNew();
        var result = SearchRanker.Rank(workers, request, TestData.Scales);
        clock.Stop();

        result.InTown.Should().NotBeEmpty();
        clock.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Understanding1000Searches_TakesUnderASecond()
    {
        string[] typed = ["tubero barado", "aircon leak", "pa-weld gate", "masahe", "kuryente brownout", "repair", "solar panel linis"];
        SearchIntentParser.Parse("warm up");

        var clock = Stopwatch.StartNew();
        for (var i = 0; i < 1000; i++)
        {
            SearchIntentParser.Parse(typed[i % typed.Length]);
        }

        clock.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SwitchingTabs_WithinFifteenSeconds_DoesNotReloadTheWorkersInbox()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        var readsAfterLoad = app.Chats.InboxReads;

        await app.Pro.RefreshIfStaleAsync();
        await app.Pro.RefreshIfStaleAsync();

        app.Chats.InboxReads.Should().Be(readsAfterLoad);
        app.Reviews.Reads.Should().Be(1);
    }

    [Fact]
    public async Task SwitchingTabs_WithinFifteenSeconds_DoesNotReloadTheSeekersInbox()
    {
        var app = await FakeApp.SeekerAsync();
        var readsAfterLoad = app.Chats.InboxReads;

        await app.Seeker.RefreshIfStaleAsync();

        app.Chats.InboxReads.Should().Be(readsAfterLoad);
    }

    [Fact]
    public async Task AfterStartOver_TheNextTabOpenReadsFresh()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        var seeker = await FakeApp.SeekerAsync();
        app.Pro.Forget();
        seeker.Seeker.Forget();

        await app.Pro.RefreshIfStaleAsync();
        await seeker.Seeker.RefreshIfStaleAsync();

        app.Chats.InboxReads.Should().Be(2);
        seeker.Chats.InboxReads.Should().Be(2);
    }

    // Shortening these makes every screen change cost a request; lengthening them makes new data slow to appear.
    [Fact]
    public void CacheWindows_StayAsDesigned()
    {
        ApiProfessionalProfileRepository.SearchFreshFor.Should().Be(TimeSpan.FromSeconds(60));
        ApiProfessionalProfileRepository.ListingFreshFor.Should().Be(TimeSpan.FromMinutes(5));
        ProWorkspace.ActivityFreshFor.Should().Be(TimeSpan.FromSeconds(15));
        SeekerWorkspace.ActivityFreshFor.Should().Be(TimeSpan.FromSeconds(15));
        ApiChatRepository.WatchInterval.Should().Be(TimeSpan.FromSeconds(5));
        ListingCache.SearchFor.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void TimedCache_KeepsAnswersForItsWindow_AndForgetsOnRequest()
    {
        var cache = new TimedCache<string>(TimeSpan.FromMinutes(1));
        cache.Set("a", "1");
        cache.Set("b", "2");

        cache.TryGet("a", out var a).Should().BeTrue();
        a.Should().Be("1");

        cache.Remove("a");
        cache.TryGet("a", out _).Should().BeFalse();

        cache.Clear();
        cache.TryGet("b", out _).Should().BeFalse();
    }

    [Fact]
    public void TimedCache_OldAnswer_IsNotUsed()
    {
        var cache = new TimedCache<string>(TimeSpan.Zero);
        cache.Set("a", "1");

        cache.TryGet("a", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ServerSearchCache_ReadsTheDatabaseOnce_UntilAListingChanges()
    {
        var cache = new ListingCache(new MemoryCache(new MemoryCacheOptions()));
        var reads = 0;
        Task<IReadOnlyList<ListingDto>> Read()
        {
            reads++;
            return Task.FromResult<IReadOnlyList<ListingDto>>([]);
        }

        await cache.SearchAsync("aircon", "rizal", Read);
        await cache.SearchAsync("aircon", "rizal", Read);
        reads.Should().Be(1);

        cache.Clear();
        await cache.SearchAsync("aircon", "rizal", Read);
        reads.Should().Be(2, "a publish or review clears the cache at once");
    }
}
