using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// Error reports must never carry who the person is: the scrubber removes sign-in tokens, numbers, emails, uids (also
/// inside chat ids), photo hashes and search words, but keeps what we need to fix the bug (types, methods, lines).
/// </summary>
public class ErrorReportRulesTests
{
    [Theory]
    [InlineData("Bearer eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJ4In0.c2lnbmF0dXJl failed", "Bearer [token] failed")]
    [InlineData("token eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJ4In0.c2lnbmF0dXJl", "token [token]")]
    [InlineData("Number +639171234567 is taken", "Number [phone] is taken")]
    [InlineData("call 0917 123 4567 now", "call [phone] now")]
    [InlineData("call 0917-123-4567", "call [phone]")]
    [InlineData("from +14155550123", "from [phone]")]
    [InlineData("mail juan.dela+cruz@gmail.com today", "mail [email] today")]
    [InlineData("Saving the profile of Xy7kP2mQ9rT4vW1zA3bC5dE8fG6h failed.", "Saving the profile of [id] failed.")]
    [InlineData("chats/Xy7kP2mQ9rT4vW1zA3bC5dE8fG6h_Ab3dE5gH7jK9mN1pQ3sT5vW7yZ0b/messages", "chats/[id]_[id]/messages")]
    [InlineData("photo 3f8a2c9d1e4b7a6f5c0d2e9b8a7f6c5d4e3b2a1f0e9d8c7b6a5f4e3d2c1b0a9f.jpg", "photo [hash].jpg")]
    [InlineData("read search/professionals?categoryId=aircon&q=Juan%20Cruz failed", "read search/professionals?[query] failed")]
    public void Scrub_RemovesWhatPointsAtAPerson(string text, string expected) =>
        ErrorReportRules.Scrub(text).Should().Be(expected);

    [Fact]
    public void Scrub_KeepsTheStackTrace()
    {
        const string stack = """
            System.Net.Http.HttpRequestException: Response status code does not indicate success: 500 (Internal Server Error).
               at SideGigPh.Maui.Services.Api.ApiClient.ThrowIfFailedAsync(HttpResponseMessage response, String what) in ApiClient.cs:line 124
               at SideGigPh.Maui.Components.Onboarding.ProfessionalInterview.FinishAsync()
            """;

        ErrorReportRules.Scrub(stack).Should().Be(stack);
    }

    [Fact]
    public void Clean_ScrubsTrimsAndCuts()
    {
        ErrorReportRules.Clean("  +639171234567  ", 100).Should().Be("[phone]");
        ErrorReportRules.Clean(new string('a', 50), 10).Should().HaveLength(10);
        ErrorReportRules.Clean("   ", 10).Should().BeNull();
        ErrorReportRules.Clean(null, 10).Should().BeNull();
    }
}
