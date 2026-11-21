using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The household's exact place for the worker it picked, end to end (the app's <see cref="JobBoard"/> → API → SQL
/// Server): only the household sends it, only the picked worker gets it, and it is gone once the job is called off or
/// both mark it done.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class MeetLocationTests(ApiTestHost api)
{
    private static readonly GeoPoint House = new(14.5291, 121.1502);

    private static JobPostDraft Draft() => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-plumbing",
        CityId = "angono",
        BudgetPesos = 900,
        Description = $"Barado ang lababo sa kusina {Guid.NewGuid():N}"[..40],
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

    /// <summary>The household posts, the worker offers, the household picks it: the job as each side sees it.</summary>
    private static async Task<(PickedJob Household, PickedJob Worker)> PickedAsync(TestPhone household, TestPhone worker)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(Draft())).Should().Be(JobPostResult.Posted);
        var post = board.Mine[0];
        (await worker.Get<JobBoard>().SendOfferAsync((await InFeedAsync(worker, post.Id))!, 900, null)).Should().Be(OfferResult.Done);
        (await board.PickAsync((await board.LoadOffersAsync(post.Id)).Single())).Should().Be(OfferResult.Done);
        return ((await board.PickedJobsAsync(worker.Uid)).Single(), (await worker.Get<JobBoard>().PickedJobsAsync(household.Uid)).Single());
    }

    [IntegrationFact]
    public async Task TheHouseholdSendsItsPlace_AndOnlyThePickedWorkerSeesIt()
    {
        var household = await TestPhone.SeekerAsync(api, "Marites");
        var worker = await PlumberAsync("Tubero Ben");
        var (mine, theirs) = await PickedAsync(household, worker);
        theirs.MeetPoint.Should().BeNull("nothing is sent until the household taps");

        var sent = await household.Get<JobBoard>().ShareMeetLocationAsync(mine, House);

        sent!.MeetPoint.Should().Be(House);
        sent.MeetSharedAt.Should().NotBeNull();
        var seen = (await worker.Get<JobBoard>().PickedJobsAsync(household.Uid)).Single();
        seen.MeetPoint.Should().Be(House);
        MeetLocation.CanSee(seen).Should().BeTrue();

        var moved = new GeoPoint(14.5300, 121.1510);
        (await household.Get<JobBoard>().ShareMeetLocationAsync(sent, moved))!.MeetPoint.Should().Be(moved, "sending again replaces it");
    }

    [IntegrationFact]
    public async Task TheWorker_AStranger_OrAPointAbroad_IsRefused()
    {
        var household = await TestPhone.SeekerAsync(api, "Cora");
        var worker = await PlumberAsync("Tubero Lito");
        var stranger = await TestPhone.SeekerAsync(api, "Iba");
        var (mine, theirs) = await PickedAsync(household, worker);
        var body = new ShareMeetLocationRequest(House.Latitude, House.Longitude);

        (await worker.Get<JobBoard>().ShareMeetLocationAsync(theirs, House)).Should().BeNull("the app never lets a worker send it");
        using (var asWorker = await api.Client(api.Tokens.For(worker.Uid)).PutAsJsonAsync($"picked-jobs/{mine.PostId}/location", body, ApiJson.Default.ShareMeetLocationRequest))
        {
            asWorker.StatusCode.Should().Be(HttpStatusCode.NotFound, "only the household sends its place");
        }

        using (var asStranger = await api.Client(api.Tokens.For(stranger.Uid)).PutAsJsonAsync($"picked-jobs/{mine.PostId}/location", body, ApiJson.Default.ShareMeetLocationRequest))
        {
            asStranger.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        using (var abroad = await api.Client(api.Tokens.For(household.Uid)).PutAsJsonAsync($"picked-jobs/{mine.PostId}/location",
                   new ShareMeetLocationRequest(35.68, 139.69), ApiJson.Default.ShareMeetLocationRequest))
        {
            abroad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        (await stranger.Get<JobBoard>().PickedJobsAsync(household.Uid)).Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task CallingTheJobOff_RemovesThePlace()
    {
        var household = await TestPhone.SeekerAsync(api, "Lorna");
        var worker = await PlumberAsync("Tubero Dan");
        var (mine, _) = await PickedAsync(household, worker);
        var board = household.Get<JobBoard>();
        var sent = await board.ShareMeetLocationAsync(mine, House);

        (await board.CancelPickAsync(sent!, PickCancelReasons.NotNeeded, null)).Should().Be(CancelPickResult.Cancelled);

        (await api.HoldsMeetLocationAsync(mine.PostId)).Should().BeFalse("the point is deleted, not just hidden");
    }

    [IntegrationFact]
    public async Task ACompletedJob_ForgetsThePlace_AndTakesNoNewOne()
    {
        var household = await TestPhone.SeekerAsync(api, "Nena");
        var worker = await PlumberAsync("Tubero Jun");
        var (mine, theirs) = await PickedAsync(household, worker);
        var board = household.Get<JobBoard>();
        var sent = await board.ShareMeetLocationAsync(mine, House);

        (await board.MarkDoneAsync(sent!)).Should().NotBeNull();
        var done = await worker.Get<JobBoard>().MarkDoneAsync(theirs);

        done!.IsCompleted.Should().BeTrue();
        done.MeetPoint.Should().BeNull("the worker found the house: the point isn't kept");
        (await api.HoldsMeetLocationAsync(mine.PostId)).Should().BeFalse();
        (await board.ShareMeetLocationAsync((await board.PickedJobsAsync(worker.Uid)).Single(), House)).Should().BeNull();
        using var late = await api.Client(api.Tokens.For(household.Uid)).PutAsJsonAsync($"picked-jobs/{mine.PostId}/location",
            new ShareMeetLocationRequest(House.Latitude, House.Longitude), ApiJson.Default.ShareMeetLocationRequest);
        late.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [IntegrationFact]
    public async Task ThePin_SaysCustomersLocation_InTheChat_WithoutTheCoordinates()
    {
        var household = await TestPhone.SeekerAsync(api, "Pining");
        var worker = await PlumberAsync("Tubero Pin");
        var (mine, _) = await PickedAsync(household, worker);
        await household.Get<JobBoard>().ShareMeetLocationAsync(mine, House);
        var listing = (await household.Seeker.GetProAsync(worker.Uid))!;
        await household.Seeker.SendAsync(listing, "Pinili kita, salamat po"); // a pick opens the chat with a first message

        var pin = await household.Seeker.SendPinAsync(listing);

        pin.Kind.Should().Be(ChatMessageKind.Pin);
        pin.Text.Should().BeNull("the place stays on the picked job, so deleting it leaves nothing in the chat");
        await worker.Pro.RefreshActivityAsync();
        var chat = worker.Pro.Chats.Single(c => c.SeekerId == household.Uid);
        chat.LastMessage.Kind.Should().Be(ChatMessageKind.Pin);
        (await worker.Pro.GetMessagesAsync(chat.Id)).Messages.Last().Kind.Should().Be(ChatMessageKind.Pin);

        using var fromWorker = await api.Client(api.Tokens.For(worker.Uid)).PostAsJsonAsync($"chats/{chat.Id}/messages",
            new SendMessageRequest(Guid.NewGuid().ToString("N"), worker.Uid, household.Uid, Wire.Pin, null, "Tubero Pin", "Pining",
                null, null, null, null), ApiJson.Default.SendMessageRequest);
        fromWorker.StatusCode.Should().Be(HttpStatusCode.Forbidden, "only the household sends its place");
    }
}
