using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Auth;

public partial class AuthPage : ComponentBase
{
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private IProfessionalProfileRepository Profiles { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        // Already signed in from a previous session? Skip the auth screen.
        var existing = await Identity.GetCurrentUserAsync();
        if (existing is not null)
        {
            await RouteAfterAuthAsync(existing);
        }
    }

    private async Task HandleAuthenticated(AuthResult result)
    {
        if (!result.IsSuccess || result.User is null)
        {
            _error = result.ErrorMessage ?? "Sign-in failed. Please try again.";
            return;
        }

        _error = null;
        await RouteAfterAuthAsync(result.User);
    }

    private async Task RouteAfterAuthAsync(AuthUser user)
    {
        // Side-gig provider path: a user with a Professional profile goes to their home;
        // a new user goes to onboarding.
        // TODO (later): the seeker/customer path — role selection -> search.
        var hasProfile = await Profiles.HasProfileAsync(user.Uid);
        Nav.NavigateTo(hasProfile ? "/professional" : "/onboarding", replace: true);
    }
}
