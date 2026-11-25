using Microsoft.AspNetCore.Components;
using MobileAppTheme.Components.Shared;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// App entry point (no UI of its own). Applies the saved language and sends the user to
/// wherever they left off: language → sign in (+ name after a phone sign-in) → role → the
/// interview step → their home. Everyone signs in before using the app; someone whose app was reinstalled gets their
/// account back first (<see cref="AccountRestore"/>: a worker's live listing, a household's setup); when that read gets no
/// answer the page says "No internet" with Try again (also by itself once the connection is back), and "Continue anyway"
/// for someone who has nothing to lose (a new account) so nobody is stuck on it.
/// </summary>
public partial class LaunchRouter : ComponentBase, IDisposable
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private AppConfigService Config { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private AccountRestore Restore { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private INetworkStatus Network { get; set; } = default!;

    /// <summary>The main splash line, the same as the busy splash's first: "AI-assisted ID checks".</summary>
    private static LocalizedText MainLine => Copy.TrustTagline;

    // The account restore got no answer: the no-internet screen shows instead of the splash.
    private bool _offline;
    private bool _busy;
    private OnboardingState? _state;

    // The app's first visit follows index.html's splash, so it shows at once; later visits wait (see ShowSplashLaterAsync).
    private static bool s_launched;
    private bool _showSplash;
    private readonly CancellationTokenSource _leaving = new();

    protected override Task OnInitializedAsync()
    {
        Network.Changed += OnNetworkChanged;
        _showSplash = !s_launched;
        s_launched = true;
        if (!_showSplash)
        {
            _ = ShowSplashLaterAsync();
        }

        return RouteAsync();
    }

    // Choosing the language or signing in passes through here on the way to the next screen, usually in a few
    // milliseconds: the splash only shows when that takes long enough to look stuck.
    private async Task ShowSplashLaterAsync()
    {
        try
        {
            await Task.Delay(BusyOverlay.ShowAfter, _leaving.Token);
            _showSplash = true;
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Already gone to the next screen.
        }
    }

    // Back online while the no-internet screen shows: try again without a tap.
    private void OnNetworkChanged()
    {
        if (_offline && Network.IsOnline)
        {
            _ = InvokeAsync(TryAgainAsync);
        }
    }

    /// <summary>"Continue anyway": the launch goes on without the account restore, as on a new phone.</summary>
    private void ContinueAnyway()
    {
        if (_state is { } state)
        {
            Nav.NavigateTo(OnboardingSteps.LaunchPath(state), replace: true);
        }
    }

    public void Dispose()
    {
        Network.Changed -= OnNetworkChanged;
        _leaving.Cancel();
        _leaving.Dispose();
    }

    private async Task TryAgainAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            _offline = false;
            await RouteAsync();
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private async Task RouteAsync()
    {
        // The config first (once per launch, while the splash shows): the saved answers are checked against it as they load (OnboardingCleanup).
        await Config.LoadAsync();
        var state = _state = await Store.LoadAsync();
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
        // question): a worker's live listing and a household's setup come back, so they open their home instead of the
        // interview. Every sign-in comes back here (IAuthLanding's default "/").
        if (await Restore.RestoreAsync(state) == AccountRestoreResult.Offline)
        {
            _offline = true;
            return;
        }

        Nav.NavigateTo(OnboardingSteps.LaunchPath(state), replace: true);
    }
}
