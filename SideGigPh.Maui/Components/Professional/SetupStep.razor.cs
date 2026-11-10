using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>One line of the setup checklist: done tick, what to do, why, and a link to do it.</summary>
public partial class SetupStep : ComponentBase
{
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Why { get; set; } = string.Empty;

    [Parameter] public bool Done { get; set; }
}
