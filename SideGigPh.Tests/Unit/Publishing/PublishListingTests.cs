using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// Publishing is how a Skilled Worker starts getting customers. If it breaks, workers can't earn, so every
/// outcome the Publish page reacts to is checked here: the app must refuse early (with the right reason) what the
/// server would refuse, and must send a complete listing when it is ready.
/// </summary>
public class PublishListingTests
{
    [Fact]
    public async Task Publish_ReadyListing_SendsItAndMarksItPublished()
    {
        // Arrange
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());

        // Act
        var result = await app.Pro.PublishAsync();

        // Assert
        result.Should().Be(PublishResult.Published);
        app.Pro.IsPublished.Should().BeTrue();
        app.Profiles.Published.Should().ContainSingle();
        app.Store.State.Professional.PublishedAt.Should().NotBeNull("the publish date is saved on the phone");
    }

    [Fact]
    public async Task Publish_AnswerLostButListingLive_CountsAsPublished()
    {
        // The phone's 4G dropped while the server was answering: the listing is live anyway.
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Profiles.PublishThrows = new HttpRequestException("Socket closed");
        app.Profiles.LandsAnyway = true;

        var result = await app.Pro.PublishAsync();

        result.Should().Be(PublishResult.Published);
        app.Pro.IsPublished.Should().BeTrue();
        app.Profiles.Forgotten.Should().Contain(TestData.WorkerUid, "the check must ask the server, not a kept copy");
    }

    [Fact]
    public async Task Publish_AnswerLostAndNotLive_StillFails()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Profiles.PublishThrows = new HttpRequestException("Socket closed");

        var publish = () => app.Pro.PublishAsync();

        await publish.Should().ThrowAsync<HttpRequestException>();
        app.Pro.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task Publish_ServerError_FailsWithoutCheckingAgain()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Profiles.PublishThrows = new HttpRequestException("500", null, System.Net.HttpStatusCode.InternalServerError);
        app.Profiles.LandsAnyway = true;

        var publish = () => app.Pro.PublishAsync();

        await publish.Should().ThrowAsync<HttpRequestException>("the server answered: it refused, nothing was lost");
        app.Pro.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task Publish_SendsAccountNameNumberAndPhotos()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());

        await app.Pro.PublishAsync();

        var listing = app.Profiles.Published.Single();
        listing.Id.Should().Be(TestData.WorkerUid, "the listing id is the signed-in uid");
        listing.DisplayName.Should().Be("Juan Dela Cruz");
        listing.MobileNumber.Should().Be(TestData.WorkerNumber);
        listing.PhotoUrl.Should().EndWith($"{TestData.PhotoId(1)}.jpg");
        listing.WorkPhotoUrls.Should().HaveCount(2);
    }

    [Fact]
    public async Task Publish_MissingInterviewAnswer_ReturnsIncompleteWithoutSending()
    {
        var draft = TestData.ReadyDraft();
        draft.RateTierId = null; // e.g. the rate question was skipped

        var app = await FakeApp.ProAsync(draft);
        var result = await app.Pro.PublishAsync();

        result.Should().Be(PublishResult.Incomplete);
        app.Profiles.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_NoProfilePhoto_ReturnsMissingProof()
    {
        var draft = TestData.ReadyDraft();
        draft.ProfilePhotoId = null;

        var app = await FakeApp.ProAsync(draft);

        (await app.Pro.PublishAsync()).Should().Be(PublishResult.MissingProof);
        app.Profiles.Published.Should().BeEmpty();
    }

    // Work photos are optional: too much friction before going live. They only help the listing be seen.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Publish_FewOrNoWorkPhotos_StillPublishes(int count)
    {
        var draft = TestData.ReadyDraft();
        draft.WorkPhotoIds = [.. Enumerable.Range(2, count).Select(TestData.PhotoId)];

        var app = await FakeApp.ProAsync(draft);

        draft.HasProof.Should().BeTrue();
        app.Pro.IsReadyToPublish.Should().BeTrue();
        (await app.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        app.Profiles.Published.Should().ContainSingle().Which.WorkPhotoUrls.Should().HaveCount(count);
    }

    [Fact]
    public async Task Publish_OwnPhotoAlone_IsEnoughProof()
    {
        // References were dropped and work photos are optional: the own photo is the whole setup checklist.
        var draft = TestData.ReadyDraft();
        draft.WorkPhotoIds = [];
        var app = await FakeApp.ProAsync(draft);

        app.Pro.Draft.HasProof.Should().BeTrue();
        (await app.Pro.PublishAsync()).Should().Be(PublishResult.Published);
    }

    // A Google account with no phone at the interview links its number before publishing: the account's number is
    // taken then, and nothing on the phone can block the publish on it any more.
    [Fact]
    public async Task Publish_NumberLinkedAfterTheInterview_PublishesWithThatNumber()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft(), TestData.Worker(phone: null));
        app.Identity.User = TestData.Worker(phone: TestData.WorkerNumber);

        (await app.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        app.Profiles.Published.Should().ContainSingle().Which.MobileNumber.Should().Be(TestData.WorkerNumber);
    }

    [Theory]
    [InlineData(PublishResult.PhoneTaken)]
    [InlineData(PublishResult.PhotoInUse)]
    public async Task Publish_ServerRefuses_PassesReasonOnAndStaysUnpublished(PublishResult serverAnswer)
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Profiles.NextResult = serverAnswer;

        var result = await app.Pro.PublishAsync();

        result.Should().Be(serverAnswer);
        app.Pro.IsPublished.Should().BeFalse();
    }
}
