using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Going live, step 2 (<see cref="PublishStep.Ready"/>): the worker sees their card one last time and taps Ready, which
/// publishes. Only after every promise on the checklist is ticked in this run of the app; otherwise (a link, the app
/// killed and reopened) it goes back to the checklist. Back returns to the checklist with the ticks kept. A full-screen
/// step with the interview look; the rules are in <see cref="OnboardingSteps"/>.
/// </summary>
public partial class PublishReadyPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private AppAnalytics Analytics { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<PublishReadyPage> Logger { get; set; } = default!;

    private bool _loaded;
    private bool _busy;
    private LocalizedText? _problem;
    private bool _failed;

    private static double Progress => OnboardingSteps.ProgressOf(PublishStep.Ready);

    protected override async Task OnInitializedAsync()
    {
        if (!await Workspace.EnsureDraftLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        if (Workspace.Draft.PublishRedirect(PublishStep.Ready, Workspace.Commitments.AllTicked) is { } elsewhere)
        {
            Nav.NavigateTo(elsewhere, replace: true);
            return;
        }

        // The listing's number is the account's verified number (asked in the interview; a worker who put it off adds it
        // here, the step that publishes). Its Back goes to the checklist; "Do this later" goes to Pro home, whose banner
        // and checklist say the number is still needed to publish (no dead end when today's codes are used up).
        if (await Gate.RedirectAsync(ProRoutes.PublishReady, requirePhone: true, OnboardingSteps.PathOf(PublishStep.Checklist),
                laterPath: ProRoutes.Home) is { } step)
        {
            Nav.NavigateTo(step, replace: true);
            return;
        }

        _loaded = true;
    }

    private async Task PublishAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _problem = null;
        _failed = false;
        try
        {
            var result = await Busy.RunAsync(Workspace.PublishAsync);
            if (result == PublishResult.Incomplete)
            {
                Nav.NavigateTo(OnboardingSteps.ProFirstUnansweredPath, replace: true);
                return;
            }

            if (result == PublishResult.MissingProof)
            {
                Nav.NavigateTo(Workspace.Draft.AfterInterviewPath(), replace: true);
                return;
            }

            _problem = result switch
            {
                PublishResult.PhoneTaken => Copy.PublishPhoneTaken,
                PublishResult.PhotoInUse => Copy.PublishPhotoInUse,
                PublishResult.TooManyTries => Copy.PublishTooManyTries,
                PublishResult.TextNotAllowed => Copy.TextNotAllowed,
                _ => null,
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Publishing the listing failed.");

            // The server answered and refused (its code is in the log above, so in the error report): not the internet.
            _problem = ex is HttpRequestException { StatusCode: not null } ? Copy.PublishRefused : null;
            _failed = _problem is null;
        }
        finally
        {
            _busy = false;
        }

        if (_problem is null && !_failed)
        {
            Analytics.Track(AppEvents.ListingPublished);
            Nav.NavigateTo(ProRoutes.Home, replace: true);
        }
    }

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(PublishStep.Ready));
}
