using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>The busy splash opens on the ID-check line, then speaks to the user's side: Seekers see the honorifics take turns, Skilled Workers see their own lines.</summary>
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

    [Theory]
    [InlineData(UserRole.Seeker)]
    [InlineData(UserRole.Professional)]
    public async Task EveryoneSeesTheIdCheckLine_First_AndOnlyOnce(UserRole role)
    {
        // The main splash line: the busy splash always opens on it and holds it longest (BusyOverlay.FirstLineFor).
        var lines = await new RoleSplashLines(new FakeOnboardingStore(new OnboardingState { Role = role })).LinesAsync();

        lines[0].Should().Be(Copy.TrustTagline);
        lines.Should().ContainSingle(line => line == Copy.TrustTagline);
    }

    [Fact]
    public async Task Professional_SeesTheWorkerLines()
    {
        var lines = await new RoleSplashLines(new FakeOnboardingStore(new OnboardingState { Role = UserRole.Professional })).LinesAsync();

        lines.Skip(1).Should().Equal(Copy.ProTaglines);
        lines.Skip(1).Should().NotIntersectWith(RoleSplashLines.SeekerLines);
    }

    [Fact]
    public async Task NoRoleYet_SeesTheSeekerLines_LikeTheLaunchSplash()
    {
        var lines = await new RoleSplashLines(new FakeOnboardingStore(new OnboardingState())).LinesAsync();

        lines.Should().Equal(RoleSplashLines.SeekerLines);
    }
}
