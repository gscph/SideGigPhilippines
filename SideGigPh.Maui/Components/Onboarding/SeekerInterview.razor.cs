using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Seeker interview: what job → which town (their one home town) → when →
/// budget → results. Each step has its own
/// URL so Android back walks the steps; every answer is saved as soon as it's tapped.
/// Once finished, tapping an answer chip on the results reopens one step and returns there.
/// The rules (which step, what an answer clears, where Next and Back go) are in <see cref="OnboardingSteps"/>.
/// </summary>
public partial class SeekerInterview : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public int? Step { get; set; }

    private OnboardingState? _state;
    private SeekerStep _step = SeekerStep.Category;

    private SeekerAnswers Answers => _state!.Seeker;

    private bool IsEditing => _state!.CompletedAt is not null;

    // Step 1 already shows some progress (endowed progress); the loader step is 100%.
    private double Progress => (int)_step / (double)SeekerStep.Finding;

    private IReadOnlyList<string> FindingSteps =>
    [
        T.Format(Copy.FindingService, Label(ReferenceData.FindService(Answers.ServiceId))),
        T.Format(Copy.FindingCity, Label(ReferenceData.Find(ReferenceData.Cities, Answers.CityId))),
        T.Get(Copy.FindingBudget),
    ];

    protected override async Task OnInitializedAsync()
    {
        _state = await Store.LoadAsync();
    }

    protected override void OnParametersSet()
    {
        if (_state is null)
        {
            return;
        }

        // Never show a step whose earlier answers are missing (e.g. a stale resume path).
        _step = Answers.StepToShow(Step);
    }

    private Task PickCategoryAsync(string id)
    {
        Answers.PickCategory(id);
        return AdvanceAsync();
    }

    private Task PickServiceAsync(string id)
    {
        Answers.ServiceId = id;
        return AdvanceAsync();
    }

    private Task PickCityAsync(string id)
    {
        Answers.PickTown(id);
        return AdvanceAsync();
    }

    private Task PickWhenAsync(string id)
    {
        Answers.UrgencyId = id;
        return AdvanceAsync();
    }

    private Task PickBudgetAsync(string id)
    {
        Answers.BudgetId = id;
        return AdvanceAsync();
    }

    private async Task AdvanceAsync()
    {
        var state = _state!;
        var path = Answers.NextPath(_step, IsEditing);
        if (!IsEditing)
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    private async Task FinishAsync()
    {
        var state = _state!;
        state.CompletedAt = DateTimeOffset.Now;
        state.ResumePath = null;
        await Store.SaveAsync(state);
        await Account.SaveProfileAsync();
        Nav.NavigateTo(SeekerRoutes.Search, replace: true);
    }

    private void GoBack() => Nav.NavigateTo(OnboardingSteps.BackPath(_step, IsEditing));

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
