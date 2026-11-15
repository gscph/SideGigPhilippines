using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// An empty list, Airbnb style: an outlined icon in a soft brand-tinted circle above a short gray line
/// ("No messages yet…"), centered with room around it.
/// </summary>
public partial class EmptyState : ComponentBase
{
    /// <summary>The picture: an outlined MudBlazor icon that says what the list holds.</summary>
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;
}
