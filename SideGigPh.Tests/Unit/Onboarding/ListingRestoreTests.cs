using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// A published Skilled Worker who reinstalls the app (or signs in on a new phone) lands on Pro home with their live
/// listing, not in the interview again. The restore never overwrites answers already on the phone, never breaks launch,
/// and leaves a Seeker (no listing) on the role question as before.
/// </summary>
public class ListingRestoreTests
{
    // Just past sign-in on a fresh install: the language is picked, nothing else.
    private static OnboardingState FreshInstall() => new() { Language = AppLanguage.Taglish };

    private static (ListingRestore Restore, FakeApp App) Make(OnboardingState state, AuthUser? user, ProfessionalDraft? live = null)
    {
        var app = new FakeApp(state, user);
        if (live is not null)
        {
            app.Profiles.OwnDrafts[TestData.WorkerUid] = live;
        }

        return (new ListingRestore(app.Store, app.Identity, app.Profiles, NullLogger<ListingRestore>.Instance), app);
    }

    [Fact]
    public async Task PublishedWorker_AfterAReinstall_LandsOnProHome_WithTheLiveListing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Restored);

        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home);
        state.Role.Should().Be(UserRole.Professional);
        state.CompletedAt.Should().NotBeNull();
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
    public async Task Seeker_OrAWorkerWithNoListing_StaysOnTheRoleQuestion()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.SeekerUser());

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Unchanged);

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        state.Role.Should().BeNull();
        app.Store.Saves.Should().Be(0);
    }

    [Fact]
    public async Task AReadWithNoAnswer_IsOffline_NotANewWorker()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker());
        app.Profiles.OwnDraftThrows = new HttpRequestException("No internet");

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Offline,
            "with no answer nobody knows whether a live listing exists: launch asks to try again");

        state.Role.Should().BeNull();
        app.Store.Saves.Should().Be(0);
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.InternalServerError)]
    [InlineData(System.Net.HttpStatusCode.Unauthorized)]
    public async Task AServerError_FallsBackToTheRoleQuestion_WithoutThrowing(System.Net.HttpStatusCode status)
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker());
        app.Profiles.OwnDraftThrows = new HttpRequestException("server", null, status);

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Unchanged, "a broken server must not lock the app");

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        app.Store.Saves.Should().Be(0);
    }

    [Fact]
    public async Task ABrokenAdapter_FallsBackToTheRoleQuestion_WithoutThrowing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker());
        app.Profiles.OwnDraftThrows = new InvalidOperationException("bug");

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Unchanged);

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
    }

    [Fact]
    public async Task ASidePickedOnThisPhone_IsNeverOverwritten_NorAsked()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        draft.ListingOutOfDate = true; // an edit not sent yet
        draft.ServiceIds = ["aircon-cleaning"];
        var state = TestData.FinishedPro(draft);
        var midInterview = new OnboardingState { Language = AppLanguage.English, Role = UserRole.Professional, ResumePath = "/start/pro/2" };
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Unchanged);
        (await restore.RestoreAsync(midInterview)).Should().Be(ListingRestoreResult.Unchanged);

        state.Professional.Should().BeSameAs(draft);
        draft.ServiceIds.Should().Equal("aircon-cleaning");
        draft.ListingOutOfDate.Should().BeTrue();
        midInterview.CompletedAt.Should().BeNull();
        app.Profiles.OwnDraftReads.Should().BeEmpty("no network call once a side is picked");
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

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Restored);

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

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Unchanged);

        state.Professional.CategoryId.Should().Be("plumbing");
        state.CompletedAt.Should().BeNull();
        app.Profiles.OwnDraftReads.Should().BeEmpty();
    }

    [Fact]
    public async Task SeekerSidePickedWithNothingAnswered_IsLeftAlone()
    {
        var state = new OnboardingState { Language = AppLanguage.English, Role = UserRole.Seeker, ResumePath = "/start/seeker/1" };
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Unchanged);

        state.Role.Should().Be(UserRole.Seeker);
        app.Profiles.OwnDraftReads.Should().BeEmpty();
    }

    [Fact]
    public async Task NobodySignedIn_RestoresNothing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, user: null, TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().Be(ListingRestoreResult.Unchanged);

        app.Profiles.OwnDraftReads.Should().BeEmpty();
        state.Role.Should().BeNull();
    }
}
