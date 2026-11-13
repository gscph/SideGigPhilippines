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

    [Fact]
    public async Task Publish_OnlyOneWorkPhoto_ReturnsMissingProof()
    {
        var draft = TestData.ReadyDraft();
        draft.WorkPhotoIds = [TestData.PhotoId(2)];

        var app = await FakeApp.ProAsync(draft);

        (await app.Pro.PublishAsync()).Should().Be(PublishResult.MissingProof);
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
