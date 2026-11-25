using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// "How the photo should look", shown before the camera opens for an ID or a clearance: a picture of a whole card in
/// a frame and three short tips. Most unreadable proofs are a cut-off card, a flash or a shaken hand; saying so first
/// is cheaper than any check afterwards.
/// </summary>
public partial class ProofPhotoGuide : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    private static readonly (string Icon, LocalizedText Tip)[] Tips =
    [
        (Icons.Material.Rounded.CropFree, Copy.BadgeGuideWhole),
        (Icons.Material.Rounded.WbSunny, Copy.BadgeGuideLight),
        (Icons.Material.Rounded.PanTool, Copy.BadgeGuideSteady),
    ];
}
