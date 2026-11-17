using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The worker's profile level under their name on the Me page: green "live", or the verified blue check of a fully
/// set-up profile (<see cref="ProfileProgress.Level"/>). Nothing before publishing: the red <c>NotLiveBanner</c> under
/// the top bar already says the profile isn't visible yet.
/// </summary>
public partial class ProfileLevelChip : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfileLevel Level { get; set; }

    private Color ChipColor => Level switch
    {
        ProfileLevel.Complete => Color.Tertiary,
        _ => Color.Success,
    };

    private string ChipIcon => Level switch
    {
        ProfileLevel.Complete => Icons.Material.Rounded.Verified,
        _ => Icons.Material.Rounded.CheckCircle,
    };

    private LocalizedText Label => Level switch
    {
        ProfileLevel.Complete => Copy.LevelComplete,
        _ => Copy.LevelLive,
    };
}
