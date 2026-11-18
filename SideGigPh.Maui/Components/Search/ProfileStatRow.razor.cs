using System.Globalization;
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The profile's numbers in one borderless row split by thin lines, like Airbnb's "4.92 ★ | Guest favorite | 260
/// Reviews": stars (or New), services done through Tarbaho, years of experience (<see cref="ProfileFacts.Stats"/>).
/// </summary>
public partial class ProfileStatRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>Stars from the reviews just read, or null with none yet ("New").</summary>
    [Parameter] public ReviewSummary? Reviews { get; set; }

    private IReadOnlyList<ProfileStat> Stats => ProfileFacts.Stats(Listing, Reviews);

    private string Average => (Reviews?.Average ?? 0).ToString("0.0", CultureInfo.InvariantCulture);

    private string Done => Listing.CompletedJobs.ToString(CultureInfo.InvariantCulture);

    private int RoundedStars => (int)Math.Round(Reviews?.Average ?? 0, MidpointRounding.AwayFromZero);

    private string ReviewCount => Reviews is { Count: 1 } ? T.Get(Copy.StatOneReview) : T.Format(Copy.StatReviews, Reviews?.Count ?? 0);
}
