using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The top of a worker's public profile when they added work photos, like Airbnb's listing page: the photos edge to
/// edge, swiped sideways, each opening full screen on a tap, with the page's buttons (<see cref="ChildContent"/>) on
/// white circles over the photo. A worker with no work photos gets no cover (the page keeps its plain top).
/// </summary>
public partial class ProfileCover : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The worker's id, for the full-screen photo links.</summary>
    [Parameter, EditorRequired] public string ProId { get; set; } = string.Empty;

    /// <summary>The work photos, at least one.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<string> Photos { get; set; } = [];

    /// <summary>Back and Report, laid over the photo's top corners.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
