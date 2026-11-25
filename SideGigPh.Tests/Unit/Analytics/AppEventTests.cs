using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Analytics;

/// <summary>
/// Our own usage counts on the phone: a search is written to the phone, never sent while the person acts, and uploaded
/// later in batches that leave the phone only once the server kept them. No personal data in what is kept.
/// </summary>
public class AppEventTests
{
    private readonly ManualClock _clock = new();
    private readonly FakePendingEvents _store = new();

    private TarbahoAnalytics Analytics() => new(_store, _clock, NullLogger<TarbahoAnalytics>.Instance);

    private List<PendingEvent> Kept => _store.Saved?.Events ?? [];

    [Fact]
    public async Task ASearch_IsKeptOnThePhone_AndAnEmptyOneIsAlsoASearchEmpty()
    {
        var analytics = Analytics();

        await analytics.Search("aircon linis", "aircon", "aircon-cleaning", "angono", 3);
        await analytics.Search("tubero", "construction", null, "taytay", 0);

        Kept.Select(e => (e.Name, e.SearchTerm, e.ServiceCategory, e.Area, e.ResultCount)).Should().Equal(
            (AppEventNames.Search, "aircon linis", "aircon", "angono", 3),
            (AppEventNames.Search, "tubero", "construction", "taytay", 0),
            (AppEventNames.SearchEmpty, "tubero", "construction", "taytay", null));
        Kept.Should().OnlyContain(e => e.SessionId == analytics.SessionId && e.ClientTs == _clock.GetUtcNow());
        Kept.Select(e => e.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task TheSameSearchAgain_InOneSession_IsCountedOnce_AndAgainAfterHalfAnHourAway()
    {
        var analytics = Analytics();
        await analytics.Search(null, "aircon", null, "angono", 2);
        await analytics.Search(null, "aircon", null, "angono", 2); // Back to the results, or a new sort
        var first = analytics.SessionId;

        analytics.AppHidden();
        _clock.Advance(TimeSpan.FromMinutes(10));
        analytics.AppShown();
        await analytics.Search(null, "aircon", null, "angono", 2);
        analytics.SessionId.Should().Be(first, "10 minutes away is the same session");

        analytics.AppHidden();
        _clock.Advance(TarbahoAnalytics.NewSessionAfter);
        analytics.AppShown();
        await analytics.Search(null, "aircon", null, "angono", 2);

        analytics.SessionId.Should().NotBe(first);
        Kept.Select(e => e.SessionId).Should().Equal(first, analytics.SessionId);
    }

    [Fact]
    public async Task TypedWords_LoseNumbersAndEmails_BeforeTheyAreKept()
    {
        await Analytics().Search("aircon 09171234567 juan@example.com", "aircon", null, "angono", 1);

        Kept.Single().SearchTerm.Should().NotContain("0917").And.NotContain("juan@");
    }

    [Fact]
    public void ThePhone_KeepsAWeekAndAtMost500_DroppingTheOldestFirst()
    {
        var now = _clock.GetUtcNow();
        var events = Enumerable.Range(0, TarbahoAnalytics.MaxQueued + 10)
            .Select(i => Event(now - TimeSpan.FromMinutes(TarbahoAnalytics.MaxQueued + 10 - i))).ToList();
        events.Insert(0, Event(now - TarbahoAnalytics.KeepFor - TimeSpan.FromMinutes(1)));
        var newest = events[^1];

        TarbahoAnalytics.Trim(events, now);

        events.Should().HaveCount(TarbahoAnalytics.MaxQueued);
        events[^1].Should().Be(newest);
        events.Should().OnlyContain(e => e.ClientTs >= now - TarbahoAnalytics.KeepFor);
    }

    [Fact]
    public async Task AnUpload_SendsTheOldest50AtATime_AndTakesThemOffOnlyOnceKept()
    {
        var analytics = Analytics();
        _store.Saved = new PendingEventsState { Events = [.. Enumerable.Range(0, 120).Select(_ => Event(_clock.GetUtcNow()))] };
        var server = new FakeAppEvents();

        (await Uploader(analytics, server).SendAsync()).Should().BeTrue();

        server.Batches.Select(b => b.Count).Should().Equal(50, 50, 20);
        server.Batches.SelectMany(b => b).Select(e => e.EventId).Should().OnlyHaveUniqueItems();
        Kept.Should().BeEmpty();
    }

    [Fact]
    public async Task AFailedUpload_DeletesNothing_AndTheNextTrySendsTheSameEvents()
    {
        var analytics = Analytics();
        _store.Saved = new PendingEventsState { Events = [.. Enumerable.Range(0, 60).Select(_ => Event(_clock.GetUtcNow()))] };
        var server = new FakeAppEvents { FailAfter = 1 };

        (await Uploader(analytics, server).SendAsync()).Should().BeFalse();
        Kept.Should().HaveCount(10, "the first 50 were kept by the server, the rest wait");

        server.FailAfter = null;
        (await Uploader(analytics, server).SendAsync()).Should().BeTrue();
        Kept.Should().BeEmpty();
        server.Batches.SelectMany(b => b).Select(e => e.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SignedOut_NothingIsUploaded_AndABatchTheServerRefuses_IsDropped()
    {
        var analytics = Analytics();
        _store.Saved = new PendingEventsState { Events = [Event(_clock.GetUtcNow())] };
        var server = new FakeAppEvents();

        (await Uploader(analytics, server, signedIn: false).SendAsync()).Should().BeFalse();
        server.Batches.Should().BeEmpty();
        Kept.Should().HaveCount(1);

        server.Refuse = true;
        (await Uploader(analytics, server).SendAsync()).Should().BeTrue();
        Kept.Should().BeEmpty("a bad batch would block every event after it");
    }

    [Fact]
    public async Task TheSeekersSearch_IsCountedWhenItsResultsLoad_NotWhenTheyDidnt()
    {
        var app = new FakeApp(TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()), TestData.Worker());
        (await app.Seeker.EnsureLoadedAsync()).Should().BeTrue();

        await app.Seeker.SearchAsync(); // the fake's results didn't load (offline): the shared Empty
        await WaitForWritesAsync();
        app.PendingEvents.Saved.Should().BeNull();

        app.Profiles.SearchResults = new SearchResult([], [], 0, 0);
        await app.Seeker.SearchAsync();
        await WaitForWritesAsync(() => app.PendingEvents.Saved is not null);

        app.PendingEvents.Saved!.Events.Select(e => (e.Name, e.ServiceCategory, e.Area, e.ResultCount)).Should().Equal(
            (AppEventNames.Search, "aircon", "taytay", 0),
            (AppEventNames.SearchEmpty, "aircon", "taytay", null));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(6, false)]
    [InlineData(-60 * 24 * 6, true)]
    [InlineData(-60 * 24 * 8, false)]
    public void AnEventsOwnTime_IsBelievedOnlyWithinAWeekAndAFewMinutesAhead(int minutes, bool believed)
    {
        var now = new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);

        AppEventRules.Believable(now.AddMinutes(minutes), now).HasValue.Should().Be(believed);
    }

    private static PendingEvent Event(DateTimeOffset at) =>
        new(Guid.NewGuid(), AppEventNames.Search, "s", "aircon", "angono", null, 1, at, "0.1.0");

    private AnalyticsUploader Uploader(TarbahoAnalytics analytics, FakeAppEvents server, bool signedIn = true) =>
        new(analytics, server, new FakeIdentity(signedIn ? TestData.Worker() : null), new AppVisibility(), NullLogger<AnalyticsUploader>.Instance);

    private static async Task WaitForWritesAsync(Func<bool>? done = null)
    {
        for (var i = 0; i < 100 && done?.Invoke() != true; i++)
        {
            await Task.Delay(10);
        }
    }

    /// <summary>The server's <c>POST events</c>: keeps each batch, or fails / refuses when told.</summary>
    private sealed class FakeAppEvents : IAppEventRepository
    {
        public List<IReadOnlyList<PendingEvent>> Batches { get; } = [];

        /// <summary>Fails (no answer) once this many batches were kept.</summary>
        public int? FailAfter { get; set; }

        public bool Refuse { get; set; }

        public Task<bool> SendAsync(IReadOnlyList<PendingEvent> events)
        {
            if (Batches.Count >= FailAfter)
            {
                throw new HttpRequestException("offline");
            }

            if (Refuse)
            {
                return Task.FromResult(false);
            }

            Batches.Add([.. events]);
            return Task.FromResult(true);
        }
    }
}
