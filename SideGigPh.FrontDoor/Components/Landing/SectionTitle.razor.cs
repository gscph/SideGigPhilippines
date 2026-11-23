using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// A section's title in the page's one style: centered, uppercase, brand purple, its last word(s) on a soft tint
/// highlight with a small dot, and an optional plain line under it.
/// </summary>
public partial class SectionTitle
{
    [Parameter, EditorRequired] public string Lead { get; set; } = "";

    /// <summary>The highlighted last word(s).</summary>
    [Parameter, EditorRequired] public string Mark { get; set; } = "";

    [Parameter] public string? Sub { get; set; }

    /// <summary>White, for the purple band.</summary>
    [Parameter] public bool OnDark { get; set; }

    private Color TitleColor => OnDark ? Color.Inherit : Color.Primary;

    private string MarkClass => OnDark ? "fd-title-mark fd-title-mark-dark" : "fd-title-mark";

    private string SubClass => OnDark ? "fd-on-primary-muted fd-title-sub" : "mud-secondary-text fd-title-sub";
}
