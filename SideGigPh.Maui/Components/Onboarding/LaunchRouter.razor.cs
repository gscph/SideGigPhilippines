using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// App entry point (no UI of its own). Applies the saved language and sends the user to
/// wherever they left off: language → role → the interview step → their home.
/// </summary>
public partial class LaunchRouter : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var state = await Store.LoadAsync();
        if (state.Language is { } language)
        {
            T.Use(language);
        }

        Nav.NavigateTo(Destination(state), replace: true);
    }

    private static string Destination(OnboardingState state)
    {
        if (state.Language is null)
        {
            return "/start";
        }

        if (state.Role is not { } role)
        {
            return "/start/role";
        }

        if (state.CompletedAt is null)
        {
            return state.ResumePath ?? (role == UserRole.Seeker ? "/start/seeker" : "/start/pro");
        }

        return role == UserRole.Seeker ? "/search" : "/professional";
    }
}
