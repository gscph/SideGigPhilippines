using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Pro home checklist until the listing is published: own photo, work photos, publish.
/// The finished interview counts as the first step, so the bar never starts empty.
/// </summary>
public partial class SetupCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public bool HasPhoto { get; set; }

    [Parameter] public bool HasWorkPhotos { get; set; }

    // Steps: interview (done), photo, work photos, publish.
    private double Percent => (1 + (HasPhoto ? 1 : 0) + (HasWorkPhotos ? 1 : 0)) * 100.0 / 4;
}
