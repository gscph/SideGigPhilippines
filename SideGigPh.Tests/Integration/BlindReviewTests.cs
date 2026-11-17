using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Blind two-way reviews, end to end: after a completed job neither side's review of the other shows (to the other
/// side, on the worker's profile, in the stars and count that search ranks by, or on the household's posts) until
/// both reviewed, or <see cref="ReviewRules.BlindFor"/> after the job. The writer always sees their own. A Seeker's
/// one-way review after a chat, with no completed job, shows at once as before.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class BlindReviewTests(ApiTestHost api)
{
    private static string Tag() => $"zb{Guid.NewGuid():N}"[..10];

    private static SeekerAnswers PlumbingInTaytay()
    {
        var answers = SideGigPh.Tests.TestSupport.TestData.AirconCleaningInTaytay();
        answers.CategoryId = "construction";
        answers.ServiceId = "construction-plumbing";
        return answers;
    }

    private async Task<TestPhone> PlumberAsync(string name)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "construction", jobs: ["construction-plumbing"]);
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }

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

    private static async Task<long> PostAsync(TestPhone household)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(new JobPostDraft
        {
            CategoryId = "construction",
            ServiceId = "construction-plumbing",
            CityId = "taytay",
            BudgetPesos = 900,
            Description = $"Tumutulo ang gripo {Tag()}",
        })).Should().Be(JobPostResult.Posted);
        return board.Mine[0].Id;
    }

    /// <summary>The household posts, the worker offers, the household picks, and both mark it done.</summary>
    private static async Task<long> CompletedJobAsync(TestPhone household, TestPhone worker)
    {
        var postId = await PostAsync(household);
        (await worker.Get<JobBoard>().SendOfferAsync((await InFeedAsync(worker, postId))!, 900, null)).Should().Be(OfferResult.Done);
        var homeBoard = household.Get<JobBoard>();
        (await homeBoard.PickAsync((await homeBoard.LoadOffersAsync(postId)).Single())).Should().Be(OfferResult.Done);
        await homeBoard.MarkDoneAsync((await homeBoard.PickedJobsAsync(worker.Uid)).Single(j => j.PostId == postId));
        var workBoard = worker.Get<JobBoard>();
        (await workBoard.MarkDoneAsync((await workBoard.PickedJobsAsync(household.Uid)).Single(j => j.PostId == postId)))!.IsCompleted.Should().BeTrue();
        return postId;
    }

    private async Task ReviewAsync(TestPhone household, string workerUid, int stars) =>
        (await TryReviewAsync(household, workerUid, stars)).Should().Be(HttpStatusCode.NoContent);

    private async Task<HttpStatusCode> TryReviewAsync(TestPhone household, string workerUid, int stars)
    {
        using var response = await api.Client(api.Tokens.For(household.Uid)).PutAsJsonAsync("reviews",
            new SaveReviewRequest(workerUid, "Lorna", stars, "Maayos", "construction-plumbing"), ApiJson.Default.SaveReviewRequest);
        return response.StatusCode;
    }

    /// <summary>A PUT that must be refused as locked: 403 with <see cref="ApiErrors.ReviewLocked"/>.</summary>
    private async Task ShouldBeLockedAsync(TestPhone caller, string route, HttpContent body)
    {
        using var response = await api.Client(api.Tokens.For(caller.Uid, caller.PhoneNumber)).PutAsync(route, body);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.ReviewLocked);
    }

    private static HttpContent Review(string workerUid, int stars) =>
        JsonContent.Create(new SaveReviewRequest(workerUid, "Lorna", stars, "Binago ko", "construction-plumbing"), ApiJson.Default.SaveReviewRequest);

    private static HttpContent HouseholdRating(string seekerUid, int stars) =>
        JsonContent.Create(new SaveHouseholdReviewRequest(seekerUid, stars, null), ApiJson.Default.SaveHouseholdReviewRequest);

    private static async Task<SearchHit?> InSearchAsync(TestPhone seeker, string workerUid)
    {
        var results = await seeker.Seeker.SearchAsync();
        while (results.InTown.Concat(results.OtherTowns).All(h => h.Pro.Id != workerUid) && results.Count < results.Total)
        {
            results = await seeker.Seeker.MoreResultsAsync(results);
        }

        return results.InTown.Concat(results.OtherTowns).FirstOrDefault(h => h.Pro.Id == workerUid);
    }

    [IntegrationFact]
    public async Task AHouseholdsReview_StaysHiddenFromEveryoneButItsWriter_UntilTheWorkerReviewsBack()
    {
        var household = await TestPhone.SeekerAsync(api, "Lorna");
        var worker = await PlumberAsync("Berto");
        var stranger = await TestPhone.SeekerAsync(api, "Cora", PlumbingInTaytay());
        await CompletedJobAsync(household, worker);

        await ReviewAsync(household, worker.Uid, 2);

        (await stranger.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().BeEmpty("the worker hasn't reviewed back");
        (await worker.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().BeEmpty("the worker can't see it either, so can't answer it");
        (await stranger.Get<IReviewRepository>().GetBySeekerAsync(household.Uid)).Should().BeEmpty();
        (await stranger.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid))!.Reviews.Should().BeNull("hidden stars don't count in the average");
        (await InSearchAsync(stranger, worker.Uid))!.Pro.Reviews.Should().BeNull("search ranks by the stars that show");
        (await household.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Single().Hidden.Should().BeTrue("the writer sees their own");
        (await household.Get<IReviewRepository>().GetBySeekerAsync(household.Uid)).Single().Should().Match<Review>(r => r.Hidden && r.Rating == 2);
        var workersView = (await worker.Get<JobBoard>().PickedJobsAsync(household.Uid)).Single();
        workersView.TheyReviewed.Should().BeTrue();
        workersView.TheirRating.Should().BeNull("the stars stay hidden until the worker reviews too");
        BlindReviews.StageOf(workersView, DateTimeOffset.UtcNow).Should().Be(BlindReviewStage.WaitingForMe);
        BlindReviews.StageOf((await household.Get<JobBoard>().PickedJobsAsync(worker.Uid)).Single(), DateTimeOffset.UtcNow)
            .Should().Be(BlindReviewStage.WaitingForThem);
    }

    [IntegrationFact]
    public async Task WhenBothReviewed_BothShow_EverywhereAtOnce()
    {
        var household = await TestPhone.SeekerAsync(api, "Mila");
        var worker = await PlumberAsync("Dodong");
        var stranger = await TestPhone.SeekerAsync(api, "Nena", PlumbingInTaytay());
        var otherWorker = await PlumberAsync("Caloy");
        await CompletedJobAsync(household, worker);

        (await worker.Get<JobBoard>().RateHouseholdAsync(household.Uid, 3, "Late magbayad")).Should().BeTrue();
        var feedPost = await PostAsync(household);
        (await InFeedAsync(otherWorker, feedPost))!.HouseholdStars.Should().BeNull("the worker's review of the household is blind too");
        (await household.Get<JobBoard>().PickedJobsAsync(worker.Uid)).Single().Should().Match<PickedJob>(j => j.TheyReviewed && j.TheirRating == null);

        await ReviewAsync(household, worker.Uid, 5);

        (await stranger.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Single().Should().Match<Review>(r => r.Rating == 5 && !r.Hidden);
        (await worker.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().ContainSingle();
        (await stranger.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid))!.Reviews.Should().Be(new ReviewSummary(5, 1));
        (await InSearchAsync(stranger, worker.Uid))!.Pro.Reviews.Should().Be(new ReviewSummary(5, 1));
        (await InFeedAsync(otherWorker, feedPost))!.HouseholdStars.Should().Be(new ReviewSummary(3, 1));
        (await household.Get<JobBoard>().PickedJobsAsync(worker.Uid)).Single().TheirRating.Should().Be(3);
        var workersView = (await worker.Get<JobBoard>().PickedJobsAsync(household.Uid)).Single();
        workersView.TheirRating.Should().Be(5);
        BlindReviews.StageOf(workersView, DateTimeOffset.UtcNow).Should().Be(BlindReviewStage.Open);
    }

    [IntegrationFact]
    public async Task AfterTheWindow_AOneSidedReviewShows()
    {
        var household = await TestPhone.SeekerAsync(api, "Rosa");
        var worker = await PlumberAsync("Ernie");
        var stranger = await TestPhone.SeekerAsync(api, "Dina");
        var postId = await CompletedJobAsync(household, worker);
        await ReviewAsync(household, worker.Uid, 4);
        (await stranger.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().BeEmpty();

        await api.CompleteJobEarlierAsync(postId, ReviewRules.BlindFor + TimeSpan.FromHours(1));

        (await stranger.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Single().Rating.Should().Be(4, "14 days passed without the worker's review");
        (await stranger.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid))!.Reviews.Should().Be(new ReviewSummary(4, 1));
        (await household.Get<IReviewRepository>().GetBySeekerAsync(household.Uid)).Single().Hidden.Should().BeFalse();
        var workersView = (await worker.Get<JobBoard>().PickedJobsAsync(household.Uid)).Single();
        workersView.TheirRating.Should().Be(4);
    }

    [IntegrationFact]
    public async Task AOneWayReviewAfterAChat_ShowsAtOnce_AndStaysWhenAJobLaterCompletes()
    {
        var household = await TestPhone.SeekerAsync(api, "Nora", PlumbingInTaytay());
        var worker = await PlumberAsync("Pedro");
        var stranger = await TestPhone.SeekerAsync(api, "Tess");
        await household.Seeker.SendAsync((await household.Seeker.GetProAsync(worker.Uid))!, "Hello po");
        await api.StartChatEarlierAsync(household.Seeker.ChatIdWith(worker.Uid), TimeSpan.FromHours(25));

        await ReviewAsync(household, worker.Uid, 4);

        (await stranger.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Single().Hidden.Should().BeFalse("no completed job: nothing to wait for");
        (await stranger.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid))!.Reviews.Should().Be(new ReviewSummary(4, 1));

        await CompletedJobAsync(household, worker);

        (await stranger.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().ContainSingle("it was written before the job, so it stays as it was");
    }

    [IntegrationFact]
    public async Task OnceBothShow_NeitherSideCanChangeTheirReview_ButAHiddenOneCanStillBeChanged()
    {
        var household = await TestPhone.SeekerAsync(api, "Linda");
        var worker = await PlumberAsync("Tonyo");
        await CompletedJobAsync(household, worker);
        await ReviewAsync(household, worker.Uid, 2);
        await ReviewAsync(household, worker.Uid, 3); // still hidden: it can change
        (await household.Get<IReviewRepository>().GetBySeekerAsync(household.Uid)).Single().Locked.Should().BeFalse();

        (await worker.Get<JobBoard>().RateHouseholdAsync(household.Uid, 4, null)).Should().BeTrue();

        await ShouldBeLockedAsync(household, "reviews", Review(worker.Uid, 1));
        await ShouldBeLockedAsync(worker, "household-reviews", HouseholdRating(household.Uid, 1));
        (await worker.Get<JobBoard>().RateHouseholdAsync(household.Uid, 1, null)).Should().BeFalse();
        (await household.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Single().Should().Match<Review>(r => r.Rating == 3 && r.Locked);
        (await household.Get<IReviewRepository>().GetBySeekerAsync(household.Uid)).Single().Locked.Should().BeTrue();
        (await worker.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Single().Locked.Should().BeFalse("only the writer is told");
        (await household.Get<JobBoard>().PickedJobsAsync(worker.Uid)).Single().MyReviewLocked.Should().BeTrue();
        (await worker.Get<JobBoard>().PickedJobsAsync(household.Uid)).Single().Should().Match<PickedJob>(j => j.MyReviewLocked && j.MyRating == 4);

        await household.Get<IReviewRepository>().DeleteForPersonAsync(household.Uid); // Start over

        (await worker.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().ContainSingle("a locked review can't be taken back either");
    }

    [IntegrationFact]
    public async Task AfterTheWindow_TheOneSidedReviewLocks_AndANewCompletedJobOpensItAgain()
    {
        var household = await TestPhone.SeekerAsync(api, "Mercy");
        var worker = await PlumberAsync("Jun");
        var postId = await CompletedJobAsync(household, worker);
        await ReviewAsync(household, worker.Uid, 4);
        await api.CompleteJobEarlierAsync(postId, ReviewRules.BlindFor + TimeSpan.FromHours(1));

        await ShouldBeLockedAsync(household, "reviews", Review(worker.Uid, 1));
        (await household.Get<JobBoard>().PickedJobsAsync(worker.Uid)).Single().MyReviewLocked.Should().BeTrue();

        var newer = await CompletedJobAsync(household, worker);

        await ReviewAsync(household, worker.Uid, 5);
        var review = (await household.Get<IReviewRepository>().GetBySeekerAsync(household.Uid)).Single();
        review.Should().Match<Review>(r => r.Rating == 5 && r.Hidden && !r.Locked, "a review of the new service, blind again");
        (await household.Get<JobBoard>().PickedJobsAsync(worker.Uid)).Single(j => j.PostId == newer).MyReviewLocked.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task AOneWayReviewAfterAChat_CanAlwaysBeChanged()
    {
        var household = await TestPhone.SeekerAsync(api, "Susan");
        var worker = await PlumberAsync("Rudy");
        await household.Seeker.SendAsync((await household.Seeker.GetProAsync(worker.Uid))!, "Hello po");
        await api.StartChatEarlierAsync(household.Seeker.ChatIdWith(worker.Uid), TimeSpan.FromDays(30));
        await ReviewAsync(household, worker.Uid, 4);

        (await TryReviewAsync(household, worker.Uid, 2)).Should().Be(HttpStatusCode.NoContent);
        (await household.Get<IReviewRepository>().GetBySeekerAsync(household.Uid)).Single().Should().Match<Review>(r => r.Rating == 2 && !r.Locked);
    }
}
