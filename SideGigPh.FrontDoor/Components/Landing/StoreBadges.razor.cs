using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// Where to get the app: "Android · Soon" as a dimmed tile, and "iPhone · Beta on TestFlight" as a live link to the public
/// TestFlight beta. Our own tiles, never the official store badges (the stores' brand rules allow those only once the
/// listings exist).
/// </summary>
public partial class StoreBadges
{
    /// <summary>White outlines, for the purple band.</summary>
    [Parameter] public bool OnDark { get; set; }

    [Parameter] public bool Center { get; set; }

    private string SoonClass => OnDark ? "fd-store-badge fd-store-soon fd-store-badge-dark" : "fd-store-badge fd-store-soon";

    private string LiveClass => OnDark ? "fd-store-badge fd-store-badge-dark" : "fd-store-badge";

    private Color LiveColor => OnDark ? Color.Inherit : Color.Dark;
}
