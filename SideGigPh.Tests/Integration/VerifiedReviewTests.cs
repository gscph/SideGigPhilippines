using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Verified two-way reviews end to end (brief item 12): a picked offer is a job between the two; both mark it done;
/// then the household's review shows "Verified job" (and needs no wait), the job counts on the worker's profile, and
/// the worker can rate the household, whose stars then show on its posts in workers' feeds.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class VerifiedReviewTests(ApiTestHost api)
{
    private static string Tag() => $"zv{Guid.NewGuid():N}"[..10];

    private static JobPostDraft Draft(string tag) => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-plumbing",
        CityId = "taytay",
        BudgetPesos = 900,
        Title = "Ayusin ang problema",
        Description = $"Barado ang CR sa taas {tag}",
    };

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

    /// <summary>The household posts, the worker offers, the household picks: one picked job between them.</summary>
    private static async Task<JobPost> PickedJobAsync(TestPhone household, TestPhone worker)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(Draft(Tag()))).Should().Be(JobPostResult.Posted);
        var post = board.Mine[0];
        (await worker.Get<JobBoard>().SendOfferAsync((await InFeedAsync(worker, post.Id))!, 900, null)).Should().Be(OfferResult.Done);
        var offer = (await board.LoadOffersAsync(post.Id)).Single();
        (await board.PickAsync(offer)).Should().Be(OfferResult.Done);
        return post;
    }

    private async Task<HttpStatusCode> ReviewAsync(TestPhone household, string workerUid, int stars)
    {
        using var response = await api.Client(api.Tokens.For(household.Uid)).PutAsJsonAsync("reviews",
            new SaveReviewRequest(workerUid, "Lorna", stars, "Mabilis at malinis", "construction-plumbing"), ApiJson.Default.SaveReviewRequest);
        return response.StatusCode;
    }

    [IntegrationFact]
    public async Task BothMarkDone_MakesAVerifiedJob_ThatCountsOnTheProfile_AndTheWorkerRatesTheHousehold()
    {
        var household = await TestPhone.SeekerAsync(api, "Lorna");
        var worker = await PlumberAsync("Berto");
        var post = await PickedJobAsync(household, worker);
        var homeBoard = household.Get<JobBoard>();
        var workBoard = worker.Get<JobBoard>();

        var job = (await homeBoard.PickedJobsAsync(worker.Uid)).Should().ContainSingle().Subject;
        job.Should().Be(new PickedJob(post.Id, "construction-plumbing", 900, job.PickedAt, IAmTheHousehold: true, DoneByHousehold: false, DoneByWorker: false));
        (await workBoard.PickedJobsAsync(household.Uid)).Single().IAmTheHousehold.Should().BeFalse();
        (await ReviewAsync(household, worker.Uid, 5)).Should().Be(HttpStatusCode.Forbidden, "no completed job yet, and the chat is new");

        (await homeBoard.MarkDoneAsync(job))!.DoneByHousehold.Should().BeTrue();
        (await workBoard.RateHouseholdAsync(household.Uid, 4, null)).Should().BeFalse("the worker hasn't marked it done yet");
        var done = await workBoard.MarkDoneAsync((await workBoard.PickedJobsAsync(household.Uid)).Single());

        done!.IsCompleted.Should().BeTrue();
        (await ReviewAsync(household, worker.Uid, 5)).Should().Be(HttpStatusCode.NoContent, "a completed job needs no wait");
        var listing = (await household.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid))!;
        listing.CompletedJobs.Should().Be(1);
        (await household.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Single().VerifiedJob.Should().BeTrue();

        (await workBoard.RateHouseholdAsync(household.Uid, 4, "Mabait, malinaw ang bilin")).Should().BeTrue();
        (await workBoard.PickedJobsAsync(household.Uid)).Single().MyRating.Should().Be(4);
        var other = await PlumberAsync("Caloy");
        (await homeBoard.PostAsync(Draft(Tag()))).Should().Be(JobPostResult.Posted);
        (await InFeedAsync(other, homeBoard.Mine[0].Id))!.HouseholdStars.Should().Be(new ReviewSummary(4, 1), "workers see how other workers rated this household");
    }

    [IntegrationFact]
    public async Task APickBetweenTwoWhoAlreadyChatted_MovesTheirChatToThePostsServiceAndTown()
    {
        var household = await TestPhone.SeekerAsync(api, "Nena");
        var worker = await PlumberAsync("Pedro");
        await household.Seeker.SendAsync((await household.Seeker.GetProAsync(worker.Uid))!, "Hello po");
        var chats = household.Get<IChatRepository>();
        var chatId = Chat.IdFor(worker.Uid, household.Uid);
        (await chats.GetAsync(chatId))!.ServiceId.Should().BeNull("the household searched nothing before writing");

        await PickedJobAsync(household, worker);

        var chat = (await chats.GetAsync(chatId))!;
        chat.ServiceId.Should().Be("construction-plumbing", "the chat is now about the picked post, so reviews and Inbox rows name it");
        chat.CityId.Should().Be("taytay");
    }

    [IntegrationFact]
    public async Task OnlyTheTwoOfThem_SeeOrMarkTheirJob()
    {
        var household = await TestPhone.SeekerAsync(api, "Mila");
        var worker = await PlumberAsync("Dodong");
        var stranger = await PlumberAsync("Iba");
        var post = await PickedJobAsync(household, worker);

        (await stranger.Get<JobBoard>().PickedJobsAsync(household.Uid)).Should().BeEmpty();
        using var response = await api.Client(api.Tokens.For(stranger.Uid, stranger.PhoneNumber)).PostAsync($"picked-jobs/{post.Id}/done", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await household.Get<JobBoard>().PickedJobsAsync(worker.Uid)).Single().DoneByWorker.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task AReviewWithoutAPickedJob_IsNotVerified()
    {
        var household = await TestPhone.SeekerAsync(api, "Nena");
        var worker = await PlumberAsync("Ernie");
        await PickedJobAsync(household, worker); // picked, but nobody marked it done

        (await household.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().BeEmpty();
        (await household.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid))!.CompletedJobs.Should().Be(0);
    }
}
