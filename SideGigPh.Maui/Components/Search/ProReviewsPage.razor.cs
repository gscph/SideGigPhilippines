using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// Every review of a worker, opened from "Show all" on their profile, like Airbnb's reviews page: the big average with
/// the star bars (<c>RatingSummary</c>), then "12 reviews" and each review in full with its 🚩.
/// </summary>
public partial class ProReviewsPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private IReadOnlyList<Review> _reviews = [];
    private bool _loading = true;

    private string CountTitle => _reviews.Count == 1 ? T.Get(Copy.StatOneReview) : T.Format(Copy.StatReviews, _reviews.Count);

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        await Busy.RunAsync(async () => _reviews = await Workspace.GetReviewsAsync(Id));
        _loading = false;
    }

    /// <summary>Report another person's review; none on the Seeker's own.</summary>
    private string? ReportHref(Review review) =>
        review.SeekerId == Workspace.MyId ? null : SeekerRoutes.Report(review.SeekerId, ReportSubject.Review, SeekerRoutes.ProReviews(Id));
}
