using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Shared;

namespace SideGigPh.Maui.Components.Search;

/// <summary>A sideways-scrolling row of pick-one chips (trades, jobs); the selected one is filled.</summary>
public partial class ChipStrip : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<ChoiceItem> Items { get; set; } = [];

    [Parameter] public string? SelectedId { get; set; }

    /// <summary>What the row is for, read out by screen readers.</summary>
    [Parameter] public string? Label { get; set; }

    [Parameter] public EventCallback<string> OnPick { get; set; }
}
