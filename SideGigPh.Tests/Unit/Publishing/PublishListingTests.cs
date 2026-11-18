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
    public async Task Publish_SendsAccountNameNumberPhotosAndReferences()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());

        await app.Pro.PublishAsync();

        var (listing, references) = app.Profiles.Published.Single();
        listing.Id.Should().Be(TestData.WorkerUid, "the listing id is the signed-in uid");
        listing.DisplayName.Should().Be("Juan Dela Cruz");
        listing.MobileNumber.Should().Be(TestData.WorkerNumber);
        listing.PhotoUrl.Should().EndWith($"{TestData.PhotoId(1)}.jpg");
        listing.WorkPhotoUrls.Should().HaveCount(2);
        references.Should().ContainSingle().Which.MobileNumber.Should().Be(TestData.ReferenceNumber);
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
        app.Profiles.Published.Should().ContainSingle().Which.Listing.WorkPhotoUrls.Should().HaveCount(count);
    }

    [Fact]
    public async Task Publish_NoReference_ReturnsMissingProof()
    {
        var draft = TestData.ReadyDraft();
        draft.References = [];

        var app = await FakeApp.ProAsync(draft);

        (await app.Pro.PublishAsync()).Should().Be(PublishResult.MissingProof);
    }

    // The bug that started this test project: references were added while a Google account had no phone yet,
    // then the worker linked that same number. The server refused the listing and the worker was stuck.
    [Fact]
    public async Task Publish_ReferenceHasWorkersOwnNumber_ReturnsBadReferenceWithoutSending()
    {
        // Arrange: a Google account with no phone added this reference...
        var draft = TestData.ReadyDraft();
        draft.References = [new WorkReference("Me again", TestData.WorkerNumber)];
        var app = await FakeApp.ProAsync(draft, TestData.Worker(phone: null));

        // ...then linked that very number before publishing.
        app.Identity.User = TestData.Worker(phone: TestData.WorkerNumber);

        // Act
        var result = await app.Pro.PublishAsync();

        // Assert
        result.Should().Be(PublishResult.BadReference);
        app.Profiles.Published.Should().BeEmpty("the server would refuse it anyway");
        app.Pro.IsPublished.Should().BeFalse();
    }

    [Theory]
    [InlineData(PublishResult.PhoneTaken)]
    [InlineData(PublishResult.BadReference)]
    public async Task Publish_ServerRefuses_PassesReasonOnAndStaysUnpublished(PublishResult serverAnswer)
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Profiles.NextResult = serverAnswer;

        var result = await app.Pro.PublishAsync();

        result.Should().Be(serverAnswer);
        app.Pro.IsPublished.Should().BeFalse();
    }
}
