using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The 2-way switch under the Requests tab's title: "New" (the feed, <see cref="ProPostsPage"/>) and "My offers"
/// (<see cref="MyOffersPage"/>), each its own page so a link or Back lands on the right side.
/// </summary>
public partial class RequestsSwitch : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>The page showing: <see cref="JobPostRoutes.Feed"/> or <see cref="JobPostRoutes.MyOffers"/>.</summary>
    [Parameter, EditorRequired] public string Current { get; set; } = JobPostRoutes.Feed;

    private void Go(string? route)
    {
        if (route is not null && route != Current)
        {
            Nav.NavigateTo(route);
        }
    }
}
