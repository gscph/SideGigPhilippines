using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The worker's profile level under their name on the Me page: red "not visible yet", green "live", or the verified
/// blue check of a fully set-up profile (<see cref="ProfileProgress.Level"/>).
/// </summary>
public partial class ProfileLevelChip : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfileLevel Level { get; set; }

    private Color ChipColor => Level switch
    {
        ProfileLevel.Complete => Color.Tertiary,
        ProfileLevel.Live => Color.Success,
        _ => Color.Error,
    };

    private string ChipIcon => Level switch
    {
        ProfileLevel.Complete => Icons.Material.Rounded.Verified,
        ProfileLevel.Live => Icons.Material.Rounded.CheckCircle,
        _ => Icons.Material.Rounded.VisibilityOff,
    };

    private LocalizedText Label => Level switch
    {
        ProfileLevel.Complete => Copy.LevelComplete,
        ProfileLevel.Live => Copy.LevelLive,
        _ => Copy.LevelNotLive,
    };
}
