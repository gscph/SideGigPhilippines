using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// A worker as a Seeker sees them after tapping a search result, laid out like Airbnb's listing page, top to bottom:
/// who and where (<see cref="ProfileHero"/>), a borderless stat row (<see cref="ProfileStatRow"/>), "What we checked"
/// (<see cref="ProfileTrust"/>), what they do (<see cref="JobList"/>), small work photos and the reviews
/// (<see cref="ReviewStrip"/>). The bar with the price and Contact stays pinned above the tabs (<see cref="ProfileFooter"/>).
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
}
