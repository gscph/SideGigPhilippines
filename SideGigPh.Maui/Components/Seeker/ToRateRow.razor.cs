using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>A pro the Seeker contacted but hasn't rated, with a Rate button.</summary>
public partial class ToRateRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Pro { get; set; } = default!;
}
