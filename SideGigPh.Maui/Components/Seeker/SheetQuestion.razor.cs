using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// One question of the search sheet, like Airbnb's: open, a card with a big title ("Where?") and its answers; closed, a
/// row with the label on the left and the current answer on the right ("Where · Angono") that opens it.
/// </summary>
public partial class SheetQuestion : ComponentBase
{
    [Parameter] public bool Open { get; set; }

    /// <summary>The open card's title, e.g. "Where?" (the page's h1 while open).</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The closed row's label, e.g. "Where".</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    /// <summary>The closed row's answer, e.g. "Angono" or "Add service".</summary>
    [Parameter, EditorRequired] public string Value { get; set; } = string.Empty;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public EventCallback OnOpen { get; set; }
}
