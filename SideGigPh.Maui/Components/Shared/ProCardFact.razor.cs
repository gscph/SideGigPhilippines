using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>One icon + text line on a <see cref="ProCard"/> (rate, experience, towns).</summary>
public partial class ProCardFact : ComponentBase
{
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;
}
