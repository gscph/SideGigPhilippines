using Microsoft.AspNetCore.Components;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// A real app screen: one of the store's phone screenshots (docs/store-assets/phone/, linked by the csproj as
/// images/app/), cropped to the phone, so the page changes when the store pictures do.
/// </summary>
public partial class PhoneShot
{
    [Parameter, EditorRequired] public string Src { get; set; } = "";

    [Parameter, EditorRequired] public string Alt { get; set; } = "";

    /// <summary>The frame's width and placement (the fd-phone… classes).</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Load it only when it scrolls near (below the fold); false for the hero, which is the first thing seen.</summary>
    [Parameter] public bool Lazy { get; set; } = true;

    private string Loading => Lazy ? "lazy" : "eager";

    private string FrameClass => $"fd-phone-frame {Class}".Trim();
}
