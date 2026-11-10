using Microsoft.AspNetCore.Components;
namespace SideGigPh.Maui.Components.Shared;

/// <summary>A filter (Search tab) or the town picker (Seeker home) shown as a chip with its current value ("📍 Taytay ▾"); tapping it lists the choices.</summary>
public partial class FilterMenu : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<ChoiceItem> Options { get; set; } = [];

    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    /// <summary>What the filter is ("Town"), for screen readers.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    [Parameter] public string? SelectedId { get; set; }

    [Parameter] public EventCallback<string> OnPick { get; set; }

    private string SelectedLabel => Options.FirstOrDefault(option => option.Id == SelectedId)?.Label ?? Label;
}
