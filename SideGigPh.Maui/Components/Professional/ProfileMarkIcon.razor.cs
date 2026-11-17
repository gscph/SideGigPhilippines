using Microsoft.AspNetCore.Components;
using MudBlazor;
using Color = MudBlazor.Color;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The small mark at the end of a Me page row (work photos, references, badges): red when publishing needs it, green
/// when it's enough, the verified blue when it's full, amber for a badge worth adding, gray while one is being checked.
/// </summary>
public partial class ProfileMarkIcon : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfileMark Mark { get; set; }

    private Color MarkColor => Mark switch
    {
        ProfileMark.Missing => Color.Error,
        ProfileMark.Done => Color.Success,
        ProfileMark.Full => Color.Tertiary,
        ProfileMark.Recommended => Color.Info,
        _ => Color.Secondary,
    };

    private string MarkIcon => Mark switch
    {
        ProfileMark.Missing => Icons.Material.Rounded.Error,
        ProfileMark.Done => Icons.Material.Rounded.CheckCircle,
        ProfileMark.Full => Icons.Material.Rounded.Verified,
        ProfileMark.Recommended => Icons.Material.Rounded.TrendingUp,
        _ => Icons.Material.Rounded.HourglassTop,
    };

    private LocalizedText Label => Mark switch
    {
        ProfileMark.Missing => Copy.PartMissing,
        ProfileMark.Done => Copy.PartDone,
        ProfileMark.Full => Copy.PartFull,
        ProfileMark.Recommended => Copy.PartRecommended,
        _ => Copy.PartWaiting,
    };
}
