using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Components.Onboarding;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Going live: the pro sees their card one last time and takes a short pledge (answer
/// customers, honest work at the posted price) before publishing. A focused full-screen
/// step, so it uses the interview look rather than the Pro home shell.
/// </summary>
public partial class PublishPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private AppAnalytics Analytics { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<PublishPage> Logger { get; set; } = default!;

    private bool _loaded;
    private bool _pledged;
    private bool _busy;
    private LocalizedText? _problem;
    private bool _badReference;
    private bool _failed;

    protected override async Task OnInitializedAsync()
    {
        if (!await Workspace.EnsureDraftLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        if (Workspace.IsPublished)
        {
            Nav.NavigateTo(ProRoutes.Home, replace: true);
            return;
        }

        // A listing missing answers (no jobs, area or towns) goes back to the first unanswered question.
        if (!Workspace.Draft.IsListingComplete)
        {
            Nav.NavigateTo(ProfessionalInterview.FirstUnansweredPath, replace: true);
            return;
        }

        // Own photo, 2 work photos and a reference come first: the setup opens at the first one missing.
        if (!Workspace.Draft.HasProof)
        {
            Nav.NavigateTo(Workspace.Draft.AfterInterviewPath(), replace: true);
            return;
        }

        // The listing's number is the account's verified number (a Google sign-in adds one here first). Its Back goes
        // to the setup's last step, since home would resume right back here; "Do this later" goes to Pro home, whose
        // banner and checklist say the number is still needed to publish (no dead end when today's codes are used up).
        if (await Gate.RedirectAsync(ProRoutes.Publish, requirePhone: true, OnboardingSteps.PathOf(ProSetupStep.References),
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
        _badReference = false;
        _failed = false;
        try
        {
            var result = await Busy.RunAsync(Workspace.PublishAsync);
            if (result == PublishResult.Incomplete)
            {
                Nav.NavigateTo(ProfessionalInterview.FirstUnansweredPath, replace: true);
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
                _ => null,
            };
            _badReference = result == PublishResult.BadReference;
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

        if (_problem is null && !_badReference && !_failed)
        {
            Analytics.Track(AppEvents.ListingPublished);
            Nav.NavigateTo(ProRoutes.Home, replace: true);
        }
    }

    // The step before it in the setup after the interview (references).
    private void GoBack() => Nav.NavigateTo(OnboardingSteps.PathOf(ProSetupStep.References));
}
