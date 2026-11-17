using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Admin;

/// <summary>
/// The operator's badge review (<see cref="AdminRoutes.Badges"/>), full screen outside both sides' shells: the
/// requests waiting, oldest first, one <see cref="PendingBadgeCard"/> each. Anyone else gets a "not for you" note
/// (the server refuses them anyway). Back goes to <c>/</c>, which opens the account's own home.
/// </summary>
public partial class BadgeReviewPage : ComponentBase
{
    [Inject] private BadgeReview Review { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private bool _loading = true;
    private bool _allowed;

    protected override async Task OnInitializedAsync()
    {
        _allowed = await Review.IsOperatorAsync();
        if (_allowed)
        {
            await Review.LoadAsync();
        }

        _loading = false;
    }

    private void GoBack() => Nav.NavigateTo("/");
}
