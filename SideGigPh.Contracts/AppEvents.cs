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

    /// <summary>A sign-up question or page shown (<see cref="AppEventSteps"/>): where people stop before they finish.</summary>
    public const string OnboardingStep = "onboarding_step";

    /// <summary>A household opened the form to post what they need (the post itself is <see cref="RequestCreated"/>).</summary>
    public const string PostStarted = "post_started";

    /// <summary>The only events <c>POST events</c> accepts; everything else is the API's own.</summary>
    public static readonly IReadOnlySet<string> FromApp =
        new HashSet<string>(StringComparer.Ordinal) { Search, SearchEmpty, OnboardingStep, PostStarted };

    /// <summary>
    /// Who an app event is about when the phone didn't say (an older app): searches and posts are a household's,
    /// a step before the side is picked is nobody's yet.
    /// </summary>
    public static string? DefaultUserType(string name) =>
        name is Search or SearchEmpty or PostStarted ? AppEventUserTypes.Customer : null;
}

/// <summary>
/// The sign-up steps an <see cref="AppEventNames.OnboardingStep"/> names (<c>app_events.step</c>), in the order people
/// meet them: the side fork, then that side's questions. Names are stable: the funnel reports are built on them, and
/// the app's step enums (<c>ProStep</c>, <c>PublishStep</c>, <c>SeekerStep</c>) map onto them by name.
/// </summary>
public static class AppEventSteps
{
    public const string Role = "role";

    /// <summary>A Skilled Worker's interview (<c>ProStep</c>), then going live (<c>PublishStep</c>).</summary>
    public static readonly IReadOnlyList<string> Worker =
    [
        Role, "pro_skills", "pro_services", "pro_rate", "pro_towns", "pro_experience", "pro_photo", "pro_number",
        "pro_preview", "publish_checklist", "publish_ready",
    ];

    /// <summary>A household's interview (<c>SeekerStep</c>).</summary>
    public static readonly IReadOnlyList<string> Household =
    [
        Role, "seeker_category", "seeker_service", "seeker_city", "seeker_when", "seeker_budget", "seeker_findby", "seeker_finding",
    ];

    /// <summary>The step name for an app step enum value: <c>pro_</c>, <c>publish_</c> or <c>seeker_</c> and its name in lower case.</summary>
    public static string Of(string prefix, string stepName) => $"{prefix}_{stepName.ToLowerInvariant()}";

    /// <summary>True for a step this list knows; anything else from a phone is dropped.</summary>
    public static bool IsKnown(string? step) => step is not null && (Worker.Contains(step) || Household.Contains(step));
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
/// One event the app queued on the phone (<c>POST events</c>, a JSON array of at most <see cref="AppEventRules.MaxBatch"/>):
/// a search with its trade and job, a sign-up step, a post form opened. Never a name, number or place finer than a
/// town: who sent it comes from the sign-in token, never from here; <see cref="UserType"/> is only which side.
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
    string? AppVersion,
    string? ServiceId = null,
    string? Step = null,
    string? UserType = null);

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
    public const int MaxServiceId = 60;
    public const int MaxStep = 40;

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
