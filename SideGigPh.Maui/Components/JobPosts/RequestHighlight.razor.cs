using Microsoft.AspNetCore.Components;
using Color = MudBlazor.Color;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// One highlight on a request's own page, Airbnb style: an icon, one bold line and at most one muted line under it
/// ("You offer this service", what the customer wrote, the worker's note).
/// </summary>
public partial class RequestHighlight : ComponentBase
{
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The muted line under the title, or null for none.</summary>
    [Parameter] public string? Detail { get; set; }

    /// <summary>Ink by default; green for a done check.</summary>
    [Parameter] public Color IconColor { get; set; } = Color.Dark;
}
