using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Api;

/// <summary>One Early Access sign-up from the landing page.</summary>
public sealed record EarlyAccessSignup(string Email, string Phone, string? Role, string? Language, DateTimeOffset At);

/// <summary>Tells the team about a sign-up (no list is kept on our side: the inbox is the list).</summary>
public interface IEarlyAccessMailer
{
    /// <summary>True when the email was handed to the mail service; false when it couldn't be (the page says "didn't send").</summary>
    Task<bool> SendAsync(EarlyAccessSignup signup, CancellationToken cancellationToken);
}

/// <summary>
/// Sends each sign-up by Azure Communication Services Email from the tarbaho.com domain (setup: azure/README.md →
/// Early Access email). Settings: <see cref="ConnectionSetting"/>, <see cref="FromSetting"/>, <see cref="ToSetting"/>.
/// The recipient lives only in the Function App setting, never in the code (the repository is public).
/// </summary>
public sealed class AcsEarlyAccessMailer(EmailClient client, string from, string to, ILogger<AcsEarlyAccessMailer> logger) : IEarlyAccessMailer
{
    public const string ConnectionSetting = "EmailConnection";
    public const string FromSetting = "EarlyAccessFrom";
    public const string ToSetting = "EarlyAccessTo";

    public async Task<bool> SendAsync(EarlyAccessSignup signup, CancellationToken cancellationToken)
    {
        var message = new EmailMessage(from, to, new EmailContent(EarlyAccessEmail.Subject(signup)) { PlainText = EarlyAccessEmail.Body(signup) });
        message.ReplyTo.Add(new EmailAddress(signup.Email));
        try
        {
            // Started, not awaited to the end: the service queues it, and the visitor shouldn't wait for delivery.
            await client.SendAsync(WaitUntil.Started, message, cancellationToken);
            return true;
        }
        catch (RequestFailedException e)
        {
            // Never log the address: not even the exception, whose message can repeat the sender, recipient or reply-to.
            logger.LogError("Early access email not sent ({Status} {Code})", e.Status, e.ErrorCode);
            return false;
        }
    }
}

/// <summary>Without the email settings (a PC): nothing is sent, and the sign-up says "didn't send" so nobody is lost silently.</summary>
public sealed class NoEarlyAccessMailer(ILogger<NoEarlyAccessMailer> logger) : IEarlyAccessMailer
{
    public Task<bool> SendAsync(EarlyAccessSignup signup, CancellationToken cancellationToken)
    {
        logger.LogWarning("Early access sign-up dropped: the email settings ({Setting}) are not set", AcsEarlyAccessMailer.ConnectionSetting);
        return Task.FromResult(false);
    }
}

/// <summary>What the team's email says: everything needed to send the Play testing or TestFlight invite, one line each.</summary>
public static class EarlyAccessEmail
{
    public static string Subject(EarlyAccessSignup s) => $"Early access: {s.Email} ({PhoneName(s.Phone)}{(s.Role is null ? "" : ", " + s.Role)})";

    public static string Body(EarlyAccessSignup s) =>
        $"""
        New early access sign-up from the Tarbaho landing page.

        Email:    {s.Email}
        Phone:    {PhoneName(s.Phone)} ({(s.Phone == Contracts.EarlyAccessRules.IPhone ? "the page gave them the public TestFlight link" : "Play Store Gmail: add it to the Play testing list")})
        Side:     {s.Role ?? "not picked"}
        Language: {(s.Language == "tl" ? "Taglish" : "English")}
        When:     {s.At.ToOffset(TimeSpan.FromHours(8)):yyyy-MM-dd HH:mm} (Philippine time)

        Reply to this email to write to them directly. Delete it when they ask (privacy policy).
        """;

    private static string PhoneName(string phone) => phone == Contracts.EarlyAccessRules.IPhone ? "iPhone" : "Android";
}
