using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Account;

/// <summary>
/// The person's account on both sides: their language, the profile backed up on the server, where sign-in lands,
/// and Start over. Start over must remove everything they made (privacy) and leave the app able to start fresh;
/// a profile save that fails offline must never block onboarding.
/// </summary>
public class AccountTests
{
    [Fact]
    public async Task SetLanguage_SavesItOnThePhoneAndTheServer_AndRefreshesTheShell()
    {
        var app = new FakeApp(TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()), TestData.SeekerUser());
        var changed = 0;
        app.Account.Changed += () => changed++;

        await app.Account.SetLanguageAsync(AppLanguage.Taglish);

        app.Account.Language.Should().Be(AppLanguage.Taglish);
        app.Store.State.Language.Should().Be(AppLanguage.Taglish);
        app.Users.Profiles[TestData.SeekerUid].Language.Should().Be(AppLanguage.Taglish);
        changed.Should().Be(1);
    }

    [Fact]
    public async Task SaveProfile_Worker_SavesAccountDetailsAndListingPhoto()
    {
        var draft = TestData.ReadyDraft();
        var app = new FakeApp(TestData.FinishedPro(draft), TestData.Worker());

        await app.Account.SaveProfileAsync();

        var profile = app.Users.Profiles[TestData.WorkerUid];
        profile.DisplayName.Should().Be("Juan Dela Cruz");
        profile.PhoneNumber.Should().Be(TestData.WorkerNumber);
        profile.Role.Should().Be(UserRole.Professional);
        profile.ProfilePhotoId.Should().Be(TestData.PhotoId(1));
    }

    [Fact]
    public async Task SaveProfile_Seeker_SavesTheirOwnPhoto_AndKeepsTheFirstSaveTime()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.ProfilePhotoId = TestData.PhotoId(9);
        var app = new FakeApp(TestData.FinishedSeeker(answers), TestData.SeekerUser());
        var firstSave = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        app.Users.Profiles[TestData.SeekerUid] = new UserProfile(TestData.SeekerUid, null, null, null, AppLanguage.English, null, firstSave, firstSave);

        await app.Account.SaveProfileAsync();

        var profile = app.Users.Profiles[TestData.SeekerUid];
        profile.ProfilePhotoId.Should().Be(TestData.PhotoId(9));
        profile.CreatedAt.Should().Be(firstSave);
        profile.UpdatedAt.Should().BeAfter(firstSave);
    }

    [Fact]
    public async Task SaveProfile_NoLanguageSavedYet_UsesTheAppLanguage()
    {
        var app = new FakeApp(new OnboardingState { Role = UserRole.Seeker }, TestData.SeekerUser(), AppLanguage.Taglish);

        await app.Account.SaveProfileAsync();

        app.Users.Profiles[TestData.SeekerUid].Language.Should().Be(AppLanguage.Taglish);
    }

    [Fact]
    public async Task SaveProfile_Offline_DoesNotThrow()
    {
        var app = new FakeApp(TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()), TestData.SeekerUser());
        app.Users.FailSaves = true;

        var save = () => app.Account.SaveProfileAsync();

        await save.Should().NotThrowAsync("an offline phone must still get through onboarding");
    }

    [Fact]
    public async Task SaveProfile_NotSignedIn_DoesNothing()
    {
        var app = new FakeApp(TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()), user: null);

        await app.Account.SaveProfileAsync();

        app.Users.Profiles.Should().BeEmpty();
    }

    [Fact]
    public async Task StartOver_DeletesEverythingTheyMade_AndForgetsBothSides()
    {
        var draft = TestData.ReadyDraft();
        var state = TestData.FinishedPro(draft);
        state.Seeker.ProfilePhotoId = TestData.PhotoId(9);
        var app = new FakeApp(state, TestData.Worker());
        await app.Pro.EnsureLoadedAsync();

        await app.Account.StartOverAsync();

        app.Photos.Deleted.Should().BeEquivalentTo([TestData.PhotoId(1), TestData.PhotoId(2), TestData.PhotoId(3), TestData.PhotoId(9)]);
        app.Profiles.Removed.Should().Equal(TestData.WorkerUid);
        app.Chats.DeletedFor.Should().Equal(TestData.WorkerUid);
        app.Reviews.DeletedFor.Should().Equal(TestData.WorkerUid);
        app.Users.Deleted.Should().Equal(TestData.WorkerUid);
        app.Store.Cleared.Should().BeTrue();
        app.Pro.Draft.CategoryId.Should().BeNull("the workspace forgot the old listing");
        app.Pro.MyId.Should().Be(ListingMapper.OwnListingId);
        app.Pro.Chats.Should().BeEmpty();
    }

    [Fact]
    public async Task StartOver_NotSignedIn_CleansUpBothLocalIds()
    {
        var app = new FakeApp(TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()), user: null);

        await app.Account.StartOverAsync();

        app.Profiles.Removed.Should().Equal(ListingMapper.OwnListingId);
        app.Chats.DeletedFor.Should().Equal(ListingMapper.OwnListingId, SeekerWorkspace.LocalSeekerId);
        app.Users.Deleted.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, "/professional")]
    [InlineData(false, "/")]
    public async Task AuthLanding_WorkerWithAListing_GoesToProHome(bool hasListing, string expected)
    {
        var profiles = new FakeProfiles();
        if (hasListing)
        {
            profiles.ById[TestData.WorkerUid] = TestData.Listing("Juan");
        }

        var landing = new SideGigPhAuthLanding(profiles);

        (await landing.GetDestinationAsync(TestData.Worker())).Should().Be(expected);
    }

    [Fact]
    public void TakeAccount_NotSignedIn_KeepsTheDraftAsIs()
    {
        var draft = new ProfessionalDraft { DisplayName = "Old" };

        draft.TakeAccount(null);

        draft.DisplayName.Should().Be("Old");
    }

    [Fact]
    public async Task ProWorkspace_Load_NotAWorker_IsSentBack()
    {
        var app = new FakeApp(TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()), TestData.SeekerUser());

        (await app.Pro.EnsureLoadedAsync()).Should().BeFalse();
        app.Pro.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task ProWorkspace_DraftLoad_ReadsNothingFromTheServer_SoTheSetupShowsOnASlowConnection()
    {
        var app = new FakeApp(TestData.FinishedPro(TestData.ReadyDraft()), TestData.Worker());

        (await app.Pro.EnsureDraftLoadedAsync()).Should().BeTrue();

        app.Chats.InboxReads.Should().Be(0);
        app.Reviews.Reads.Should().Be(0);
        app.Pro.MyId.Should().Be(TestData.Worker().Uid);
    }

    [Fact]
    public async Task ProWorkspace_FullLoad_AfterTheDraft_ReadsChatsAndReviewsOnce()
    {
        var app = new FakeApp(TestData.FinishedPro(TestData.ReadyDraft()), TestData.Worker());
        await app.Pro.EnsureDraftLoadedAsync();

        await app.Pro.EnsureLoadedAsync();
        await app.Pro.EnsureLoadedAsync();

        app.Chats.InboxReads.Should().Be(1);
        app.Reviews.Reads.Should().Be(1);
    }

    [Fact]
    public async Task ProWorkspace_Load_NoSignIn_UsesTheLocalListingId()
    {
        var app = new FakeApp(TestData.FinishedPro(TestData.ReadyDraft()), user: null);

        await app.Pro.EnsureLoadedAsync();

        app.Pro.MyId.Should().Be(ListingMapper.OwnListingId);
        app.Pro.Listing.Id.Should().Be(ListingMapper.OwnListingId);
        app.Pro.JoinedAt.Should().NotBeNull();
    }
}
