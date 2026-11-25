using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// Someone who reinstalls the app (or signs in on a new phone) comes back as they were: a published Skilled Worker on
/// Pro home with their live listing, a household on Seeker home with its town, someone on both sides with both, on the
/// side they last used. The restore never overwrites answers already on the phone, never breaks launch, and leaves a new
/// account on the role question.
/// </summary>
public class AccountRestoreTests
{
    private static readonly DateTimeOffset LastWeek = DateTimeOffset.Now.AddDays(-7);

    // Just past sign-in on a fresh install: the language is picked, nothing else.
    private static OnboardingState FreshInstall() => new() { Language = AppLanguage.Taglish };

    private static (AccountRestore Restore, FakeApp App) Make(
        OnboardingState state, AuthUser? user, ProfessionalDraft? live = null, UserProfile? profile = null)
    {
        var app = new FakeApp(state, user);
        if (live is not null)
        {
            app.Profiles.OwnDrafts[TestData.WorkerUid] = live;
        }

        if (profile is not null)
        {
            app.Users.Profiles[profile.Uid] = profile;
        }

        return (new AccountRestore(app.Store, app.Identity, app.Profiles, app.Users, NullLogger<AccountRestore>.Instance), app);
    }

    // The server's copy of an account: the side last used and, once the household interview was finished, its home.
    private static UserProfile Saved(string uid, UserRole? role, DateTimeOffset? seekerFinishedAt = null, string? town = null, string? barangay = null) =>
        new(uid, "Name", null, null, AppLanguage.English, role, DateTimeOffset.Now, DateTimeOffset.Now,
            SeekerFinishedAt: seekerFinishedAt, HomeTownId: town, HomeBarangayId: barangay);

    // --- Skilled Worker ---------------------------------------------------------

    [Fact]
    public async Task PublishedWorker_AfterAReinstall_LandsOnProHome_WithTheLiveListing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home);
        state.Role.Should().Be(UserRole.Professional);
        state.ProFinishedAt.Should().NotBeNull();
        state.SeekerFinishedAt.Should().BeNull("no household side on the server");
        state.Professional.PublishedAt.Should().NotBeNull();
        state.Professional.ListingOutOfDate.Should().BeFalse("the phone holds exactly what is live");
        state.Professional.IsListingComplete.Should().BeTrue();
        state.Professional.HasProof.Should().BeTrue("the photos came back too, so edits can re-publish");
        state.Professional.DisplayName.Should().Be("Juan Dela Cruz", "name and number come from the account");
        state.Professional.MobileNumber.Should().Be(TestData.WorkerNumber);
        app.Store.State.Should().BeSameAs(state);
        app.Store.Saves.Should().BeGreaterThan(0, "saved, so the next launch opens Pro home with no network call");
    }

    [Fact]
    public async Task RestoredWorker_OpensProHome_AndAnEditRePublishesTheListing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());
        await restore.RestoreAsync(state);

        (await app.Pro.EnsureLoadedAsync()).Should().BeTrue("Pro home loads the restored listing");
        app.Pro.IsPublished.Should().BeTrue();
        app.Pro.WorkPhotos.Should().HaveCount(2);
        (await app.Pro.ListingEditedAsync()).Should().Be(PublishResult.Published);

        app.Profiles.Published.Should().ContainSingle().Which.WorkPhotoUrls.Should().HaveCount(2);
    }

    [Fact]
    public async Task LiveWorker_WithAnAnswerTheServerDoesntKeep_LandsOnProHome()
    {
        var live = TestData.ReadyDraft();
        live.ExperienceId = null;
        var state = FreshInstall();
        var (restore, _) = Make(state, TestData.Worker(), live);

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home, "a live listing never sends the worker back into the interview");
    }

    [Fact]
    public async Task NewAccount_WithNoListingNorHousehold_StaysOnTheRoleQuestion()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.SeekerUser());

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        state.Role.Should().BeNull();
        app.Store.Saves.Should().Be(0);
    }

    // --- Household ---------------------------------------------------------------

    [Fact]
    public async Task Household_AfterAReinstall_LandsOnSeekerHome_WithItsTown()
    {
        var barangay = ReferenceData.BarangaysOf("taytay")[0].Id;
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.SeekerUser(),
            profile: Saved(TestData.SeekerUid, UserRole.Seeker, LastWeek, "taytay", barangay) with { ProfilePhotoId = "photo-1" });

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        OnboardingSteps.LaunchPath(state).Should().Be(SeekerRoutes.Home, "no role question, no interview again");
        state.Role.Should().Be(UserRole.Seeker);
        state.SeekerFinishedAt.Should().Be(LastWeek);
        state.ProFinishedAt.Should().BeNull();
        state.Seeker.CityId.Should().Be("taytay");
        state.Seeker.AreaId.Should().Be(ReferenceData.ServiceAreaId);
        state.Seeker.BarangayId.Should().Be(barangay);
        state.Seeker.ProfilePhotoId.Should().Be("photo-1");
        state.Seeker.TownShown().Should().Be("taytay");
        app.Store.Saves.Should().BeGreaterThan(0);
        (await app.Seeker.EnsureLoadedAsync()).Should().BeTrue("Seeker home loads the restored household");
    }

    [Fact]
    public async Task Household_WhoseTownIsNoLongerServed_IsAskedTheTownOnly()
    {
        var state = FreshInstall();
        var (restore, _) = Make(state, TestData.SeekerUser(),
            profile: Saved(TestData.SeekerUid, UserRole.Seeker, LastWeek, "makati", "makati-poblacion"));

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        state.Seeker.CityId.Should().BeNull();
        state.Seeker.BarangayId.Should().BeNull();
        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.SeekerTownPath);
    }

    [Fact]
    public async Task Household_WithABarangayOfAnotherTown_KeepsTheTownOnly()
    {
        var state = FreshInstall();
        var (restore, _) = Make(state, TestData.SeekerUser(),
            profile: Saved(TestData.SeekerUid, UserRole.Seeker, LastWeek, "angono", ReferenceData.BarangaysOf("taytay")[0].Id));

        await restore.RestoreAsync(state);

        state.Seeker.CityId.Should().Be("angono");
        state.Seeker.BarangayId.Should().BeNull();
    }

    [Fact]
    public async Task ProfileWithoutAFinishedHousehold_RestoresNothing()
    {
        // A side picked, the interview never finished: nothing to bring back, the role question as on a new phone.
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.SeekerUser(), profile: Saved(TestData.SeekerUid, UserRole.Seeker, town: "taytay"));

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        app.Store.Saves.Should().Be(0);
    }

    // --- Both sides ----------------------------------------------------------------

    [Fact]
    public async Task BothSides_LastUsedAsHousehold_OpenOnTheHousehold_AndSwitchingOpensProHome()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft(),
            Saved(TestData.WorkerUid, UserRole.Seeker, LastWeek, "angono"));

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        state.Role.Should().Be(UserRole.Seeker, "the side they last used");
        state.HasFinished(UserRole.Seeker).Should().BeTrue();
        state.HasFinished(UserRole.Professional).Should().BeTrue();
        OnboardingSteps.LaunchPath(state).Should().Be(SeekerRoutes.Home);
        SideSwitch.MoveTo(state, UserRole.Professional).Should().Be(ProRoutes.Home, "the worker side is set up too");
        SideSwitch.MoveTo(state, UserRole.Seeker).Should().Be(SeekerRoutes.Home);
        app.Store.Saves.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BothSides_LastUsedAsWorker_OpenOnProHome()
    {
        var state = FreshInstall();
        var (restore, _) = Make(state, TestData.Worker(), TestData.ReadyDraft(),
            Saved(TestData.WorkerUid, UserRole.Professional, LastWeek, "angono"));

        await restore.RestoreAsync(state);

        state.Role.Should().Be(UserRole.Professional);
        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home);
        SideSwitch.MoveTo(state, UserRole.Seeker).Should().Be(SeekerRoutes.Home, "the household side is set up, not asked again");
    }

    [Fact]
    public async Task LastSideNotRestored_OpensTheSideThatCameBack()
    {
        // Last used as a worker who never went live: only the household comes back, so the app opens on it.
        var state = FreshInstall();
        var (restore, _) = Make(state, TestData.Worker(), profile: Saved(TestData.WorkerUid, UserRole.Professional, LastWeek, "angono"));

        await restore.RestoreAsync(state);

        state.Role.Should().Be(UserRole.Seeker);
        OnboardingSteps.LaunchPath(state).Should().Be(SeekerRoutes.Home);
    }

    // --- No answer, server errors ------------------------------------------------------

    [Fact]
    public async Task AListingReadWithNoAnswer_IsOffline_NotANewWorker()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker());
        app.Profiles.OwnDraftThrows = new HttpRequestException("No internet");

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Offline,
            "with no answer nobody knows whether a live listing exists: launch asks to try again");

        state.Role.Should().BeNull();
        app.Store.Saves.Should().Be(0);
        app.Users.Reads.Should().ContainSingle("both reads go out together");
    }

    [Fact]
    public async Task AProfileReadWithNoAnswer_IsOffline_NotANewHousehold()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());
        app.Users.GetThrows = new HttpRequestException("No internet");

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Offline,
            "with no answer nobody knows whether the household is set up: launch asks to try again");

        state.Role.Should().BeNull();
        app.Store.Saves.Should().Be(0);
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.InternalServerError)]
    [InlineData(System.Net.HttpStatusCode.Unauthorized)]
    public async Task AListingServerError_FallsBackToTheRoleQuestion_WithoutThrowing(System.Net.HttpStatusCode status)
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker());
        app.Profiles.OwnDraftThrows = new HttpRequestException("server", null, status);

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged, "a broken server must not lock the app");

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        app.Store.Saves.Should().Be(0);
    }

    [Fact]
    public async Task AProfileServerError_IsUnchanged_WithoutThrowing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.SeekerUser());
        app.Users.GetThrows = new HttpRequestException("server", null, System.Net.HttpStatusCode.InternalServerError);

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged, "a broken server must not lock the app");

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        app.Store.Saves.Should().Be(0);
    }

    [Fact]
    public async Task AProfileServerError_StillBringsTheLiveListingBack()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());
        app.Users.GetThrows = new HttpRequestException("server", null, System.Net.HttpStatusCode.InternalServerError);

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home);
    }

    [Fact]
    public async Task ABrokenAdapter_FallsBackToTheRoleQuestion_WithoutThrowing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker());
        app.Profiles.OwnDraftThrows = new InvalidOperationException("bug");

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
    }

    // --- Answers on the phone are never overwritten -----------------------------------------

    [Fact]
    public async Task ASidePickedOnThisPhone_IsNeverOverwritten_NorAsked()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        draft.ListingOutOfDate = true; // an edit not sent yet
        draft.ServiceIds = ["aircon-cleaning"];
        var state = TestData.FinishedPro(draft);
        var midInterview = new OnboardingState { Language = AppLanguage.English, Role = UserRole.Professional, ResumePath = "/start/pro/2" };
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft(), Saved(TestData.WorkerUid, UserRole.Seeker, LastWeek, "angono"));

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);
        (await restore.RestoreAsync(midInterview)).Should().Be(AccountRestoreResult.Unchanged);

        state.Professional.Should().BeSameAs(draft);
        draft.ServiceIds.Should().Equal("aircon-cleaning");
        draft.ListingOutOfDate.Should().BeTrue();
        state.SeekerFinishedAt.Should().BeNull();
        midInterview.ProFinishedAt.Should().BeNull();
        app.Profiles.OwnDraftReads.Should().BeEmpty("no network call once a side is picked");
        app.Users.Reads.Should().BeEmpty();
    }

    [Fact]
    public async Task AnsweredHouseholdOnThisPhone_IsNeverOverwritten_NorAsked()
    {
        var answers = TestData.AirconCleaningInTaytay();
        var state = new OnboardingState { Language = AppLanguage.English, Role = UserRole.Seeker, SeekerFinishedAt = DateTimeOffset.Now, Seeker = answers };
        var (restore, app) = Make(state, TestData.SeekerUser(), profile: Saved(TestData.SeekerUid, UserRole.Seeker, LastWeek, "angono"));

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        state.Seeker.Should().BeSameAs(answers);
        state.Seeker.CityId.Should().Be("taytay");
        app.Users.Reads.Should().BeEmpty("no network call once a side is picked");
    }

    [Fact]
    public async Task HouseholdBecomingAWorker_OnItsFirstQuestion_IsNeverOverwritten()
    {
        // "Become a Skilled Worker" on a set-up household: the worker side's first question with nothing answered.
        var answers = TestData.AirconCleaningInTaytay();
        var state = new OnboardingState
        {
            Language = AppLanguage.English,
            Role = UserRole.Professional,
            ResumePath = OnboardingSteps.FirstPath(UserRole.Professional),
            SeekerFinishedAt = DateTimeOffset.Now,
            Seeker = answers,
        };
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft(), Saved(TestData.WorkerUid, UserRole.Seeker, LastWeek, "angono"));

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        state.Seeker.CityId.Should().Be("taytay");
        app.Profiles.OwnDraftReads.Should().BeEmpty();
    }

    [Fact]
    public async Task WorkerSidePickedWithNothingAnswered_SignedInWithTheNumbersAccount_LandsOnProHome()
    {
        // The role page asked a Google sign-in for the number, it belonged to the worker's earlier phone account, and
        // "Sign in with this number" brought that account back to launch before the first question.
        var state = new OnboardingState
        {
            Language = AppLanguage.English,
            Role = UserRole.Professional,
            ResumePath = OnboardingSteps.FirstPath(UserRole.Professional),
        };
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home);
        state.ResumePath.Should().BeNull();
        state.Professional.PublishedAt.Should().NotBeNull();
        app.Store.Saves.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WorkerSideWithTheFirstAnswerGiven_IsNeverOverwritten_NorAsked()
    {
        var state = new OnboardingState
        {
            Language = AppLanguage.English,
            Role = UserRole.Professional,
            ResumePath = "/start/pro/2",
            Professional = new ProfessionalDraft { CategoryId = "plumbing" },
        };
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        state.Professional.CategoryId.Should().Be("plumbing");
        state.ProFinishedAt.Should().BeNull();
        app.Profiles.OwnDraftReads.Should().BeEmpty();
    }

    [Fact]
    public async Task SeekerSidePickedWithNothingAnswered_IsLeftAlone()
    {
        var state = new OnboardingState { Language = AppLanguage.English, Role = UserRole.Seeker, ResumePath = "/start/seeker/1" };
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        state.Role.Should().Be(UserRole.Seeker);
        app.Profiles.OwnDraftReads.Should().BeEmpty();
    }

    [Fact]
    public async Task NobodySignedIn_RestoresNothing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, user: null, TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(AccountRestoreResult.Unchanged);

        app.Profiles.OwnDraftReads.Should().BeEmpty();
        app.Users.Reads.Should().BeEmpty();
        state.Role.Should().BeNull();
    }
}
