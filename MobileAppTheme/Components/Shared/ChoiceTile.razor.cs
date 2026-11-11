using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// One answer in <see cref="ChoiceTiles"/>, Airbnb style: a bordered row with the label (and hint)
/// on the left and the icon on the right; the chosen one gets an ink border and a gray tint.
/// </summary>
public partial class ChoiceTile : ComponentBase
{
    [Parameter, EditorRequired] public ChoiceItem Item { get; set; } = default!;

    [Parameter] public bool Selected { get; set; }

    /// <summary>Shows a checkbox, for "pick all that apply" questions.</summary>
    [Parameter] public bool ShowCheck { get; set; }

    [Parameter] public EventCallback<string> OnPick { get; set; }

    private string CssClass => Selected ? "choice-tile choice-tile-selected" : "choice-tile";
}
