namespace SideGigPh.Contracts;

// The operator site's Catalog page (ops/catalog…, only the uids in AdminUids): the trades (Categories) and their jobs
// (Services) the app offers, renamed, added and retired by an operator. Ids are made by the server and never change; a
// trade or job is never deleted, only retired (IsActive = false) and restored. Posts and chats keep the names they
// were made with (CatalogNameDto), so a rename here changes what people see from now on, never what they posted.

/// <summary><c>GET ops/catalog</c>: every trade, retired ones too, in the app's order, each with its jobs.</summary>
public sealed record AdminCatalogDto(List<AdminTradeDto> Trades);

/// <summary>
/// One trade as the operator sees it.
/// </summary>
/// <param name="Workers">Skilled workers whose listing is of this trade (live or not).</param>
/// <param name="OpenPosts">Households' posts of this trade still open.</param>
/// <param name="EditedAt">When an operator last changed it here; null = only <c>004-config.sql</c> ever set it.</param>
public sealed record AdminTradeDto(
    string Id,
    string English,
    string Taglish,
    string? HintEnglish,
    string? HintTaglish,
    int SortOrder,
    bool IsActive,
    int Workers,
    int OpenPosts,
    DateTimeOffset? EditedAt,
    List<AdminServiceDto> Services);

/// <summary>
/// One job within a trade.
/// </summary>
/// <param name="IsOther">The trade's "Others" job: always last, can be renamed but never retired.</param>
/// <param name="Price">The price guide shown to workers and households (only a guide), or null.</param>
/// <param name="Workers">Skilled workers who offer this job.</param>
/// <param name="OpenPosts">Households' posts for this job still open.</param>
public sealed record AdminServiceDto(
    string Id,
    string CategoryId,
    string English,
    string Taglish,
    int SortOrder,
    bool IsActive,
    bool IsOther,
    PriceGuideDto? Price,
    int Workers,
    int OpenPosts,
    DateTimeOffset? EditedAt);

/// <summary>
/// <c>POST ops/catalog/trades</c> (a new trade, which gets its "Others" job too) and <c>PUT ops/catalog/trades/{id}</c>
/// (rename). Names are required (<see cref="CatalogRules.IsTradeName"/>); hints are optional.
/// </summary>
public sealed record SaveTradeRequest(string English, string Taglish, string? HintEnglish = null, string? HintTaglish = null);

/// <summary>
/// <c>POST ops/catalog/services</c> (a new job in <paramref name="CategoryId"/>, placed just before its "Others") and
/// <c>PUT ops/catalog/services/{id}</c> (rename; <paramref name="CategoryId"/> is ignored: a job never moves trade).
/// <paramref name="Price"/> is optional (<see cref="CatalogRules.IsPrice"/>).
/// </summary>
public sealed record SaveServiceRequest(string CategoryId, string English, string Taglish, PriceGuideDto? Price = null);

/// <summary>The limits the operator site and the API both check on a trade or job.</summary>
public static class CatalogRules
{
    /// <summary>Most characters of a trade's name (the <c>Categories</c> column).</summary>
    public const int MaxTradeName = 60;

    /// <summary>Most characters of a job's name (the <c>Services</c> column).</summary>
    public const int MaxServiceName = 100;

    /// <summary>Most characters of a trade's hint.</summary>
    public const int MaxHint = 120;

    /// <summary>The most a price guide may say, in pesos.</summary>
    public const int MaxPrice = 1_000_000;

    public static bool IsTradeName(string? name) => IsName(name, MaxTradeName);

    public static bool IsServiceName(string? name) => IsName(name, MaxServiceName);

    public static bool IsHint(string? hint) => hint is null || hint.Trim().Length <= MaxHint;

    /// <summary>No guide, or low ≤ average ≤ high, all above 0 and at most <see cref="MaxPrice"/>.</summary>
    public static bool IsPrice(PriceGuideDto? price) =>
        price is null || (price.Min > 0 && price.Min <= price.Average && price.Average <= price.High && price.High <= MaxPrice);

    private static bool IsName(string? name, int max) => name?.Trim() is { Length: > 0 } trimmed && trimmed.Length <= max;
}
