using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// A pro as a Seeker sees them after tapping a search result, top to bottom: who they are
/// (<see cref="ProfileHero"/>), their three numbers (<see cref="ProfileStats"/>), why to trust them
/// (<see cref="ProfileTrust"/>: badges, verified number), the jobs they take (<see cref="JobList"/>),
/// work photos and every review. The Contact button stays pinned above the tabs and opens the Inbox thread.
/// </summary>
public partial class PublicProfilePage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    /// <summary>Where Back goes (e.g. the offers on a post); the results when missing.</summary>
    [SupplyParameterFromQuery(Name = "back")] public string? Back { get; set; }

    private ProfessionalListing? _pro;
    private IReadOnlyList<Review> _reviews = [];
    private bool _loading = true;

    private ReviewSummary? Summary => _reviews.Count == 0 ? null : new ReviewSummary(_reviews.Average(r => r.Rating), _reviews.Count);

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        // Side by side: the listing usually comes from the search just made, the reviews from the server.
        await Busy.RunAsync(async () =>
        {
            var reviews = Workspace.GetReviewsAsync(Id);
            _pro = await Workspace.GetProAsync(Id);
            _reviews = _pro is null ? [] : await reviews;
        });
        _loading = false;
    }

    /// <summary>Report another person's review; none on the Seeker's own.</summary>
    private string? ReviewReportHref(Review review) =>
        review.SeekerId == Workspace.MyId ? null : SeekerRoutes.Report(review.SeekerId, ReportSubject.Review, SeekerRoutes.Pro(Id));
}
