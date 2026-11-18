using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.PhoneNumbers;

/// <summary>
/// Phone numbers are how people sign in, how customers call workers and the anti-fraud key (1 phone = 1 listing).
/// The app must accept every way Filipinos type a mobile number, always store it the same way (+639…), and that
/// stored form must be one the database accepts.
/// </summary>
public class PhilippineMobileTests
{
    [Theory]
    [InlineData("09171234567")]
    [InlineData("0917 123 4567")]
    [InlineData("0917-123-4567")]
    [InlineData("9171234567")]
    [InlineData("639171234567")]
    [InlineData("+63 917 123 4567")]
    [InlineData("+639171234567")]
    public void TryNormalize_EveryUsualWayOfTyping_GivesTheSameE164(string typed)
    {
        PhilippineMobile.TryNormalize(typed, out var e164).Should().BeTrue();
        e164.Should().Be("+639171234567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0917123456")]    // one digit short
    [InlineData("091712345678")]  // one digit too many
    [InlineData("0281234567")]    // Manila landline
    [InlineData("+15551234567")]  // not Philippine
    [InlineData("hello")]
    public void TryNormalize_NotAPhilippineMobile_IsRefused(string? typed) =>
        PhilippineMobile.TryNormalize(typed, out _).Should().BeFalse();

    [Fact]
    public void TryNormalize_Result_IsAcceptedByTheDatabase()
    {
        PhilippineMobile.TryNormalize("0918 765 4321", out var e164);

        // The schema's mobile number checks (001-schema.sql: LIKE '+639' and 9 digits).
        e164.Should().MatchRegex(@"^\+639[0-9]{9}$", "the app must never store a number the database refuses");
    }

    [Fact]
    public void ToLocal_ShowsTheNumberTheWayPeopleWriteIt() =>
        PhilippineMobile.ToLocal("+639171234567").Should().Be("0917 123 4567");

    [Fact]
    public void ToLocal_SomethingElse_IsLeftAsIs() =>
        PhilippineMobile.ToLocal("+15551234567").Should().Be("+15551234567");

    [Fact]
    public void SignInScreenFormat_UsesTheSameRules()
    {
        var format = new PhilippinePhoneNumberFormat();

        format.TryNormalize("0917 123 4567", out var e164).Should().BeTrue();
        e164.Should().Be("+639171234567");
        format.Region.Should().Contain("+63");
        PhilippineMobile.TryNormalize(format.Placeholder, out _).Should().BeTrue("the example number in the box is itself valid");
    }
}
