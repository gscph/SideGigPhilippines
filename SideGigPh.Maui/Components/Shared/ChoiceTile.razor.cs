using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>One big answer button in <see cref="ChoiceTiles"/>: icon, label, optional hint and checkbox.</summary>
public partial class ChoiceTile : ComponentBase
{
    [Parameter, EditorRequired] public ChoiceItem Item { get; set; } = default!;

    [Parameter] public bool Selected { get; set; }

    /// <summary>Shows a checkbox, for "pick all that apply" questions.</summary>
    [Parameter] public bool ShowCheck { get; set; }

    [Parameter] public EventCallback<string> OnPick { get; set; }

    // A selected tile keeps the primary border and gets the theme's primary tint behind it.
    private string CssClass => Selected
        ? "choice-tile justify-start border-2 mud-primary-hover"
        : "choice-tile justify-start border-2";
}
