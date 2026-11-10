using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Shared;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// Every trade as an icon with its name under it, DoorDash/Grab style: two rows that scroll
/// sideways. Reports the tapped trade; the parent starts the search.
/// </summary>
public partial class CategoryRail : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<ChoiceItem> Items { get; set; } = [];

    /// <summary>What the list is ("Browse by service"), for screen readers.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    [Parameter] public EventCallback<string> OnPick { get; set; }
}
