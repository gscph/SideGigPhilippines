using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>The busy splash speaks to the user's side: Seekers see the honorifics take turns, Skilled Workers see their own lines.</summary>
public class SplashLinesTests
{
    [Fact]
    public async Task Seeker_SeesTheBrandLineWithEveryHonorific()
    {
        var lines = await new RoleSplashLines(new FakeOnboardingStore(new OnboardingState { Role = UserRole.Seeker })).LinesAsync();

        lines.Select(line => line.Taglish).Where(line => line != Copy.TrustTagline.Taglish)
            .Should().Equal(Honorifics.All.Select(h => $"May {h} para diyan"));
        lines.Should().Contain(line => line.English == "Find Ates nearby");
    }

    [Fact]
    public async Task Seeker_SeesTheTrustLine_Second()
    {
        // Brief item 16. Second, so a wait of two lines already shows it.
        var lines = await new RoleSplashLines(new FakeOnboardingStore(new OnboardingState { Role = UserRole.Seeker })).LinesAsync();

        lines[1].Should().Be(Copy.TrustTagline);
        lines.Should().ContainSingle(line => line == Copy.TrustTagline);
    }

    [Fact]
    public async Task Professional_SeesTheWorkerLines()
    {
        var lines = await new RoleSplashLines(new FakeOnboardingStore(new OnboardingState { Role = UserRole.Professional })).LinesAsync();

        lines.Should().BeEquivalentTo(RoleSplashLines.ProLines).And.NotBeEmpty();
        lines.Should().NotIntersectWith(RoleSplashLines.SeekerLines);
    }

    [Fact]
    public async Task NoRoleYet_SeesTheSeekerLines_LikeTheLaunchSplash()
    {
        var lines = await new RoleSplashLines(new FakeOnboardingStore(new OnboardingState())).LinesAsync();

        lines.Should().Equal(RoleSplashLines.SeekerLines);
    }
}
