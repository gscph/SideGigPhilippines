using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The worker's own offers (<see cref="JobPostRoutes.MyOffers"/>, from the Requests tab): newest change first, one
/// <see cref="MyOfferCard"/> each, read fresh on every visit.
/// </summary>
public partial class MyOffersPage : ComponentBase
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private IReadOnlyList<MyOffer>? _offers;

    protected override async Task OnInitializedAsync() => _offers = await Board.MyOffersAsync();
}
