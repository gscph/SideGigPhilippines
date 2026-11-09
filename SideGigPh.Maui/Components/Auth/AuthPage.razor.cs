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
        // Not yet linked from the onboarding flow (auth placement is still being designed).
        // A user with a Professional profile goes to their home; anyone else goes back to the
        // launch router, which resumes their onboarding.
        var hasProfile = await Profiles.HasProfileAsync(user.Uid);
        Nav.NavigateTo(hasProfile ? "/professional" : "/", replace: true);
    }
}
