using SideGigPh.Maui.Models;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The app's main journey, end to end through the real API and database: a Skilled Worker publishes, a Seeker finds
/// them, messages them, the worker replies, and the Seeker leaves a review that lifts the worker in search. If any
/// layer is mis-wired (a route, a JSON name, a column, a token check), one of these fails.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class WorkerToCustomerTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task WorkerPublishes_SeekerFindsThemInSearch()
    {
        // Arrange
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Juan");
        var seeker = await TestPhone.SeekerAsync(api, "Maricel");

        // Act
        var published = await worker.Pro.PublishAsync();
        var results = await seeker.Seeker.SearchAsync();

        // Assert
        published.Should().Be(PublishResult.Published);
        var hit = results.InTown.Should().ContainSingle(h => h.Pro.Id == worker.Uid).Subject;
        hit.Pro.DisplayName.Should().Be("Juan");
        hit.Pro.MobileNumber.Should().Be(worker.PhoneNumber, "the listing number is the account's verified number");
        hit.Pro.PhotoUrl.Should().StartWith(InMemoryBlobContainer.ContainerUri.ToString());
        hit.Pro.WorkPhotoUrls.Should().HaveCount(2);
        hit.Pro.ReferenceCount.Should().Be(1, "Seekers see how many people vouch, never who");
        hit.FitsBudget.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task SeekerMessagesWorker_WorkerSeesItAndReplies_SeekerSeesTheReply()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Ana");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Ben");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;

        // The Seeker sends the ready-made first message, which starts the chat.
        await seeker.Seeker.SendAsync(listing, seeker.Seeker.DefaultMessage(listing));

        // The worker's Inbox shows it as unread, with the job from the Seeker's answers.
        await worker.Pro.RefreshActivityAsync();
        var chat = worker.Pro.Chats.Should().ContainSingle().Subject;
        worker.Pro.UnreadCount.Should().Be(1);
        chat.ServiceId.Should().Be("aircon-cleaning");
        chat.CityId.Should().Be("taytay");
        (await worker.Pro.GetMessagesAsync(chat.Id)).Single().Text.Should().Contain("Aircon cleaning");

        // Opening it clears the badge; the reply reaches the Seeker.
        await worker.Pro.MarkReadAsync(chat.Id);
        await worker.Pro.ReplyAsync(chat, "Available po ako bukas.");
        await worker.Pro.RefreshActivityAsync();
        worker.Pro.UnreadCount.Should().Be(0);

        await seeker.Seeker.RefreshActivityAsync();
        seeker.Seeker.UnreadCount.Should().Be(1);
        (await seeker.Seeker.GetMessagesAsync(worker.Uid)).Select(m => m.Text).Should().EndWith("Available po ako bukas.");
    }

    [IntegrationFact]
    public async Task SeekerCallsWorker_TheCallIsLoggedInTheChat()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Carlo");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Dina");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;

        var logged = await seeker.Seeker.CallAsync(listing);

        seeker.Dialer.Called.Should().Equal(worker.PhoneNumber);
        logged!.Kind.Should().Be(ChatMessageKind.Call);
        await worker.Pro.RefreshActivityAsync();
        worker.Pro.Chats.Single().LastMessage.Kind.Should().Be(ChatMessageKind.Call);
    }

    [IntegrationFact]
    public async Task SeekerReviewsWorker_StarsShowInSearchAndOnTheWorkersReviews()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Eli", jobs: ["aircon-freon"]);
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Fe", Answers("aircon-freon"));
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        await seeker.Seeker.SendAsync(listing, "Hi po");

        var saved = await seeker.Seeker.SubmitReviewAsync(listing, 4, "Maayos", "Fe S.");

        saved.Should().BeTrue();
        var hit = (await TestPhone.SeekerAsync(api, "Gina", Answers("aircon-freon")).ContinueWith(t => t.Result.Seeker.SearchAsync()).Unwrap())
            .InTown.Single(h => h.Pro.Id == worker.Uid);
        hit.Reviews.Should().Be(new ReviewSummary(4, 1));
        await worker.Pro.RefreshActivityAsync();
        worker.Pro.ReviewSummary.Should().Be(new ReviewSummary(4, 1));
        worker.Pro.Reviews.Single().Comment.Should().Be("Maayos");
    }

    [IntegrationFact]
    public async Task ReviewWithoutAChat_IsRefusedByTheServer()
    {
        // The app hides the review button, but the server must refuse it too (no fake reviews).
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Hugo");
        await worker.Pro.PublishAsync();
        var stranger = await TestPhone.SeekerAsync(api, "Ivy");

        var save = () => stranger.Get<SideGigPh.Maui.Services.IReviewRepository>().SaveAsync(
            new Review("x", worker.Uid, stranger.Uid, "Ivy", 1, "fake", null, DateTimeOffset.Now));

        (await save.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [IntegrationFact]
    public async Task WorkerInAnotherTown_IsListedAsNearby()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Jojo", jobs: ["aircon-pms"], towns: ["antipolo"]);
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Kim", Answers("aircon-pms"));

        var results = await seeker.Seeker.SearchAsync();

        results.InTown.Should().NotContain(h => h.Pro.Id == worker.Uid);
        results.OtherTowns.Should().Contain(h => h.Pro.Id == worker.Uid);
    }

    [IntegrationFact]
    public async Task TypedSearch_FindsTheWorker()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Lito", trade: "plumbing", jobs: ["plumbing-clog"]);
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Mia");

        await seeker.Seeker.SearchForAsync("barado ang lababo");
        var results = await seeker.Seeker.SearchAsync();

        results.InTown.Should().Contain(h => h.Pro.Id == worker.Uid);
    }

    private static SeekerAnswers Answers(string job)
    {
        var answers = SideGigPh.Tests.TestSupport.TestData.AirconCleaningInTaytay();
        answers.ServiceId = job;
        return answers;
    }
}
