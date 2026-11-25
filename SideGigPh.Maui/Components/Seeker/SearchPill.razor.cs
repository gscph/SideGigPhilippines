using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>Seeker home's one search entry, like Airbnb's "Start your search" pill: it opens the search sheet.</summary>
public partial class SearchPill : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    private static string Href => SeekerRoutes.Find(SeekerRoutes.Home);
}
