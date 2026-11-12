using Microsoft.AspNetCore.Components;
using Color = MudBlazor.Color;

namespace SideGigPh.Maui.Components.Search;

/// <summary>One boxed number on the profile's stat row: an icon, a short value ("4.8", "10+ years") and a caption.</summary>
public partial class StatTile : ComponentBase
{
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Value { get; set; } = string.Empty;

    [Parameter] public string? Caption { get; set; }

    /// <summary>Gold for stars, green for money, the brand color for 10+ years; ink otherwise.</summary>
    [Parameter] public Color IconColor { get; set; } = Color.Dark;

    /// <summary>Also paints the value in <see cref="IconColor"/>.</summary>
    [Parameter] public bool Highlight { get; set; }
}
