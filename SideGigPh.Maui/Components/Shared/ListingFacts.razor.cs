using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// A pro's key facts, one icon line each: stars (when reviewed), price, experience, towns. <see cref="Dense"/> puts
/// them side by side and leaves out the towns (search results already group workers by town).
/// </summary>
public partial class ListingFacts : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    [Parameter] public ReviewSummary? Reviews { get; set; }

    /// <summary>The job the Seeker searched for: the price shown is the worker's price for it (else over all their jobs).</summary>
    [Parameter] public string? ServiceId { get; set; }

    /// <summary>Side by side, without towns: the compact search-result card.</summary>
    [Parameter] public bool Dense { get; set; }
}
