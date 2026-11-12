using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Pro home checklist: own photo, at least 2 work photos, a reference. Once all three are ticked a Publish button
/// shows under them. The finished interview counts as the first step, so the bar never starts empty. A listing
/// published before these were required sees it too (without the button) until they're done.
/// </summary>
public partial class SetupCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public bool HasPhoto { get; set; }

    [Parameter] public int WorkPhotoCount { get; set; }

    [Parameter] public bool HasReference { get; set; }

    [Parameter] public bool IsPublished { get; set; }

    private bool HasWorkPhotos => WorkPhotoCount >= ProfessionalDraft.MinWorkPhotos;

    private bool AllDone => HasPhoto && HasWorkPhotos && HasReference;

    // Steps: interview (done), photo, work photos, reference, publish.
    private double Percent =>
        (1 + (HasPhoto ? 1 : 0) + (HasWorkPhotos ? 1 : 0) + (HasReference ? 1 : 0) + (IsPublished ? 1 : 0)) * 100.0 / 5;
}
