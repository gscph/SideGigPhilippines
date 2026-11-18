using System.Globalization;
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The profile's reviews, like Airbnb's: "★ 4.9 · 12 reviews", a sideways row of review cards (those with words first,
/// <see cref="ProfileFacts.InRow"/>) and a gray full-width "Show all 12 reviews" to <see cref="ProReviewsPage"/>.
/// </summary>
public partial class ReviewStrip : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<Review> Reviews { get; set; } = [];

    /// <summary>Where "Show all" goes.</summary>
    [Parameter, EditorRequired] public string AllHref { get; set; } = string.Empty;

    private ReviewSummary? Summary => ProfileFacts.Summary(Reviews);

    private IReadOnlyList<Review> InRow => ProfileFacts.InRow(Reviews);

    private string Heading => Summary is { } summary
        ? $"{summary.Average.ToString("0.0", CultureInfo.InvariantCulture)} · {Count(summary.Count)}"
        : string.Empty;

    private string ShowAll => Reviews.Count == 1 ? T.Get(Copy.ProfileShowReview) : T.Format(Copy.ProfileShowAllReviews, Reviews.Count);

    private string Count(int count) => count == 1 ? T.Get(Copy.StatOneReview) : T.Format(Copy.StatReviews, count);
}
