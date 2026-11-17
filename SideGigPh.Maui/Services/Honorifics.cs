namespace SideGigPh.Maui.Services;

/// <summary>
/// Tarbaho's brand voice: workers are called by the honorifics Filipinos actually use (Manong, Kuya, Ate, Manang),
/// never "laborers". The brand name never changes; only the honorific rotates: a random one on the splash and the
/// search box, and the one that fits the trade once a trade is picked (Manong for construction and maestro work, Kuya
/// for everyday repairs, Ate for errands). Ate and Manang are always in the rotation so women workers see the app is
/// for them too. The copy takes the word as <c>{0}</c>; some English lines want the plural ("Find Kuyas nearby")
/// where Taglish keeps the word ("May Kuya para diyan"), so those pass <c>englishPlural</c>.
/// </summary>
public static class Honorifics
{
    public const string Manong = "Manong";
    public const string Kuya = "Kuya";
    public const string Ate = "Ate";
    public const string Manang = "Manang";

    public static IReadOnlyList<string> All { get; } = [Manong, Kuya, Ate, Manang];

    /// <summary>Construction and maestro trades (category ids are stable).</summary>
    private static readonly HashSet<string> ManongTrades = ["construction", "roofing", "carpentry", "masonry_tiling"];

    /// <summary>Errands, and the cleaning, laundry and sewing trades if they come back.</summary>
    private static readonly HashSet<string> AteTrades = ["errand_runner"];

    /// <summary>A different honorific each time (splash, search box).</summary>
    public static string Random() => All[System.Random.Shared.Next(All.Count)];

    /// <summary>The honorific that fits a trade; everyday repairs (and anything new) get Kuya.</summary>
    public static string For(string? categoryId) =>
        categoryId is null ? Kuya
        : ManongTrades.Contains(categoryId) ? Manong
        : AteTrades.Contains(categoryId) ? Ate
        : Kuya;

    public static string Plural(string honorific) => honorific + "s";

    /// <summary>Fills a copy line's <c>{0}</c> with the honorific (its plural in English when <paramref name="englishPlural"/>).</summary>
    public static string FormatHonorific(this AppText text, LocalizedText copy, string honorific, bool englishPlural = false) =>
        text.Format(copy, englishPlural && text.Language == AppLanguage.English ? Plural(honorific) : honorific);
}
