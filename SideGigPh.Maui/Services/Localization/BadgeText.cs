using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>A badge's words in the user's language, shared by the cards, the public profile and the pro's own badges page.</summary>
public static class BadgeText
{
    /// <summary>The short label on a chip: "ID Verified", "NBI / Police Clearance".</summary>
    public static string Badge(this AppText text, Badge badge) =>
        text.Get(badge.Kind == BadgeKind.Clearance ? Copy.BadgeClearance : Copy.BadgeId);

    /// <summary>What Tarbaho checked for it, one plain sentence.</summary>
    public static string BadgeNote(this AppText text, Badge badge) =>
        text.Get(badge.Kind == BadgeKind.Clearance ? Copy.BadgeClearanceNote : Copy.BadgeIdNote);

    /// <summary>
    /// The badges to show as chips, most telling first (ID, then clearance), at most <paramref name="max"/>,
    /// and how many were left out.
    /// </summary>
    public static (IReadOnlyList<Badge> Shown, int More) BadgesToShow(this ProfessionalListing listing, int max)
    {
        var ordered = listing.Badges.OrderBy(b => b.Kind).ToList();
        return (ordered.Take(max).ToList(), Math.Max(0, ordered.Count - max));
    }
}
