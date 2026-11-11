using Microsoft.AspNetCore.Components;
namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A filter or picker shown as a chip with its current value ("📍 Downtown ▾"); tapping it lists the
/// choices. Long lists scroll, and items with a <see cref="ChoiceItem.Group"/> get a heading per group.
/// </summary>
public partial class FilterMenu : ComponentBase
{
    /// <summary>Taller lists scroll inside the menu instead of running off the screen.</summary>
    private const int MaxListHeight = 360;

    [Parameter, EditorRequired] public IReadOnlyList<ChoiceItem> Options { get; set; } = [];

    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    /// <summary>What the filter is ("Town"), for screen readers.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    [Parameter] public string? SelectedId { get; set; }

    [Parameter] public EventCallback<string> OnPick { get; set; }

    private string SelectedLabel => Options.FirstOrDefault(option => option.Id == SelectedId)?.Label ?? Label;

    /// <summary>True for the first item of a new group, which gets the group's heading above it.</summary>
    private bool StartsGroup(int index) =>
        Options[index].Group is { } group && (index == 0 || Options[index - 1].Group != group);
}
