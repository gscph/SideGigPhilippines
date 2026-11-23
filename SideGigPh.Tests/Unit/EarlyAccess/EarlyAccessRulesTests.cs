using SideGigPh.Api;
using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.EarlyAccess;

/// <summary>The landing page's sign-up rules (shared by the page and the API) and the email the team gets.</summary>
public class EarlyAccessRulesTests
{
    [Theory]
    [InlineData("juan@gmail.com")]
    [InlineData("  Juan.Dela-Cruz+tarbaho@Gmail.COM ")]
    [InlineData("ate@school.edu.ph")]
    public void PlainAddresses_AreValid(string email) => EarlyAccessRules.IsValidEmail(email).Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("juan")]
    [InlineData("juan@gmail")]
    [InlineData("juan @gmail.com")]
    [InlineData("juan@@gmail.com")]
    [InlineData("juan@gmail..com")]
    public void BrokenAddresses_AreNot(string? email) => EarlyAccessRules.IsValidEmail(email).Should().BeFalse();

    [Fact]
    public void AnAddressOverTheMailLimit_IsNot() =>
        EarlyAccessRules.IsValidEmail(new string('a', 250) + "@x.com").Should().BeFalse();

    [Fact]
    public void Normalize_TrimsAndLowerCases() => EarlyAccessRules.Normalize("  Juan@Gmail.Com ").Should().Be("juan@gmail.com");

    [Theory]
    [InlineData("juan@gmial.com", "juan@gmail.com")]
    [InlineData("Juan@GMAIL.CON", "juan@gmail.com")]
    [InlineData("ate@yaho.com", "ate@yahoo.com")]
    public void CommonPhoneTypos_GetASuggestion(string typed, string meant) => EarlyAccessRules.SuggestFix(typed).Should().Be(meant);

    [Theory]
    [InlineData("juan@gmail.com")]
    [InlineData("juan")]
    [InlineData("@gmial.com")]
    public void AFineOrUnreadableAddress_GetsNone(string typed) => EarlyAccessRules.SuggestFix(typed).Should().BeNull();

    [Fact]
    public void OnlyKnownWords_AreKept()
    {
        EarlyAccessRules.CleanPhone("android").Should().Be("android");
        EarlyAccessRules.CleanPhone("iphone").Should().Be("iphone");
        EarlyAccessRules.CleanPhone("nokia").Should().BeNull();
        EarlyAccessRules.CleanRole("worker").Should().Be("worker");
        EarlyAccessRules.CleanRole("admin").Should().BeNull();
        EarlyAccessRules.CleanLanguage("tl").Should().Be("tl");
        EarlyAccessRules.CleanLanguage("fr").Should().BeNull();
    }

    [Fact]
    public void TheTeamsEmail_SaysWhichInviteToSend()
    {
        var at = new DateTimeOffset(2026, 11, 23, 0, 15, 0, TimeSpan.Zero);

        var android = new EarlyAccessSignup("juan@gmail.com", EarlyAccessRules.Android, EarlyAccessRules.Worker, "tl", at);
        var iphone = new EarlyAccessSignup("ana@icloud.com", EarlyAccessRules.IPhone, null, "en", at);

        EarlyAccessEmail.Subject(android).Should().Be("Early access: juan@gmail.com (Android, worker)");
        EarlyAccessEmail.Body(android).Should().Contain("Play testing list").And.Contain("Taglish").And.Contain("2026-11-23 08:15");
        EarlyAccessEmail.Subject(iphone).Should().Be("Early access: ana@icloud.com (iPhone)");
        EarlyAccessEmail.Body(iphone).Should().Contain("public TestFlight link").And.Contain("not picked");
    }
}
