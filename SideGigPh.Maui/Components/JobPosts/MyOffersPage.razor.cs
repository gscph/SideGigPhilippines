using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The worker's own offers (<see cref="JobPostRoutes.MyOffers"/>, "My offers" side of the Requests switch): newest
/// change first, one short <see cref="RequestRow"/> each (you offered ₱X, picked you, not picked), read fresh on every
/// visit; a tap opens the request's own page.
/// </summary>
public partial class MyOffersPage : ComponentBase
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private IReadOnlyList<MyOffer>? _offers;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync() => _offers = await Board.MyOffersAsync();
}
