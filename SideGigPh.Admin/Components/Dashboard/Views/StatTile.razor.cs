using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SideGigPh.Admin.Components.Dashboard.Views;

/// <summary>One number on the dashboard: an icon and label, the number, a line under it and an optional link to its list.</summary>
public partial class StatTile
{
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter, EditorRequired] public string Value { get; set; } = "";
    [Parameter] public string? Caption { get; set; }
    [Parameter] public string Icon { get; set; } = Icons.Material.Outlined.Insights;
    [Parameter] public Color IconColor { get; set; } = Color.Primary;
    [Parameter] public string? Href { get; set; }
    [Parameter] public string? LinkText { get; set; }
}
