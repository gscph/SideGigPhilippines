using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// Seeker home, like Airbnb's Explore: one "Start your search" pill (<see cref="SearchPill"/>, opens the search sheet),
/// every trade as a tile three across, the row to post what they need done (<see cref="PostRequestCard"/>, so posting is
/// seen on landing, not only on the Post tab), then their recent searches (kept on the phone, <see cref="RecentSearches"/>;
/// a tap searches again, "Post" opens the post form filled with it), recent messages and pros waiting for a rating.
/// Tapping a trade opens the results for it.
/// </summary>
public partial class SeekerHome : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<ChoiceItem> Categories => ChoiceItem.From(ReferenceData.Categories, T);

    protected override Task OnInitializedAsync() => Workspace.RefreshIfStaleAsync();

    private async Task SearchAgainAsync(RecentSearch recent)
    {
        await Workspace.UseRecentAsync(recent);
        Nav.NavigateTo(SeekerRoutes.Search);
    }

    private async Task PostAgainAsync(RecentSearch recent)
    {
        await Workspace.UseRecentAsync(recent);
        Nav.NavigateTo(JobPosts.JobPostRoutes.New);
    }

    private async Task PickCategoryAsync(string categoryId)
    {
        await Workspace.PickCategoryAsync(categoryId);
        Nav.NavigateTo(SeekerRoutes.Search);
    }
}
