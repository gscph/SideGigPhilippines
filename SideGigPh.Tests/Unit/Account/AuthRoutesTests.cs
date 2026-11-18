using MobileAppTheme.Auth;

namespace SideGigPh.Tests.Unit.Account;

/// <summary>
/// The add-number step's links: where it goes once done (<c>then</c>) and where its Back goes (<c>back</c>). Back must
/// not go home when home would send the person straight back here (a Skilled Worker's Publish), and only paths inside
/// the app are accepted.
/// </summary>
public class AuthRoutesTests
{
    [Fact]
    public void AddPhone_WithBack_CarriesBothPaths()
    {
        AuthRoutes.AddPhone("/professional/publish", "/start/pro/setup/1")
            .Should().Be("/verify/phone?then=%2Fprofessional%2Fpublish&back=%2Fstart%2Fpro%2Fsetup%2F1");
    }

    [Fact]
    public void AddPhone_WithLater_LetsAPageThatNeedsTheNumberBeLeftForNow()
    {
        AuthRoutes.AddPhone("/professional/publish", "/start/pro/setup/1", later: "/professional")
            .Should().Be("/verify/phone?then=%2Fprofessional%2Fpublish&back=%2Fstart%2Fpro%2Fsetup%2F1&later=%2Fprofessional");
        AuthRoutes.AddPhone(null, later: "https://example.com").Should().Be("/verify/phone", "only a path inside the app");
    }

    [Fact]
    public void AddPhone_WithoutBack_IsUnchanged()
    {
        AuthRoutes.AddPhone("/search").Should().Be("/verify/phone?then=%2Fsearch");
        AuthRoutes.AddPhone("/start/pro/1", "/start/role", canSkip: true)
            .Should().Be("/verify/phone?then=%2Fstart%2Fpro%2F1&back=%2Fstart%2Frole&skip=true");
        AuthRoutes.AddPhone(null, canSkip: true).Should().Be("/verify/phone", "skipping needs somewhere to go");
    }

    [Fact]
    public void EditName_OpensTheNamePageInEditModeAndComesBack()
    {
        AuthRoutes.EditName("/professional/me").Should().Be("/verify/name?edit=true&then=%2Fprofessional%2Fme");
        AuthRoutes.EditName("https://example.com").Should().Be("/verify/name?edit=true", "only paths inside the app");
    }

    [Fact]
    public void AddPhone_BackOutsideTheApp_IsDropped()
    {
        AuthRoutes.AddPhone(null, "https://example.com").Should().Be("/verify/phone");
        AuthRoutes.AddPhone(null, "/start/pro/setup/1").Should().Be("/verify/phone?back=%2Fstart%2Fpro%2Fsetup%2F1");
    }
}
