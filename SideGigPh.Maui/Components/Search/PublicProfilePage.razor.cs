using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// A pro as a Seeker sees them after tapping a search result: photo, trade, stars, price,
/// experience, towns, jobs, work photos and every review. The Contact button stays pinned
/// above the tabs and opens the Inbox thread with this pro.
/// </summary>
public partial class PublicProfilePage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private ProfessionalListing? _pro;
    private IReadOnlyList<Review> _reviews = [];
    private bool _loading = true;

    private ReviewSummary? Summary => _reviews.Count == 0 ? null : new ReviewSummary(_reviews.Average(r => r.Rating), _reviews.Count);

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _pro = await Workspace.GetProAsync(Id);
        _reviews = _pro is null ? [] : await Workspace.GetReviewsAsync(Id);
        _loading = false;
    }
}
