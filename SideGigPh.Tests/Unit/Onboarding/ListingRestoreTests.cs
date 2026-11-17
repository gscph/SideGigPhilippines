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

        (await restore.RestoreAsync(state)).Should().BeTrue();

        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home);
        state.Role.Should().Be(UserRole.Professional);
        state.CompletedAt.Should().NotBeNull();
        state.Professional.PublishedAt.Should().NotBeNull();
        state.Professional.ListingOutOfDate.Should().BeFalse("the phone holds exactly what is live");
        state.Professional.IsListingComplete.Should().BeTrue();
        state.Professional.HasProof.Should().BeTrue("the private references came back too, so edits can re-publish");
        state.Professional.DisplayName.Should().Be("Juan Dela Cruz", "name and number come from the account");
        state.Professional.MobileNumber.Should().Be(TestData.WorkerNumber);
        app.Store.State.Should().BeSameAs(state);
        app.Store.Saves.Should().BeGreaterThan(0, "saved, so the next launch opens Pro home with no network call");
    }

    [Fact]
    public async Task RestoredWorker_OpensProHome_AndAnEditRePublishesWithTheReferences()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker(), TestData.ReadyDraft());
        await restore.RestoreAsync(state);

        (await app.Pro.EnsureLoadedAsync()).Should().BeTrue("Pro home loads the restored listing");
        app.Pro.IsPublished.Should().BeTrue();
        app.Pro.WorkPhotos.Should().HaveCount(2);
        (await app.Pro.ListingEditedAsync()).Should().Be(PublishResult.Published);

        app.Profiles.Published.Should().ContainSingle().Which.References.Should().ContainSingle(r => r.MobileNumber == TestData.ReferenceNumber);
    }

    [Fact]
    public async Task Seeker_OrAWorkerWithNoListing_StaysOnTheRoleQuestion()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.SeekerUser());

        (await restore.RestoreAsync(state)).Should().BeFalse();

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        state.Role.Should().BeNull();
        app.Store.Saves.Should().Be(0);
    }

    [Fact]
    public async Task AFailedRead_FallsBackToTheRoleQuestion_WithoutThrowing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, TestData.Worker());
        app.Profiles.OwnDraftThrows = new HttpRequestException("offline");

        (await restore.RestoreAsync(state)).Should().BeFalse();

        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
        app.Store.Saves.Should().Be(0);
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

        (await restore.RestoreAsync(state)).Should().BeFalse();
        (await restore.RestoreAsync(midInterview)).Should().BeFalse();

        state.Professional.Should().BeSameAs(draft);
        draft.ServiceIds.Should().Equal("aircon-cleaning");
        draft.ListingOutOfDate.Should().BeTrue();
        midInterview.CompletedAt.Should().BeNull();
        app.Profiles.OwnDraftReads.Should().BeEmpty("no network call once a side is picked");
    }

    [Fact]
    public async Task NobodySignedIn_RestoresNothing()
    {
        var state = FreshInstall();
        var (restore, app) = Make(state, user: null, TestData.ReadyDraft());

        (await restore.RestoreAsync(state)).Should().BeFalse();

        app.Profiles.OwnDraftReads.Should().BeEmpty();
        state.Role.Should().BeNull();
    }
}
