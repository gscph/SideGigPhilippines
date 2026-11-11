using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Layout;

/// <summary>One bottom tab: an icon over a label, with a tinted pill behind the active one.</summary>
public partial class AppNavItem : ComponentBase
{
    [Parameter, EditorRequired] public AppTab Tab { get; set; } = default!;

    [Parameter] public bool Active { get; set; }

    private string PillClass => Active ? "rounded-pill px-5 py-1 mud-background-gray" : "rounded-pill px-5 py-1";
}
