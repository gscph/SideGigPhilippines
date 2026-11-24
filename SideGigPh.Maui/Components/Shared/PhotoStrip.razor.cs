using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Photos two across, scrolling sideways when there are more (snapping to each photo): a job post's photos, on the
/// household's post card, its form and the worker's request page. Each child is one photo. (The worker's own work
/// photos are a 2 × 3 grid, <c>WorkPhotoGrid</c>.)
/// </summary>
public partial class PhotoStrip : ComponentBase
{
    /// <summary>What the photos are, for screen readers.</summary>
    [Parameter] public string? Label { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
