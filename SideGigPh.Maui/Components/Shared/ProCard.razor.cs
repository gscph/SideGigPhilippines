using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// A Professional's listing card. The same card is used in Seeker results and in the
/// Professional's own preview, so what they build is exactly what customers will see.
/// </summary>
public partial class ProCard : ComponentBase
{
    /// <summary>How many work photos fit across the card, newest first.</summary>
    private const int MaxThumbnails = 3;

    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>The pro's stars, shown when they have reviews.</summary>
    [Parameter] public ReviewSummary? Reviews { get; set; }

    /// <summary>Optional green badge, e.g. "Fits your budget".</summary>
    [Parameter] public string? Highlight { get; set; }

    /// <summary>Extra CSS classes on the card, e.g. to outline the pro's own listing.</summary>
    [Parameter] public string? Class { get; set; }
}
