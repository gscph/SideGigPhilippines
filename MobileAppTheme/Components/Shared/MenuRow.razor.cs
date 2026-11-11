using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Shared;

/// <summary>One row of a settings-style list (Airbnb's Profile tab): icon, label, optional second line, chevron, hairline under it.</summary>
public partial class MenuRow : ComponentBase
{
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;

    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>A red count on the icon; hidden at 0.</summary>
    [Parameter] public int Badge { get; set; }
}
