using Microsoft.AspNetCore.Components;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>One of the app's pictures in a soft round backdrop (tint on white, white on the purple band). Decorative: no alt text.</summary>
public partial class IconCircle
{
    [Parameter, EditorRequired] public string Src { get; set; } = "";

    /// <summary>The circle's size in pixels; the picture fills 60% of it.</summary>
    [Parameter] public int Size { get; set; } = 56;

    /// <summary>White backdrop, for the purple band.</summary>
    [Parameter] public bool OnDark { get; set; }

    private int ImageSize => (int)(Size * 0.6);

    private string SizeStyle => $"width:{Size}px;height:{Size}px;flex-shrink:0";

    private string CircleClass => OnDark ? "fd-icon-circle fd-icon-circle-white" : "fd-icon-circle";
}
