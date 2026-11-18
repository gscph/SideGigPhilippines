using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Search;

/// <summary>One number on the profile's stat row: the value, gold stars under it when given, then a short caption.</summary>
public partial class ProfileStatCell : ComponentBase
{
    [Parameter, EditorRequired] public string Value { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Caption { get; set; } = string.Empty;

    /// <summary>Whole stars to draw under the value (the average, rounded); none when null.</summary>
    [Parameter] public int? Stars { get; set; }
}
