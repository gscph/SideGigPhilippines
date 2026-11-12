using Microsoft.AspNetCore.Components;
using Color = MudBlazor.Color;

namespace SideGigPh.Maui.Components.Search;

/// <summary>One reason to trust a pro: an icon, a short title and a gray note under it.</summary>
public partial class TrustRow : ComponentBase
{
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter] public string? Note { get; set; }

    /// <summary>Green (a done check) by default.</summary>
    [Parameter] public Color IconColor { get; set; } = Color.Success;
}
