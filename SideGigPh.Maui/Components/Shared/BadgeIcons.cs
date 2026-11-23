using MudBlazor;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>One icon per kind of badge, the same on the cards, the public profile and the pro's badges page.</summary>
public static class BadgeIcons
{
    public static string Of(BadgeKind kind) => kind switch
    {
        BadgeKind.Id => Icons.Material.Rounded.VerifiedUser,
        BadgeKind.Clearance => Icons.Material.Rounded.GppGood,
        BadgeKind.Tesda => Icons.Material.Rounded.School,
        _ => Icons.Material.Rounded.Apartment,
    };
}
