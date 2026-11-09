using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Seeker interview: what job → which town → when → budget → results. Each step has its own
/// URL so Android back walks the steps; every answer is saved as soon as it's tapped.
/// Once finished, tapping an answer chip on the results reopens one step and returns there.
/// </summary>
public partial class SeekerInterview : ComponentBase
{
    private enum SeekerStep
    {
        Category = 1,
        Service,
        City,
        When,
        Budget,
        Finding,
    }

    [Inject] private IOnboardingStore Store { get; set; } = default!;
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

    protected override async Task OnInitializedAsync() => _state = await Store.LoadAsync();

    protected override void OnParametersSet()
    {
        if (_state is null)
        {
            return;
        }

        // Never show a step whose earlier answers are missing (e.g. a stale resume path).
        var requested = (SeekerStep)Math.Clamp(Step ?? 1, 1, (int)SeekerStep.Finding);
        var firstUnanswered = FirstUnanswered();
        _step = requested > firstUnanswered ? firstUnanswered : requested;
    }

    private Task PickCategoryAsync(string id)
    {
        if (Answers.CategoryId != id)
        {
            Answers.CategoryId = id;
            Answers.ServiceId = null;
        }

        return AdvanceAsync();
    }

    private Task PickServiceAsync(string id)
    {
        Answers.ServiceId = id;
        return AdvanceAsync();
    }

    private Task PickCityAsync(string id)
    {
        Answers.CityId = id;
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
        string path;
        if (IsEditing)
        {
            var next = FirstUnanswered();
            path = next == SeekerStep.Finding ? "/search" : StepPath(next);
        }
        else
        {
            path = StepPath(_step + 1);
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    private async Task FinishAsync()
    {
        // Future auth gate goes here: after the interview, before the first benefit (results).
        var state = _state!;
        state.CompletedAt = DateTimeOffset.Now;
        state.ResumePath = null;
        await Store.SaveAsync(state);
        Nav.NavigateTo("/search", replace: true);
    }

    private void GoBack()
    {
        var target = IsEditing ? "/search"
            : _step == SeekerStep.Category ? "/start/role"
            : StepPath(_step - 1);
        Nav.NavigateTo(target);
    }

    private SeekerStep FirstUnanswered()
    {
        if (Answers.CategoryId is null) return SeekerStep.Category;
        if (Answers.ServiceId is null) return SeekerStep.Service;
        if (Answers.CityId is null) return SeekerStep.City;
        if (Answers.UrgencyId is null) return SeekerStep.When;
        if (Answers.BudgetId is null) return SeekerStep.Budget;
        return SeekerStep.Finding;
    }

    private static string StepPath(SeekerStep step) => $"/start/seeker/{(int)step}";

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
