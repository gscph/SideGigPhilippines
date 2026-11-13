using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Professional (Skilled Worker) interview: trades → jobs → towns → experience → rate → earnings goal →
/// preview. The answers build the listing card the pro sees at the end; its name and number come
/// from the account (<see cref="ListingMapper.TakeAccount"/>), so they are never asked here.
/// One URL per step; answers are saved whenever the user moves forward. The rules (which step, what an
/// answer clears, where Back goes) are in <see cref="OnboardingSteps"/>.
/// </summary>
public partial class ProfessionalInterview : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>
    /// Opens the first question still unanswered: a step past it is never shown, so asking for the last step
    /// lands there (or on the preview when everything is answered). Publish sends incomplete listings here.
    /// </summary>
    public static string FirstUnansweredPath => OnboardingSteps.ProFirstUnansweredPath;

    [Parameter] public int? Step { get; set; }

    private OnboardingState? _state;
    private ProStep _step = ProStep.Skills;
    private Task? _finishing;

    private IReadOnlyList<string> SettingUpSteps =>
    [
        T.Get(Copy.SettingUpListing),
        T.Get(Copy.SettingUpHome),
        T.Get(Copy.SettingUpChecklist),
    ];

    private ProfessionalDraft Draft => _state!.Professional;

    // Step 1 already shows some progress (endowed progress); the preview is 100%.
    private double Progress => (int)_step / (double)ProStep.Preview;

    protected override async Task OnInitializedAsync()
    {
        _state = await Store.LoadAsync();

        // The preview card shows the account's name and number.
        Draft.TakeAccount(await Identity.GetCurrentUserAsync());
    }

    protected override void OnParametersSet()
    {
        if (_state is null)
        {
            return;
        }

        _step = Draft.StepToShow(Step);
    }

    private Task PickSkillAsync(string categoryId)
    {
        Draft.PickTrade(categoryId);
        return AdvanceAsync();
    }

    private void ToggleService(string serviceId) => Draft.ToggleService(serviceId);

    private void ToggleCity(string cityId) => Draft.ToggleTown(cityId);

    private void ToggleAllTowns() => Draft.ToggleAllTowns();

    private Task PickExperienceAsync(string id)
    {
        Draft.ExperienceId = id;
        return AdvanceAsync();
    }

    private Task PickRateAsync(string id)
    {
        Draft.RateTierId = id;
        return AdvanceAsync();
    }

    private void PickGoal(string id) => Draft.IncomeGoalId = id;

    private async Task AdvanceAsync()
    {
        var state = _state!;
        var path = OnboardingSteps.PathOf(_step + 1);
        if (state.CompletedAt is null)
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    /// <summary>
    /// The pro tapped Done: the "setting up" loader shows (like the Seeker's) while the answers are saved and Pro
    /// home is loaded behind it, so the home page opens filled instead of after a blank pause.
    /// </summary>
    private void Finish() => _finishing = FinishAsync();

    private async Task FinishAsync()
    {
        var state = _state!;
        state.CompletedAt ??= DateTimeOffset.Now;
        state.ResumePath = null;
        await Store.SaveAsync(state);
        await Account.SaveProfileAsync(); // Their side and earnings goal.
        await Workspace.EnsureLoadedAsync();
    }

    /// <summary>The loader finished: wait for the saves if the network is slower than the animation, then open Pro home.</summary>
    private async Task GoHomeAsync()
    {
        await Busy.RunAsync(() => _finishing!);
        Nav.NavigateTo(ProRoutes.Home, replace: true);
    }

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(_step, finished: _state!.CompletedAt is not null));

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };
}
