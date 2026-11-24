namespace SideGigPh.Contracts;

/// <summary>
/// The usage events Tarbaho counts (table <c>app_events</c>), and only these. Names are stable: the weekly report
/// (<c>analytics/weekly.sql</c>) is built on them. The API writes the business events itself; the app sends only
/// <see cref="FromApp"/>.
/// </summary>
public static class AppEventNames
{
    public const string SignUp = "sign_up";
    public const string Search = "search";
    public const string SearchEmpty = "search_empty";
    public const string RequestCreated = "request_created";
    public const string RequestMatched = "request_matched";
    public const string JobCompleted = "job_completed";
    public const string Rehire = "rehire";
    public const string WorkerProfileComplete = "worker_profile_complete";
    public const string WorkerFirstJob = "worker_first_job";
    public const string ReportUser = "report_user";
    public const string BlockUser = "block_user";

    /// <summary>The only events <c>POST events</c> accepts; everything else is the API's own.</summary>
    public static readonly IReadOnlySet<string> FromApp = new HashSet<string>(StringComparer.Ordinal) { Search, SearchEmpty };
}

/// <summary>Who an event is about (<c>app_events.user_type</c>).</summary>
public static class AppEventUserTypes
{
    public const string Customer = "customer";
    public const string Worker = "worker";

    /// <summary>The side as the API stores it (<see cref="Wire.Seeker"/> / <see cref="Wire.Professional"/>), or null.</summary>
    public static string? Of(string? role) => role switch
    {
        Wire.Seeker => Customer,
        Wire.Professional => Worker,
        _ => null,
    };
}

/// <summary>
/// One event the app queued on the phone (<c>POST events</c>, a JSON array of at most <see cref="AppEventRules.MaxBatch"/>).
/// Never a name, number or place finer than a town: who sent it comes from the sign-in token, never from here.
/// </summary>
public sealed record AppEventDto(
    Guid EventId,
    string Name,
    string? SessionId,
    string? ServiceCategory,
    string? Area,
    string? SearchTerm,
    int? ResultCount,
    DateTimeOffset? ClientTs,
    string? AppVersion);

/// <summary>The limits both sides keep for <see cref="AppEventDto"/>.</summary>
public static class AppEventRules
{
    /// <summary>The most events in one upload.</summary>
    public const int MaxBatch = 50;

    /// <summary>Column sizes in <c>app_events</c>; longer text is cut.</summary>
    public const int MaxName = 40;
    public const int MaxSession = 64;
    public const int MaxCategory = 40;
    public const int MaxArea = 60;
    public const int MaxSearchTerm = 100;
    public const int MaxVersion = 20;

    /// <summary>A phone clock this far ahead is wrong: the time it says is dropped (the arrival time is used).</summary>
    public static readonly TimeSpan MaxAhead = TimeSpan.FromMinutes(5);

    /// <summary>An event older than this is not counted by its own time (the phone keeps nothing older either).</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    /// <summary>The time the phone says, when it can be believed; else null.</summary>
    public static DateTimeOffset? Believable(DateTimeOffset? clientTs, DateTimeOffset now) =>
        clientTs is { } at && at <= now + MaxAhead && at >= now - MaxAge ? at : null;

    /// <summary>Typed search words with anything personal taken out (numbers, emails, links), trimmed and cut to the column.</summary>
    public static string? SearchTerm(string? words) => ErrorReportRules.Clean(words, MaxSearchTerm);

    /// <summary><paramref name="text"/> trimmed and cut to <paramref name="max"/>; null when empty.</summary>
    public static string? Cut(string? text, int max) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim() is var trimmed && trimmed.Length > max ? trimmed[..max] : text.Trim();
}
