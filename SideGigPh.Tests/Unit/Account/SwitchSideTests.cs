using System.Text.Json;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Local;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Account;

/// <summary>
/// One account can be a household and a Skilled Worker, switched on the Me page (<see cref="SideSwitch"/>,
/// <see cref="LocalAccount.SwitchSideAsync"/>): each side keeps its answers and its finished interview.
/// </summary>
public class SwitchSideTests
{
    private static OnboardingState LivePro()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        return TestData.FinishedPro(draft);
    }

    [Fact]
    public async Task LiveWorker_SwitchesToCustomer_StartsTheHouseholdInterview_AndKeepsTheListing()
    {
        var state = LivePro();
        var app = new FakeApp(state, TestData.Worker());

        var path = await app.Account.SwitchSideAsync(UserRole.Seeker);

        path.Should().Be(OnboardingSteps.FirstPath(UserRole.Seeker));
        state.Role.Should().Be(UserRole.Seeker);
        state.ResumePath.Should().Be(path, "a closed app reopens the household interview");
        state.HasFinished(UserRole.Professional).Should().BeTrue("the worker side stays set up");
        state.Professional.PublishedAt.Should().NotBeNull();
        OnboardingSteps.LaunchPath(state).Should().Be(path);
        app.Profiles.Removed.Should().BeEmpty("switching never takes the listing down");
    }

    [Fact]
    public async Task BothSidesSetUp_SwitchingBackAndForth_OpensEachHome_WithoutQuestions()
    {
        var state = LivePro();
        state.Seeker = TestData.AirconCleaningInTaytay();
        state.SeekerFinishedAt = DateTimeOffset.Now;
        var app = new FakeApp(state, TestData.Worker());

        (await app.Account.SwitchSideAsync(UserRole.Seeker)).Should().Be(SeekerRoutes.Home);
        OnboardingSteps.LaunchPath(state).Should().Be(SeekerRoutes.Home, "the app now opens on the household side");
        (await app.Account.SwitchSideAsync(UserRole.Professional)).Should().Be(ProRoutes.Home);
        OnboardingSteps.LaunchPath(state).Should().Be(ProRoutes.Home);
        state.ResumePath.Should().BeNull();
    }

    [Fact]
    public async Task BothSidesSetUp_BothWorkspacesLoad_WhicheverSideIsOpen()
    {
        var state = LivePro();
        state.Seeker = TestData.AirconCleaningInTaytay();
        state.SeekerFinishedAt = DateTimeOffset.Now;
        state.Role = UserRole.Seeker;
        var app = new FakeApp(state, TestData.Worker());

        (await app.Seeker.EnsureLoadedAsync()).Should().BeTrue();
        (await app.Pro.EnsureLoadedAsync()).Should().BeTrue("a notification or Back can open the other side's pages");
        app.Seeker.MyId.Should().Be(TestData.WorkerUid);
        app.Pro.MyId.Should().Be(TestData.WorkerUid, "one account: the same uid on both sides");
    }

    [Fact]
    public async Task Customer_SwitchesToWorker_StartsTheWorkerInterview_AndKeepsTheirSearch()
    {
        var state = TestData.FinishedSeeker(TestData.AirconCleaningInTaytay());
        var app = new FakeApp(state, TestData.SeekerUser());

        var path = await app.Account.SwitchSideAsync(UserRole.Professional);

        path.Should().Be(OnboardingSteps.FirstPath(UserRole.Professional));
        state.HasFinished(UserRole.Seeker).Should().BeTrue();
        state.Seeker.ServiceId.Should().Be("aircon-cleaning");
        (await app.Pro.EnsureLoadedAsync()).Should().BeFalse("Pro home waits for the finished worker interview");
    }

    [Fact]
    public async Task Switch_SavesTheSideOnTheServer()
    {
        var app = new FakeApp(LivePro(), TestData.Worker());

        await app.Account.SwitchSideAsync(UserRole.Seeker);

        app.Users.Profiles[TestData.WorkerUid].Role.Should().Be(UserRole.Seeker);
    }

    [Fact]
    public async Task SaveProfile_NewSideWithoutAPhoto_KeepsTheOtherSidesPhoto()
    {
        var state = LivePro();
        state.Role = UserRole.Seeker;
        var app = new FakeApp(state, TestData.Worker());

        await app.Account.SaveProfileAsync();

        app.Users.Profiles[TestData.WorkerUid].ProfilePhotoId.Should().Be(state.Professional.ProfilePhotoId,
            "switching side must not wipe the account's photo");
    }

    [Fact]
    public void FinishedWorkerNotLiveYet_SwitchingBack_GoesWhereTheyLeftIt()
    {
        var draft = TestData.ReadyDraft();
        draft.ProfilePhotoId = null;
        var state = TestData.FinishedPro(draft);
        state.Role = UserRole.Seeker;

        SideSwitch.MoveTo(state, UserRole.Professional).Should().Be(draft.AfterInterviewPath());
    }

    [Theory]
    [InlineData("seeker", UserRole.Seeker)]
    [InlineData("professional", UserRole.Professional)]
    public void SwitchPath_RoundTrips(string side, UserRole role)
    {
        SideSwitch.PathTo(role).Should().Be($"/switch/{side}");
        SideSwitch.Parse(side).Should().Be(role);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("5")]
    public void SwitchPath_Unknown_IsNull(string? side) => SideSwitch.Parse(side).Should().BeNull();

    // --- Files saved before one account could be both sides --------------------------------------------------

    [Theory]
    [InlineData("Seeker", UserRole.Seeker)]
    [InlineData("Professional", UserRole.Professional)]
    public void OldFile_FinishDate_BecomesThatSidesDate(string savedRole, UserRole role)
    {
        var json = $$"""{ "Language": "English", "Role": "{{savedRole}}", "CompletedAt": "2026-06-01T08:00:00+08:00" }""";

        var state = JsonSerializer.Deserialize(json, OnboardingJsonContext.Default.OnboardingState)!.FitToCatalog();

        state.HasFinished(role).Should().BeTrue("a finished interview is never asked again");
        state.HasFinished(role.Other()).Should().BeFalse();
        state.CompletedAt.Should().BeNull();
        JsonSerializer.Serialize(state, OnboardingJsonContext.Default.OnboardingState).Should().NotContain("CompletedAt");
    }

    [Fact]
    public void OldFile_FinishDateWithoutASide_IsDropped()
    {
        var state = new OnboardingState { CompletedAt = DateTimeOffset.Now }.FitToCatalog();

        state.SeekerFinishedAt.Should().BeNull();
        state.ProFinishedAt.Should().BeNull();
        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.RolePath);
    }
}
