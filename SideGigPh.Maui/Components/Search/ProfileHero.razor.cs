using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The top of a worker's public profile, centered like Airbnb's detail page: the photo, the name (the page's h1), one
/// line saying what and where ("Aircon Technician in Angono", the Seeker's own town first) and one muted line with the
/// other towns.
/// </summary>
public partial class ProfileHero : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>The Seeker's town, named first when the worker goes there.</summary>
    [Parameter] public string? SeekerTownId { get; set; }

    private IReadOnlyList<string> Towns => ProfileFacts.Towns(Listing, SeekerTownId);

    private string TradeIn => T.TradeIn(Listing, Towns);

    private string? AlsoGoesTo => T.AlsoGoesTo(Towns);
}
