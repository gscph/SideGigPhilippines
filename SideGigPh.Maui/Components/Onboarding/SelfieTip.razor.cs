using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>One line of the selfie guide (<see cref="SelfieGuide"/>): an icon and a short tip.</summary>
public partial class SelfieTip : ComponentBase
{
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;
}
