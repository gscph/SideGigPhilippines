using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Pro home checklist: own photo, a reference and a number confirmed with an SMS code. Once all three are ticked a
/// Publish button shows under them. Work photos sit last as an optional line ("be seen more"): they never count
/// toward the bar or Publish (the layout also opens Publish once by itself).
/// The finished interview counts as the first step, so the bar never starts empty. A listing published before
/// these were required sees it too (without the button) until they're done.
/// </summary>
public partial class SetupCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public bool HasPhoto { get; set; }

    [Parameter] public int WorkPhotoCount { get; set; }

    [Parameter] public bool HasReference { get; set; }

    [Parameter] public bool HasVerifiedPhone { get; set; }

    /// <summary>Where the number step leads: the add-phone page (sends the SMS code).</summary>
    [Parameter, EditorRequired] public string AddPhoneHref { get; set; } = string.Empty;

    [Parameter] public bool IsPublished { get; set; }

    private bool HasWorkPhotos => WorkPhotoCount >= ProfessionalDraft.GoodWorkPhotos;

    private bool AllDone => HasPhoto && HasReference && HasVerifiedPhone;

    // Steps: interview (done), photo, reference, number, publish. Work photos are optional and not counted.
    private double Percent =>
        (1 + (HasPhoto ? 1 : 0) + (HasReference ? 1 : 0) + (HasVerifiedPhone ? 1 : 0) + (IsPublished ? 1 : 0))
        * 100.0 / 5;
}
