using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// A pro's approved badges as small outlined chips in the brand color ("ID Verified", "NBI / Police Clearance"),
/// most telling first. Nothing is drawn for a pro with no badge.
/// </summary>
public partial class BadgeChips : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>The most chips drawn; the rest become "+2 more".</summary>
    [Parameter] public int Max { get; set; } = 3;

    [Parameter] public string? Class { get; set; }

    private IReadOnlyList<Badge> Shown => Listing.BadgesToShow(Max).Shown;

    private int More => Listing.BadgesToShow(Max).More;
}
