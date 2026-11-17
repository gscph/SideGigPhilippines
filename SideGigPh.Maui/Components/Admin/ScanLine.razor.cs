using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Admin;

/// <summary>One field the AI read from an ID ("Name: …"), a dash when it read nothing there.</summary>
public partial class ScanLine : ComponentBase
{
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    [Parameter] public string? Value { get; set; }
}
