using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>A badge's words in the user's language, shared by the cards, the public profile and the pro's own badges page.</summary>
public static class BadgeText
{
    /// <summary>The short label on a chip: "ID Verified", "TESDA Certified", "Ayala Land project".</summary>
    public static string Badge(this AppText text, Badge badge) => badge.Kind switch
    {
        BadgeKind.Id => text.Get(Copy.BadgeId),
        BadgeKind.Tesda => text.Get(Copy.BadgeTesda),
        _ => BadgeCatalog.CompanyOf(badge) is { } company ? text.Format(Copy.BadgeProject, company) : text.Get(Copy.BadgeProjectTitle),
    };

    /// <summary>What Tarbaho checked for it, one plain sentence (with the building for a project, when the worker named it).</summary>
    public static string BadgeNote(this AppText text, Badge badge) => badge.Kind switch
    {
        BadgeKind.Id => text.Get(Copy.BadgeIdNote),
        BadgeKind.Tesda => text.Get(Copy.BadgeTesdaNote),
        _ => string.IsNullOrWhiteSpace(badge.ProjectName) ? text.Get(Copy.BadgeProjectNote) : text.Format(Copy.BadgeProjectAt, badge.ProjectName.Trim()),
    };

    /// <summary>
    /// The badges to show as chips, most telling first (ID, TESDA, then projects), at most <paramref name="max"/>,
    /// and how many were left out.
    /// </summary>
    public static (IReadOnlyList<Badge> Shown, int More) BadgesToShow(this ProfessionalListing listing, int max)
    {
        var ordered = listing.Badges.OrderBy(b => b.Kind).ToList();
        return (ordered.Take(max).ToList(), Math.Max(0, ordered.Count - max));
    }
}
