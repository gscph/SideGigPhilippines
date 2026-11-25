using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// The Me page row that switches to the other side (<see cref="SideSwitch"/>): "Switch to customer" on the worker's Me
/// (its second line says whether that side is set up or starts with a few questions, and that the listing stays), and
/// "Switch to Skilled Worker" on the household's once that side is set up; before then the household's Me shows
/// <c>BecomeWorkerCard</c> instead.
/// </summary>
public partial class SwitchSideRow : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The side the row switches to.</summary>
    [Parameter, EditorRequired] public UserRole To { get; set; }

    private bool _setUp;

    private LocalizedText Label => To == UserRole.Seeker ? Copy.SwitchToSeeker : Copy.SwitchToPro;

    private LocalizedText Hint => To == UserRole.Professional ? Copy.SwitchToProHint
        : _setUp ? Copy.SwitchToSeekerHint
        : Copy.SwitchToSeekerNewHint;

    protected override async Task OnParametersSetAsync() => _setUp = (await Store.LoadAsync()).HasFinished(To);
}
