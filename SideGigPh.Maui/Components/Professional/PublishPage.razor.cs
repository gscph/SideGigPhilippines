using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Going live, step 1 (<see cref="PublishStep.Checklist"/>): the worker ticks every promise (<see cref="PublishChecklist"/>:
/// honest work and prices, the Terms, the community rules, fair offers) before the Ready step, which shows the card and
/// publishes. Every "Publish" link opens here; a live listing goes home (its edits never pass the promises again). The
/// verified-number check stays here, before the promises, as it was before the pledge. A full-screen step with the
/// interview look; the rules are in <see cref="OnboardingSteps"/>.
/// </summary>
public partial class PublishPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _loaded;

    private static double Progress => OnboardingSteps.ProgressOf(PublishStep.Checklist);

    protected override async Task OnInitializedAsync()
    {
        if (!await Workspace.EnsureDraftLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        if (Workspace.Draft.PublishRedirect(PublishStep.Checklist, Workspace.Commitments.AllTicked) is { } elsewhere)
        {
            Nav.NavigateTo(elsewhere, replace: true);
            return;
        }

        // The listing's number is the account's verified number (a Google sign-in adds one here first). Its Back goes
        // to the setup's last step, since home would resume right back here; "Do this later" goes to Pro home, whose
        // banner and checklist say the number is still needed to publish (no dead end when today's codes are used up).
        if (await Gate.RedirectAsync(ProRoutes.Publish, requirePhone: true, OnboardingSteps.BackPath(PublishStep.Checklist),
                laterPath: ProRoutes.Home) is { } step)
        {
            Nav.NavigateTo(step, replace: true);
            return;
        }

        _loaded = true;
    }

    private void Next() => Nav.NavigateTo(OnboardingSteps.PathOf(PublishStep.Ready));

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(PublishStep.Checklist));
}
