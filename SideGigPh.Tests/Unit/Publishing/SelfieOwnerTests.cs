using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The server takes a new profile photo only from the account that sent it as a selfie (<c>selfie-required</c>). A
/// worker who took their selfie signed in with Google, then confirmed a number that was another account's, is signed in
/// to that account with the same code and keeps their answers: the selfie can't come along. Seen in the error reports
/// as "couldn't publish the listing: 400 selfie-required" and "It's on our side" on the Ready step.
/// </summary>
public class SelfieOwnerTests
{
    private static AuthUser PhoneAccount() => new() { Uid = "phone-account", DisplayName = "Juan", PhoneNumber = TestData.WorkerNumber };

    [Fact]
    public async Task Selfie_RemembersTheAccountThatSentIt()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Picker.Picks(50);

        await app.Pro.SetProfilePhotoAsync(await app.Picker.TakeSelfieAsync());

        app.Store.State.Professional.ProfilePhotoOwnerId.Should().Be(TestData.WorkerUid);
    }

    [Fact]
    public void TakeAccount_DropsAnotherAccountsSelfie_KeepsTheOwnAndAnUnknownOne()
    {
        var other = new ProfessionalDraft { ProfilePhotoId = TestData.PhotoId(1), ProfilePhotoOwnerId = "google-account" };
        var own = new ProfessionalDraft { ProfilePhotoId = TestData.PhotoId(2), ProfilePhotoOwnerId = TestData.WorkerUid };
        var unknown = new ProfessionalDraft { ProfilePhotoId = TestData.PhotoId(3) };

        foreach (var draft in new[] { other, own, unknown })
        {
            draft.TakeAccount(TestData.Worker());
        }

        other.ProfilePhotoId.Should().BeNull("the server refuses a selfie another account sent");
        other.ProfilePhotoOwnerId.Should().BeNull();
        own.ProfilePhotoId.Should().Be(TestData.PhotoId(2));
        unknown.ProfilePhotoId.Should().Be(TestData.PhotoId(3), "a listing brought back from the server keeps its photo; the server decides");
    }

    [Fact]
    public async Task AccountSwitched_DropsTheLastAccountsSelfie_SoTheInterviewEndsOnTheSelfieStep()
    {
        // The trail of the report: selfie as the Google account, the number step finds the number on another account.
        var draft = TestData.ReadyDraft();
        draft.ProfilePhotoId = null;
        var app = new FakeApp(new OnboardingState { Role = UserRole.Professional, Professional = draft }, TestData.Worker(phone: null));
        await app.Pro.LoadInterviewDraftAsync();
        app.Picker.Picks(50);
        await app.Pro.SetProfilePhotoAsync(await app.Picker.TakeSelfieAsync());

        app.Identity.User = PhoneAccount();
        await app.Account.AccountSwitchedAsync();

        var saved = app.Store.State.Professional;
        saved.ProfilePhotoId.Should().BeNull();
        saved.CategoryId.Should().NotBeNull("the answers stay on the phone");
        saved.AfterInterviewPath().Should().Be(OnboardingSteps.PathOf(ProStep.Photo), "this account takes its own selfie");
        app.Photos.Deleted.Should().BeEmpty("the other account's photo isn't this account's to delete");
    }

    [Fact]
    public async Task Publish_AfterSigningInToAnotherAccount_AsksForASelfie_AndSendsNothing()
    {
        // The other way to the same place: sign out, then sign in with the number ("Sign in with this number").
        var draft = TestData.ReadyDraft();
        draft.ProfilePhotoOwnerId = TestData.WorkerUid;
        var app = new FakeApp(TestData.FinishedPro(draft), PhoneAccount());
        await app.Pro.EnsureDraftLoadedAsync();

        (await app.Pro.PublishAsync()).Should().Be(PublishResult.MissingProof);

        app.Profiles.Published.Should().BeEmpty("the server would refuse it");
        app.Pro.Draft.AfterInterviewPath().Should().Be(OnboardingSteps.PathOf(ProStep.Photo));
    }

    [Fact]
    public async Task Publish_RefusedForTheSelfie_DropsIt_SoTheSelfieStepAsksAgain()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Profiles.NextResult = PublishResult.SelfieNeeded;

        (await app.Pro.PublishAsync()).Should().Be(PublishResult.SelfieNeeded);

        app.Store.State.Professional.ProfilePhotoId.Should().BeNull();
        app.Pro.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task LiveListingSync_RefusedForTheSelfie_IsNotSentAgainOnEveryRefresh()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);
        app.Profiles.NextResult = PublishResult.SelfieNeeded;

        (await app.Pro.ListingEditedAsync()).Should().Be(PublishResult.SelfieNeeded);
        (await app.Pro.SyncListingAsync()).Should().BeNull("nothing is sent until a new selfie is taken");

        app.Profiles.Published.Should().ContainSingle();
        app.Pro.Draft.HasProfilePhoto.Should().BeFalse("the profile shows its own photo missing");
        app.Pro.Draft.ListingOutOfDate.Should().BeTrue("the new selfie re-publishes the edit");
    }
}
