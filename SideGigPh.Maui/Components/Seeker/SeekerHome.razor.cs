using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// Seeker home: "What do you need done?" with a search box, their last search one tap away,
/// recent messages, pros waiting for a rating, and every trade as a big tile to browse.
/// Searching or tapping a trade opens the Search tab with that search.
/// </summary>
public partial class SeekerHome : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>"Pa-linis ng aircon · Taytay", or null before any search.</summary>
    private string? LastSearch
    {
        get
        {
            var request = Workspace.Request;
            var job = ReferenceData.FindService(request.ServiceId) ?? ReferenceData.Find(ReferenceData.Categories, request.CategoryId);
            if (job is null)
            {
                return null;
            }

            var town = ReferenceData.Find(ReferenceData.Cities, request.CityId);
            return string.Join(" · ", new[] { job, town }.OfType<CatalogOption>().Select(option => T.Get(option.Label)));
        }
    }

    protected override Task OnInitializedAsync() => Workspace.RefreshActivityAsync();

    private async Task SearchAsync(string words)
    {
        if (string.IsNullOrWhiteSpace(words))
        {
            return;
        }

        await Workspace.SearchForAsync(words);
        Nav.NavigateTo(SeekerRoutes.Search);
    }

    private async Task PickCategoryAsync(string categoryId)
    {
        await Workspace.PickCategoryAsync(categoryId);
        Nav.NavigateTo(SeekerRoutes.Search);
    }
}
