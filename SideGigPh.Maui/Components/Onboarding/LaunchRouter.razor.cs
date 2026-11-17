using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// App entry point (no UI of its own). Applies the saved language and sends the user to
/// wherever they left off: language → sign in (+ name after a phone sign-in) → role → the
/// interview step → their home. Everyone signs in before using the app; a worker whose app was reinstalled gets their
/// live listing back first (<see cref="ListingRestore"/>).
/// </summary>
public partial class LaunchRouter : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private AppConfigService Config { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private ListingRestore Restore { get; set; } = default!;
    [Inject] private AppAnalytics Analytics { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>The main splash line, the same as the busy splash's first: "AI-assisted ID checks".</summary>
    private static LocalizedText MainLine => Copy.TrustTagline;

    protected override async Task OnInitializedAsync()
    {
        // The config first (once per launch, while the splash shows): the saved answers are checked against it as they load (OnboardingCleanup).
        await Config.LoadAsync();
        var state = await Store.LoadAsync();
        // Usage analytics and A/B variants (both off unless the build turned them on); neither holds up the launch.
        Analytics.Start(state);
        if (state.Language is not { } language)
        {
            // The sign-in screen needs their language, so this comes first.
            Nav.NavigateTo(OnboardingSteps.LanguagePath, replace: true);
            return;
        }

        T.Use(language);
        if (await Gate.RedirectAsync("/") is { } signIn)
        {
            Nav.NavigateTo(signIn, replace: true);
            return;
        }

        // Signed in on a phone with no answers (reinstalled, a new phone, or "Sign in with this number" before the first
        // question): a worker's live listing comes back, so they open Pro home instead of the interview. Every sign-in
        // comes back here (IAuthLanding's default "/").
        await Restore.RestoreAsync(state);
        Nav.NavigateTo(OnboardingSteps.LaunchPath(state), replace: true);
    }
}
