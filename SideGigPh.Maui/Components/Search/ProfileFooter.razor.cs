using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The profile's pinned bar, like Airbnb's: the worker's average price on the left (money green, "avg per service ·
/// only a guide") and one filled Contact button on the right.
/// </summary>
public partial class ProfileFooter : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>The chat Contact opens.</summary>
    [Parameter, EditorRequired] public string ContactHref { get; set; } = string.Empty;

    private string? Price => Listing.RateLabel();
}
