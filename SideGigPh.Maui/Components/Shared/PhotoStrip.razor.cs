using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Work photos two across, scrolling sideways when there are more (snapping to each photo). Used on the public
/// profile (Seekers see a worker's past work) and on the worker's own work-photo page. Each child is one photo.
/// </summary>
public partial class PhotoStrip : ComponentBase
{
    /// <summary>What the photos are, for screen readers.</summary>
    [Parameter] public string? Label { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
