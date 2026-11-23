using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>The Folded T logo and the "Tarbaho" wordmark: brand purple on white, white on the purple band.</summary>
public partial class BrandMark
{
    [Parameter] public bool OnDark { get; set; }

    private Color MarkColor => OnDark ? Color.Inherit : Color.Primary;
}
