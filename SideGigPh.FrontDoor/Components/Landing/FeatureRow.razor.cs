using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>One point in a feature list: the app's picture in a round backdrop, a title and a plain line beside it.</summary>
public partial class FeatureRow
{
    [Parameter, EditorRequired] public string Icon { get; set; } = "";

    [Parameter, EditorRequired] public string Title { get; set; } = "";

    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>A money point: the title in money green.</summary>
    [Parameter] public bool Money { get; set; }

    /// <summary>White circles, for a tint band.</summary>
    [Parameter] public bool OnTint { get; set; } = true;

    private Color TitleColor => Money ? Color.Success : Color.Default;
}
