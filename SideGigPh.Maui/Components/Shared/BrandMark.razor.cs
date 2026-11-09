using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>The SideGigPh pin logo and name, with an optional tagline under it.</summary>
public partial class BrandMark : ComponentBase
{
    [Parameter] public string? Tagline { get; set; }
}
