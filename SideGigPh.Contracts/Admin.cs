namespace SideGigPh.Contracts;

// The operator site's reads (SideGigPh.Admin → admin/…, only the uids in AdminUids). Lists come in pages with a total, so
// the site's grids page on the server. Phone numbers and emails come masked: the dashboard is for counting and
// spotting problems, not for contacting people.

/// <summary>The sort fields and filters every admin list understands (<c>?sort=…&amp;desc=true&amp;search=…&amp;skip=0&amp;take=25</c>).</summary>
public static class AdminLists
{
    /// <summary>The most rows one page may ask for.</summary>
    public const int MaxTake = 100;

    /// <summary>A page when the caller doesn't say.</summary>
    public const int DefaultTake = 25;

    public const string SortJoined = "joined";
    public const string SortActive = "active";
    public const string SortName = "name";
    public const string SortPosts = "posts";
    public const string SortRating = "rating";
    public const string SortCompleted = "completed";
    public const string SortRate = "rate";
    public const string SortLast = "last";
    public const string SortCount = "count";

    /// <summary><c>?role=none</c>: accounts that signed in but never picked a side.</summary>
    public const string NoRole = "none";
}

/// <summary><c>GET admin/stats</c>: the numbers on the dashboard. Days are Philippine days (UTC+8).</summary>
public sealed record AdminStatsDto(
    DateTimeOffset At,
    AdminUserStatsDto Users,
    AdminActivityStatsDto Activity,
    AdminTrustStatsDto Trust,
    List<AdminDayDto> SignUps,
    List<AdminCountDto> WorkersByTrade,
    List<AdminCountDto> WorkersByTown,
    List<AdminCountDto> PostsByTrade,
    List<AdminCountDto> SeekersByTown,
    List<AdminCountDto> Languages);

/// <summary>Who signed up and who came back.</summary>
public sealed record AdminUserStatsDto(
    int Total,
    int Seekers,
    int Workers,
    int NoRole,
    int LiveListings,
    int NewToday,
    int New7Days,
    int New30Days,
    int Active7Days,
    int Active30Days);

/// <summary>What people did: posts, offers, chats, reviews.</summary>
public sealed record AdminActivityStatsDto(
    int Posts,
    int OpenPosts,
    int Posts7Days,
    int Offers,
    int PickedOffers,
    int CompletedJobs,
    int Chats,
    int Chats7Days,
    int Messages7Days,
    int Reviews,
    int OtherJobNotes,
    int FeedbackPosts);

/// <summary>What needs the operator: badges waiting, reports open, the app's errors.</summary>
public sealed record AdminTrustStatsDto(
    int PendingBadges,
    int ApprovedBadges,
    int OpenReports,
    int Blocks,
    int ErrorReports24Hours,
    int ErrorReports7Days);

/// <summary>One Philippine day of sign-ups, by the side each account is on now.</summary>
public sealed record AdminDayDto(DateOnly Day, int Seekers, int Workers, int NoRole);

/// <summary>A count for one trade, town or language.</summary>
public sealed record AdminCountDto(string Id, string Label, int Count);

/// <summary><c>GET admin/users</c>: one account.</summary>
public sealed record AdminUserDto(
    string Uid,
    string? Name,
    string? Phone,
    string? Email,
    string? Role,
    string Language,
    bool HasPhoto,
    bool HasListing,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActiveAt);

/// <summary><c>GET admin/seekers</c>: one household and what it did.</summary>
public sealed record AdminSeekerDto(
    string Uid,
    string? Name,
    string? Phone,
    string Language,
    string? TownId,
    string? Town,
    int Posts,
    int OpenPosts,
    int Chats,
    int CompletedJobs,
    int ReviewsWritten,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActiveAt);

/// <summary><c>GET admin/workers</c>: one skilled worker, published or not, and how their listing does.</summary>
public sealed record AdminWorkerDto(
    string Uid,
    string? Name,
    string? Phone,
    string? TradeId,
    string? Trade,
    string? OtherWork,
    string? Towns,
    int RatePesos,
    string? ExperienceId,
    bool Live,
    bool HasPhoto,
    int WorkPhotos,
    int Badges,
    int PendingBadges,
    int Reviews,
    double? Rating,
    int Chats,
    int Offers,
    int CompletedJobs,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? LastActiveAt);

/// <summary><c>GET admin/error-reports</c>: one problem an app sent (scrubbed when recorded and again on the server).</summary>
public sealed record AdminErrorReportDto(
    long Id,
    string Kind,
    string Source,
    string Message,
    string? Detail,
    string? Route,
    string? Trail,
    int Count,
    string AppVersion,
    string CodeVersion,
    string Channel,
    string Platform,
    string? Device,
    string Install,
    DateTimeOffset FirstAt,
    DateTimeOffset LastAt,
    DateTimeOffset ReceivedAt);

public sealed record AdminUserPageDto(List<AdminUserDto> Items, int Total);

public sealed record AdminSeekerPageDto(List<AdminSeekerDto> Items, int Total);

public sealed record AdminWorkerPageDto(List<AdminWorkerDto> Items, int Total);

public sealed record AdminErrorReportPageDto(List<AdminErrorReportDto> Items, int Total);
