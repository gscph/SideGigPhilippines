using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Big tap tiles (icon + label + optional hint) for interview answers. It only reports
/// which tile was tapped; the parent decides whether that selects, toggles or advances.
/// </summary>
public partial class ChoiceTiles : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<ChoiceItem> Items { get; set; } = [];

    [Parameter] public IReadOnlyCollection<string> SelectedIds { get; set; } = [];

    /// <summary>Shows a checkbox on each tile, for "pick all that apply" questions.</summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>Two-column grid, for short labels such as town names.</summary>
    [Parameter] public bool Compact { get; set; }

    [Parameter] public EventCallback<string> OnPick { get; set; }
}
