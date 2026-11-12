using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The towns we serve as stacked answer tiles. A Seeker taps their one home town; a Skilled Worker
/// ticks several, or all of them with "Select all". It only reports taps; the parent decides.
/// </summary>
public partial class TownPicker : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public IReadOnlyCollection<string> SelectedIds { get; set; } = [];

    /// <summary>Several towns may be ticked; shows "Select all" / "Remove all".</summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>A town was tapped.</summary>
    [Parameter] public EventCallback<string> OnPick { get; set; }

    /// <summary>"Select all" / "Remove all" was tapped.</summary>
    [Parameter] public EventCallback OnPickAll { get; set; }

    private bool AllSelected => ReferenceData.Cities.All(town => SelectedIds.Contains(town.Id));

    private IReadOnlyList<ChoiceItem> Tiles => ChoiceItem.From(ReferenceData.Cities, T);
}
