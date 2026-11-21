using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// A worker as a Seeker sees them after tapping a search result, laid out like Airbnb's listing page, top to bottom:
/// their work photos edge to edge (<see cref="ProfileCover"/>, only when they added some), who and where
/// (<see cref="ProfileHero"/>, its photo overlapping the cover), a borderless stat row (<see cref="ProfileStatRow"/>),
/// "What we checked" (<see cref="ProfileTrust"/>), what they do (<see cref="JobList"/>) and the reviews
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

    /// <summary>The work photos open the page as a cover (Airbnb style); without any, the plain centered top.</summary>
    private bool HasCover => !_loading && _pro is { WorkPhotoUrls.Count: > 0 };

    private string BackHref => SeekerRoutes.BackOrSearch(Back);

    private string? ReportHref => _pro is not null && _pro.Id != Workspace.MyId
        ? SeekerRoutes.Report(_pro.Id, ReportSubject.Listing, SeekerRoutes.Pro(_pro.Id))
        : null;

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
