using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>The jobs a pro takes, as small chips (pro card and public profile).</summary>
public partial class ServiceChips : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    private IReadOnlyList<string> Labels => T.JobLabels(Listing);
}
