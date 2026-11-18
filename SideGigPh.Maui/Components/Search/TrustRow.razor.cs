using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// One highlight row, like Airbnb's "My qualifications": an ink line icon, a short bold title and at most one muted line.
/// </summary>
public partial class TrustRow : ComponentBase
{
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter] public string? Note { get; set; }
}
