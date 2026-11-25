using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Our own usage counts end to end (<c>app_events</c>): the API logs each business event in the code path that makes
/// the real change, once, and <c>POST events</c> keeps only the app's search events, for the caller of the token,
/// each event id once. Delete account keeps the counts without the person's id.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class AppEventTests(ApiTestHost api)
{
    private static string Tag() => $"ze{Guid.NewGuid():N}"[..10];

    private async Task<List<AppEventRow>> EventsAsync(Func<AppEventRow, bool> which)
    {
        using var scope = api.Server.Services.CreateScope();
        var rows = await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().AppEvents.AsNoTracking().ToListAsync();
        return rows.Where(which).OrderBy(e => e.Id).ToList();
    }

    private Task<List<AppEventRow>> EventsOfAsync(string uid) => EventsAsync(e => e.UserId == uid);

    private Task<List<AppEventRow>> EventsOfPostAsync(long postId) => EventsAsync(e => e.RequestId == postId.ToString());

    private async Task<HttpStatusCode> SendAsync(string? token, List<AppEventDto> events)
    {
        using var response = await api.Client(token).PostAsJsonAsync("events", events, ApiJson.Default.ListAppEventDto);
        return response.StatusCode;
    }

    private static AppEventDto Search(DateTimeOffset? at = null, string name = AppEventNames.Search) =>
        new(Guid.NewGuid(), name, "session-1", "aircon", "angono", "aircon linis", 3, at ?? DateTimeOffset.UtcNow, "0.1.0");

    private async Task<TestPhone> PlumberAsync(string name)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "construction", jobs: ["construction-plumbing"]);
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }

    private static JobPostDraft Draft() => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-plumbing",
        CityId = "taytay",
        BudgetPesos = 900,
        Title = "Ayusin ang gripo",
        Description = $"Tumutulo ang gripo {Tag()}",
    };

    private static async Task<JobPost?> InFeedAsync(TestPhone worker, long postId)
    {
        var board = worker.Get<JobBoard>();
        await board.LoadFeedAsync();
        while (board.HasMore && board.Feed.All(p => p.Id != postId))
        {
            await board.LoadMoreFeedAsync();
        }

        return board.Feed.FirstOrDefault(p => p.Id == postId);
    }

    /// <summary>The household posts, the worker offers, the household picks, and (when asked) both mark it done.</summary>
    private static async Task<long> JobAsync(TestPhone household, TestPhone worker, bool done)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(Draft())).Should().Be(JobPostResult.Posted);
        var postId = board.Mine[0].Id;
        (await worker.Get<JobBoard>().SendOfferAsync((await InFeedAsync(worker, postId))!, 900, null)).Should().Be(OfferResult.Done);
        (await board.PickAsync((await board.LoadOffersAsync(postId)).Single())).Should().Be(OfferResult.Done);
        if (done)
        {
            var workBoard = worker.Get<JobBoard>();
            await board.MarkDoneAsync((await board.PickedJobsAsync(worker.Uid)).Single(j => j.PostId == postId));
            await workBoard.MarkDoneAsync((await workBoard.PickedJobsAsync(household.Uid)).Single(j => j.PostId == postId));
        }

        return postId;
    }

    [IntegrationFact]
    public async Task AJob_IsCounted_FromThePost_ToTheMatch_ToBothSidesDone_AndAGoodWorkersFirstJob()
    {
        var household = await TestPhone.SeekerAsync(api, "Lorna");
        var worker = await PlumberAsync("Berto");

        var postId = await JobAsync(household, worker, done: true);

        var events = await EventsOfPostAsync(postId);
        events.Select(e => e.EventName).Should().Equal(
            AppEventNames.RequestCreated, AppEventNames.RequestMatched, AppEventNames.JobCompleted, AppEventNames.WorkerFirstJob);
        events.Should().OnlyContain(e => e.Source == AppEventStore.FromServer && e.ServiceCategory == "construction" && e.ClientTs == null);
        events[0].Should().BeEquivalentTo(new { UserId = household.Uid, UserType = "customer", Area = "taytay" }, o => o.ExcludingMissingMembers());
        events[1].MinutesToMatch.Should().BeInRange(0, 5);
        events[2].UserType.Should().Be("worker", "the worker marked it done last");
        events[3].UserId.Should().Be(worker.Uid);
        (await EventsOfAsync(worker.Uid)).Count(e => e.EventName == AppEventNames.WorkerProfileComplete).Should().Be(1);
    }

    [IntegrationFact]
    public async Task ASecondJobWithTheSameWorker_IsARehire_AndNotTheirFirstJobAgain()
    {
        var household = await TestPhone.SeekerAsync(api, "Mila");
        var worker = await PlumberAsync("Caloy");
        await JobAsync(household, worker, done: true);

        var second = await JobAsync(household, worker, done: true);

        (await EventsOfPostAsync(second)).Select(e => e.EventName).Should().Equal(
            AppEventNames.RequestCreated, AppEventNames.RequestMatched, AppEventNames.Rehire, AppEventNames.JobCompleted);
        (await EventsOfAsync(worker.Uid)).Count(e => e.EventName == AppEventNames.WorkerFirstJob).Should().Be(1);
    }

    [IntegrationFact]
    public async Task PublishingAgain_IsNotASecondCompleteProfile_AndAReportOrBlock_IsCountedWithItsReason()
    {
        var worker = await PlumberAsync("Dodong");
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        var household = await TestPhone.SeekerAsync(api, "Nena");

        using (var response = await api.Client(api.Tokens.For(household.Uid)).PostAsJsonAsync("reports",
                   new ReportRequest(worker.Uid, ReportKinds.Listing, ReportReasons.Scam, null, null, AlsoBlock: true), ApiJson.Default.ReportRequest))
        {
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        (await EventsOfAsync(worker.Uid)).Count(e => e.EventName == AppEventNames.WorkerProfileComplete).Should().Be(1);
        (await EventsOfAsync(household.Uid)).Where(e => e.EventName is AppEventNames.ReportUser or AppEventNames.BlockUser)
            .Select(e => (e.EventName, e.Reason)).Should().Equal(
                (AppEventNames.ReportUser, ReportReasons.Scam), (AppEventNames.BlockUser, ReportReasons.Scam));
    }

    [IntegrationFact]
    public async Task TheFirstSidePicked_IsTheSignUp_AndDeleteAccount_KeepsTheCountsWithoutTheId()
    {
        var household = await TestPhone.SeekerAsync(api, "Olga");
        await household.Account.SaveProfileAsync();
        await household.Account.SaveProfileAsync();
        (await SendAsync(api.Tokens.For(household.Uid), [Search()])).Should().Be(HttpStatusCode.Accepted);

        var mine = await EventsOfAsync(household.Uid);
        mine.Where(e => e.EventName == AppEventNames.SignUp).Should().ContainSingle().Which.UserType.Should().Be("customer");
        var ids = mine.Select(e => e.Id).ToList();

        await household.Account.DeleteAccountAsync();

        (await EventsOfAsync(household.Uid)).Should().BeEmpty();
        (await EventsAsync(e => ids.Contains(e.Id))).Should().HaveCount(ids.Count).And.OnlyContain(e => e.UserId == null);
    }

    [IntegrationFact]
    public async Task TheAppsSearches_AreKeptForTheTokensAccount_EachEventOnce_WithABadClockDropped()
    {
        var seeker = await TestPhone.SeekerAsync(api, "Pia");
        var token = api.Tokens.For(seeker.Uid);
        var once = Search();
        var future = Search(DateTimeOffset.UtcNow.AddHours(1));
        var old = Search(DateTimeOffset.UtcNow.AddDays(-8), AppEventNames.SearchEmpty);
        var typed = Search() with { SearchTerm = "aircon 09171234567 " + new string('x', 200) };

        (await SendAsync(token, [once, future, old, typed])).Should().Be(HttpStatusCode.Accepted);
        (await SendAsync(token, [once])).Should().Be(HttpStatusCode.Accepted, "a batch sent again after a lost answer");

        var kept = (await EventsOfAsync(seeker.Uid)).Where(e => e.Source == AppEventStore.FromApp).ToList();
        kept.Select(e => e.EventId).Should().Equal(once.EventId, future.EventId, old.EventId, typed.EventId);
        kept.Should().OnlyContain(e => e.UserType == "customer" && e.SessionId == "session-1" && e.Area == "angono");
        kept[0].ClientTs.Should().BeCloseTo(once.ClientTs!.Value.UtcDateTime, TimeSpan.FromMilliseconds(1));
        kept[1].ClientTs.Should().BeNull("a phone clock an hour ahead is wrong");
        kept[2].ClientTs.Should().BeNull("over a week old");
        kept[3].SearchTerm.Should().HaveLength(AppEventRules.MaxSearchTerm).And.NotContain("0917");
    }

    [IntegrationFact]
    public async Task OnlyTheAppsOwnEvents_FromASignedInCaller_InSmallBatches_AreKept()
    {
        var seeker = await TestPhone.SeekerAsync(api, "Quin");
        var token = api.Tokens.For(seeker.Uid);
        var good = Search();

        (await SendAsync(null, [good])).Should().Be(HttpStatusCode.Unauthorized);
        (await SendAsync(token, [good, Search(name: AppEventNames.JobCompleted)])).Should().Be(HttpStatusCode.BadRequest,
            "the business events are the server's own");
        (await SendAsync(token, [.. Enumerable.Range(0, AppEventRules.MaxBatch + 1).Select(_ => Search())])).Should().Be(HttpStatusCode.BadRequest);
        (await SendAsync(token, [])).Should().Be(HttpStatusCode.BadRequest);

        (await EventsOfAsync(seeker.Uid)).Should().NotContain(e => e.Source == AppEventStore.FromApp);
    }

    [IntegrationFact]
    public async Task TheWeeklyReport_RunsOnTheEvents_AndCountsTheWeeksJobs()
    {
        var household = await TestPhone.SeekerAsync(api, "Sally");
        await JobAsync(household, await PlumberAsync("Tonio"), done: true);
        var report = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "analytics", "weekly.sql"));

        await using var db = new SqlConnection(api.ConnectionString);
        await db.OpenAsync();
        await using var command = new SqlCommand(report, db);
        await using var reader = await command.ExecuteReaderAsync();
        var results = 0;
        var jobsThisWeek = 0;
        do
        {
            results++;
            while (await reader.ReadAsync())
            {
                if (results == 1)
                {
                    jobsThisWeek = reader.GetInt32(1); // ordered by week: the last row is this week
                }
            }
        }
        while (await reader.NextResultAsync());

        results.Should().Be(7, "one answer per question");
        jobsThisWeek.Should().BeGreaterThan(0);
        (await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "analytics", "retention.sql"))).Should().Contain("DATEADD(MONTH, -12");
    }

    [IntegrationFact]
    public async Task ASearchOnThePhone_ReachesTheServer_OnlyWhenUploaded()
    {
        var seeker = await TestPhone.SeekerAsync(api, "Rosa");

        await seeker.Seeker.SearchAsync();
        for (var i = 0; i < 100 && seeker.PendingEvents.Saved is null; i++)
        {
            await Task.Delay(10);
        }

        (await EventsOfAsync(seeker.Uid)).Should().NotContain(e => e.Source == AppEventStore.FromApp, "nothing is sent while the person searches");
        seeker.PendingEvents.Saved!.Events.Should().NotBeEmpty();

        (await seeker.Get<AnalyticsUploader>().SendAsync()).Should().BeTrue();

        (await EventsOfAsync(seeker.Uid)).Where(e => e.Source == AppEventStore.FromApp).Select(e => (e.EventName, e.ServiceCategory, e.Area))
            .Should().StartWith([(AppEventNames.Search, "aircon", "taytay")]);
        seeker.PendingEvents.Saved.Events.Should().BeEmpty();
    }
}
