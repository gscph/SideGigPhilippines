using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;
using MobileAppTheme.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// One-pick tiles for an interview question with an "Others" tile (trades, a Seeker's jobs). Once "Others" is picked
/// the list folds to that tile (<see cref="OtherChoice"/>), so the box for what they type sits under the question and
/// the phone's keyboard doesn't cover it; "Show the list again" opens the rest.
/// </summary>
public partial class OneChoiceTiles : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<CatalogOption> Options { get; set; } = [];

    /// <summary>The tile shown as picked, if any.</summary>
    [Parameter] public string? PickedId { get; set; }

    [Parameter] public EventCallback<string> OnPick { get; set; }

    private bool _showAll;

    private IReadOnlyCollection<string> Selected => PickedId is null ? [] : [PickedId];

    private void ShowAll() => _showAll = true;

    private Task PickAsync(string id)
    {
        _showAll = false;  // picking "Others" (again) folds the list
        return OnPick.InvokeAsync(id);
    }
}
