using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A small map with a pin (<see cref="MapLinks.Frame"/>: OpenStreetMap's embeddable view, free, no key), in a rounded
/// outlined box. Needs the internet; offline the box stays blank. MudBlazor has no web-frame component, so the frame
/// is written here in code (the one raw element, never in markup); it is lazy-loaded and sends no referrer.
/// </summary>
public partial class MapFrame : ComponentBase
{
    [Parameter, EditorRequired] public GeoPoint Point { get; set; }

    /// <summary>What the map shows, for screen readers (the frame's title).</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    private RenderFragment Frame => builder => Build(builder);

    private void Build(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "iframe");
        builder.AddAttribute(1, "src", MapLinks.Frame(Point));
        builder.AddAttribute(2, "title", Title);
        builder.AddAttribute(3, "loading", "lazy");
        builder.AddAttribute(4, "referrerpolicy", "no-referrer");
        builder.AddAttribute(5, "class", "app-map-frame-view");
        builder.CloseElement();
    }
}
