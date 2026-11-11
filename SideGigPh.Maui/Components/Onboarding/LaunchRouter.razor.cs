using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// App entry point (no UI of its own). Applies the saved language and sends the user to
/// wherever they left off: language → sign in (+ name after a phone sign-in) → role → the
/// interview step → their home. Everyone signs in before using the app.
/// </summary>
public partial class LaunchRouter : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var state = await Store.LoadAsync();
        if (state.Language is not { } language)
        {
            // The sign-in screen needs their language, so this comes first.
            Nav.NavigateTo("/start", replace: true);
            return;
        }

        T.Use(language);
        Nav.NavigateTo(await Gate.RedirectAsync("/") ?? Destination(state), replace: true);
    }

    private static string Destination(OnboardingState state)
    {
        if (state.Role is not { } role)
        {
            return "/start/role";
        }

        if (state.CompletedAt is null)
        {
            return state.ResumePath ?? (role == UserRole.Seeker ? "/start/seeker" : "/start/pro");
        }

        return role == UserRole.Seeker ? SeekerRoutes.Home : ProRoutes.Home;
    }
}
