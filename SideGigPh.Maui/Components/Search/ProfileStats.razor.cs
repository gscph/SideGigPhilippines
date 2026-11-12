using System.Globalization;
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The profile's three numbers side by side, so a Seeker gets them at a glance: stars (gold), experience
/// (the brand color with an award icon at 10+ years) and price per job (dollar green).
/// </summary>
public partial class ProfileStats : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>Stars from the reviews just read, or null with none yet ("New").</summary>
    [Parameter] public ReviewSummary? Reviews { get; set; }

    private static string Average(ReviewSummary reviews) => reviews.Average.ToString("0.0", CultureInfo.InvariantCulture);
}
