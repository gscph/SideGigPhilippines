using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Layout;

/// <summary>One bottom tab: an icon over a label; the active one takes the brand color (no pill, Airbnb style).</summary>
public partial class AppNavItem : ComponentBase
{
    [Parameter, EditorRequired] public AppTab Tab { get; set; } = default!;

    [Parameter] public bool Active { get; set; }

}
