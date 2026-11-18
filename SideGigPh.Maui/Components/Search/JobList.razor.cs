using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// "What Juan does" on the public profile: a plain list like Airbnb's "What this place offers", each service with its
/// own small picture and its label in full-size text.
/// </summary>
public partial class JobList : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    private IReadOnlyList<(string Id, string Label)> Jobs => T.Jobs(Listing);
}
