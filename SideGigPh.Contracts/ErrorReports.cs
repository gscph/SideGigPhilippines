using System.Text.RegularExpressions;

namespace SideGigPh.Contracts;

/// <summary>
/// One problem the app had on a phone (<c>POST error-reports</c>): an error it logged, a crash, or a screen stuck
/// loading. The same problem repeated is sent once with its <paramref name="Count"/>. <paramref name="Kind"/> is one of
/// <see cref="ErrorKinds"/>. Every text is scrubbed (<see cref="ErrorReportRules.Scrub"/>) before it leaves the phone.
/// </summary>
/// <param name="Source">Where in the app: the logger's category, e.g. <c>SideGigPh.Maui.Services.Api.ApiClient</c>.</param>
/// <param name="Detail">The exception and its stack trace.</param>
/// <param name="Route">The screen it happened on, e.g. <c>/start/pro/done</c>.</param>
/// <param name="Trail">The screens and steps just before it, oldest first, one per line.</param>
public sealed record ErrorReportDto(
    string Kind,
    string Source,
    string Message,
    string? Detail,
    string? Route,
    string? Trail,
    int Count,
    DateTimeOffset FirstAt,
    DateTimeOffset LastAt);

/// <summary>
/// The reports one phone sends at once, with what it runs. <paramref name="InstallId"/> is random per app install,
/// never the account, so a report can't be tied to a person.
/// </summary>
/// <param name="AppVersion">The store version and build, e.g. <c>1.0 (12)</c>.</param>
/// <param name="CodeVersion">The git commit the app was built from, e.g. <c>48466c9</c>.</param>
/// <param name="Channel">One of <see cref="AppChannels"/>.</param>
/// <param name="Platform">The OS and its version, e.g. <c>android 14</c>.</param>
/// <param name="Device">The maker and model, e.g. <c>samsung SM-A155F</c>.</param>
public sealed record ErrorReportBatch(
    string InstallId,
    string AppVersion,
    string CodeVersion,
    string Channel,
    string Platform,
    string? Device,
    List<ErrorReportDto> Reports);

/// <summary>Which build a phone runs: a developer's (<c>dev</c>), a tester's (<c>beta</c>) or the store's (<c>prod</c>).</summary>
public static class AppChannels
{
    public const string Dev = "dev";
    public const string Beta = "beta";
    public const string Prod = "prod";

    public static bool IsKnown(string? channel) => channel is Dev or Beta or Prod;
}

/// <summary>The kinds of problem an <see cref="ErrorReportDto"/> describes.</summary>
public static class ErrorKinds
{
    /// <summary>The app logged an error and carried on (an API call failed, a save threw).</summary>
    public const string Error = "error";

    /// <summary>An exception nothing caught: the app closed.</summary>
    public const string Crash = "crash";

    /// <summary>The loading screen stayed up too long (looks frozen to the person).</summary>
    public const string Slow = "slow";

    public static bool IsKnown(string? kind) => kind is Error or Crash or Slow;
}

/// <summary>
/// The limits both sides keep (the columns of <c>ErrorReports</c>), and the scrubber that keeps personal data out of a
/// report. The app scrubs before sending and the server scrubs again, so an older app's slip is still caught.
/// </summary>
public static partial class ErrorReportRules
{
    /// <summary>Most reports in one batch; the server refuses more.</summary>
    public const int MaxReports = 20;

    public const int MaxSource = 200;
    public const int MaxMessage = 1000;
    public const int MaxDetail = 4000;
    public const int MaxRoute = 200;
    public const int MaxTrail = 2000;
    public const int MaxDevice = 80;
    public const int MaxVersion = 30;
    public const int MaxCodeVersion = 40;

    /// <summary>
    /// The text with what could point at a person replaced: sign-in tokens, email addresses, phone numbers, Firebase
    /// uids (also inside chat ids), photo hashes and the query part of addresses (search words). Null stays null.
    /// </summary>
    public static string? Scrub(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        text = Jwt().Replace(text, "[token]");
        text = Bearer().Replace(text, "Bearer [token]");
        text = Email().Replace(text, "[email]");
        text = Query().Replace(text, "?[query]");
        text = Hash().Replace(text, "[hash]");
        text = Uid().Replace(text, "[id]");
        text = Phone().Replace(text, "[phone]");
        return text;
    }

    /// <summary>Scrubbed, trimmed and cut to <paramref name="max"/> characters; null when empty.</summary>
    public static string? Clean(string? text, int max)
    {
        var scrubbed = Scrub(text)?.Trim();
        return string.IsNullOrEmpty(scrubbed) ? null : scrubbed.Length > max ? scrubbed[..max] : scrubbed;
    }

    [GeneratedRegex(@"eyJ[\w-]+\.[\w-]+\.[\w-]+")]
    private static partial Regex Jwt();

    [GeneratedRegex(@"Bearer\s+\S+", RegexOptions.IgnoreCase)]
    private static partial Regex Bearer();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(\.[\w-]+)+")]
    private static partial Regex Email();

    // "search/professionals?categoryId=…&q=…": keep the path, drop what follows the "?".
    [GeneratedRegex(@"\?[^\s""'<>]+")]
    private static partial Regex Query();

    [GeneratedRegex(@"(?<![0-9a-fA-F])[0-9a-fA-F]{64}(?![0-9a-fA-F])")]
    private static partial Regex Hash();

    // A Firebase uid: 28 letters and digits with at least one digit, one capital and one small letter (code words
    // and type names don't look like that). Also inside "{workerUid}_{seekerUid}" chat ids.
    [GeneratedRegex(@"(?<![A-Za-z0-9])(?=[A-Za-z0-9]{28}(?![A-Za-z0-9]))(?=[A-Za-z0-9]*[0-9])(?=[A-Za-z0-9]*[A-Z])(?=[A-Za-z0-9]*[a-z])[A-Za-z0-9]{28}")]
    private static partial Regex Uid();

    // Philippine mobiles (+63 9xx…, 09xx…, with spaces or dashes) and any other international number.
    [GeneratedRegex(@"(\+?63|0)9\d{2}[\s-]?\d{3}[\s-]?\d{4}|\+\d{10,15}")]
    private static partial Regex Phone();
}
