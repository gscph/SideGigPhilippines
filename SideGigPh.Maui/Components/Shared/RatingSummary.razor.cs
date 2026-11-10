using System.Globalization;
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>Average stars, review count, and how many reviews gave each star (5 down to 1).</summary>
public partial class RatingSummary : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>At least one review; the page shows an empty state instead of this when there are none.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Review> Reviews { get; set; } = [];

    private double Mean => Reviews.Average(r => r.Rating);

    private string Average => Mean.ToString("0.0", CultureInfo.InvariantCulture);

    private int Rounded => (int)Math.Round(Mean, MidpointRounding.AwayFromZero);

    private string CountLabel => Reviews.Count == 1
        ? T.Get(Copy.ReviewsCountOne)
        : T.Format(Copy.ReviewsCount, Reviews.Count);

    private double Share(int stars) => Reviews.Count(r => r.Rating == stars) * 100.0 / Reviews.Count;
}
