namespace SideGigPh.Maui.Models;

/// <summary>
/// The answer lists that may be a few hours stale, from the server's <c>GET config</c>: the trades
/// (<see cref="Categories"/>) and each trade's jobs (most-requested first, "Others" last), the towns we serve, the
/// old price scale (cheapest first, read only for listings and budgets from older app versions), experience, urgency,
/// monthly income goals, each job's price guide (<see cref="Prices"/>, by job id) and the minimum wage every job's lowest
/// price stays at or above, the launch's town (<see cref="DefaultTownId"/>, pre-selected wherever a town is picked) and
/// the barangays we serve in each town (<see cref="BarangaysByTown"/>, A–Z, always optional to pick). Loaded once when
/// the app opens and kept on the phone (<c>RemoteConfig</c>); the app ships a copy for its first launch offline
/// (<c>ReferenceData.BuiltInConfig</c>). The last two are null in a copy saved before them.
/// </summary>
public sealed record AppConfig(
    IReadOnlyList<CatalogOption> Categories,
    IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> ServicesByCategory,
    IReadOnlyList<CatalogOption> Cities,
    IReadOnlyList<CatalogOption> PriceTiers,
    IReadOnlyList<CatalogOption> Experience,
    IReadOnlyList<CatalogOption> Urgencies,
    IReadOnlyList<CatalogOption> IncomeGoals,
    IReadOnlyDictionary<string, PriceGuide> Prices,
    int MinimumWage,
    string? DefaultTownId = null,
    IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>? BarangaysByTown = null);

/// <summary>
/// What a job usually costs, in pesos (labor only): the lowest fair price (never under the minimum wage), the average,
/// and above average. A Skilled Worker charges one of the three; a Seeker's budget choices are built from them.
/// </summary>
public sealed record PriceGuide(int Min, int Average, int High);

/// <summary>
/// What someone typed after picking "Others" in a trade. Kept on the server for analytics only: it never becomes a
/// job and is never shown to anyone.
/// </summary>
public sealed record OtherJobNote(string CategoryId, UserRole Side, string Text);
