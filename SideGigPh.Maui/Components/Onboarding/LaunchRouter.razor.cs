using Microsoft.AspNetCore.Components;
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
    [Inject] private AppConfigService Config { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // The config first (once per launch, while the splash shows): the saved answers are checked against it as they load (OnboardingCleanup).
        await Config.LoadAsync();
        var state = await Store.LoadAsync();
        if (state.Language is not { } language)
        {
            // The sign-in screen needs their language, so this comes first.
            Nav.NavigateTo(OnboardingSteps.LanguagePath, replace: true);
            return;
        }

        T.Use(language);
        Nav.NavigateTo(await Gate.RedirectAsync("/") ?? OnboardingSteps.LaunchPath(state), replace: true);
    }
}
