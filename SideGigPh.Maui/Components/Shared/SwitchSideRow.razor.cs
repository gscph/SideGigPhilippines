using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// The Me page row that switches to the other side (<see cref="SideSwitch"/>): "Switch to customer" on the worker's Me,
/// "Switch to Skilled Worker" on the household's. Its second line says whether that side is set up already or starts
/// with a few questions, and that nothing on this side is lost.
/// </summary>
public partial class SwitchSideRow : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The side the row switches to.</summary>
    [Parameter, EditorRequired] public UserRole To { get; set; }

    private bool _setUp;

    private LocalizedText Label => To == UserRole.Seeker ? Copy.SwitchToSeeker : Copy.SwitchToPro;

    private LocalizedText Hint => (To, _setUp) switch
    {
        (UserRole.Seeker, true) => Copy.SwitchToSeekerHint,
        (UserRole.Seeker, false) => Copy.SwitchToSeekerNewHint,
        (_, true) => Copy.SwitchToProHint,
        _ => Copy.SwitchToProNewHint,
    };

    protected override async Task OnParametersSetAsync() => _setUp = (await Store.LoadAsync()).HasFinished(To);
}
