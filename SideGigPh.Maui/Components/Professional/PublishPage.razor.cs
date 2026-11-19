using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Going live, step 1 (<see cref="PublishStep.Checklist"/>): the worker ticks every promise (<see cref="PublishChecklist"/>:
/// honest work and prices, the Terms, the community rules, fair offers) before the Ready step, which
/// publishes. Every "Publish" link opens here; a live listing goes home (its edits never pass the promises again). The
/// verified-number check is on the Ready step, not here, so a worker who tapped "Do this later" on the interview's number
/// question isn't asked again straight away. A full-screen step with the interview look; the rules are in
/// <see cref="OnboardingSteps"/>.
/// </summary>
public partial class PublishPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
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

        // No number check here: the interview's number question just asked, and a worker who put it off is asked once
        // more on the Ready step, which is the one that publishes.
        _loaded = true;
    }

    private void Next() => Nav.NavigateTo(OnboardingSteps.PathOf(PublishStep.Ready));

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(PublishStep.Checklist));
}
