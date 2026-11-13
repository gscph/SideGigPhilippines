namespace SideGigPh.Maui.Models;

/// <summary>
/// The answer lists that may be a few hours stale, from the server's <c>GET config</c>: the trades
/// (<see cref="Categories"/>) and each trade's jobs (most-requested first, "Others" last), the towns we serve, the
/// price scale (cheapest first), experience, urgency and monthly income goals. Loaded once when the app opens and
/// kept on the phone (<c>RemoteConfig</c>); the app ships a copy for its first launch offline (<c>ReferenceData.BuiltInConfig</c>).
/// </summary>
public sealed record AppConfig(
    IReadOnlyList<CatalogOption> Categories,
    IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> ServicesByCategory,
    IReadOnlyList<CatalogOption> Cities,
    IReadOnlyList<CatalogOption> PriceTiers,
    IReadOnlyList<CatalogOption> Experience,
    IReadOnlyList<CatalogOption> Urgencies,
    IReadOnlyList<CatalogOption> IncomeGoals);

/// <summary>
/// What someone typed after picking "Others" in a trade. Kept on the server for analytics only: it never becomes a
/// job and is never shown to anyone.
/// </summary>
public sealed record OtherJobNote(string CategoryId, UserRole Side, string Text);
