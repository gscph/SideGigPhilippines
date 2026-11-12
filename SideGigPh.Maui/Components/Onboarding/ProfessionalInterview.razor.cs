using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Professional (Skilled Worker) interview: trades → jobs → area → towns → experience → rate → earnings goal →
/// preview. The answers build the listing card the pro sees at the end; its name and number come
/// from the account (<see cref="ListingMapper.TakeAccount"/>), so they are never asked here.
/// One URL per step; answers are saved whenever the user moves forward.
/// </summary>
public partial class ProfessionalInterview : ComponentBase
{
    private enum ProStep
    {
        Skills = 1,
        Services,
        Area,
        Towns,
        Experience,
        Rate,
        Goal,
        Preview,
    }

    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>
    /// Opens the first question still unanswered: a step past it is never shown, so asking for the last step
    /// lands there (or on the preview when everything is answered). Publish sends incomplete listings here.
    /// </summary>
    public static string FirstUnansweredPath => StepPath(ProStep.Preview);

    [Parameter] public int? Step { get; set; }

    private OnboardingState? _state;
    private ProStep _step = ProStep.Skills;

    private ProfessionalDraft Draft => _state!.Professional;

    // Step 1 already shows some progress (endowed progress); the preview is 100%.
    private double Progress => (int)_step / (double)ProStep.Preview;

    protected override async Task OnInitializedAsync()
    {
        _state = await Store.LoadAsync();

        // Drafts saved before the area step had towns but no area: take it from their towns.
        Draft.AreaId ??= ReferenceData.AreaOf(Draft.CityIds.FirstOrDefault());

        // The preview card shows the account's name and number.
        Draft.TakeAccount(await Identity.GetCurrentUserAsync());
    }

    protected override void OnParametersSet()
    {
        if (_state is null)
        {
            return;
        }

        var requested = (ProStep)Math.Clamp(Step ?? 1, 1, (int)ProStep.Preview);
        var firstUnanswered = FirstUnanswered();
        _step = requested > firstUnanswered ? firstUnanswered : requested;
    }

    private Task PickSkillAsync(string categoryId)
    {
        if (Draft.CategoryId != categoryId)
        {
            Draft.CategoryId = categoryId;
            Draft.ServiceIds.Clear(); // Jobs belonged to the previous trade.
        }

        return AdvanceAsync();
    }

    private void ToggleService(string serviceId) => Toggle(Draft.ServiceIds, serviceId);

    private Task PickAreaAsync(string areaId)
    {
        if (Draft.AreaId != areaId)
        {
            Draft.AreaId = areaId;
            Draft.CityIds.Clear(); // Towns belonged to the previous area.
        }

        return AdvanceAsync();
    }

    private void ToggleCity(string cityId) => Toggle(Draft.CityIds, cityId);

    /// <summary>"Select all" adds every town in the area; when all are already picked it removes them.</summary>
    private void ToggleWholeArea()
    {
        var towns = ReferenceData.CitiesByArea[Draft.AreaId!].Select(town => town.Id).ToList();
        if (towns.All(Draft.CityIds.Contains))
        {
            Draft.CityIds.RemoveAll(towns.Contains);
            return;
        }

        Draft.CityIds.AddRange(towns.Except(Draft.CityIds));
    }

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
        var path = StepPath(_step + 1);
        if (state.CompletedAt is null)
        {
            state.ResumePath = path;
        }

        await Store.SaveAsync(state);
        Nav.NavigateTo(path);
    }

    private async Task FinishAsync()
    {
        var state = _state!;
        state.CompletedAt ??= DateTimeOffset.Now;
        state.ResumePath = null;
        await Store.SaveAsync(state);
        await Account.SaveProfileAsync(); // Their side and earnings goal.
        Nav.NavigateTo("/professional", replace: true);
    }

    private void GoBack()
    {
        var target = _step != ProStep.Skills ? StepPath(_step - 1)
            : _state!.CompletedAt is not null ? "/professional"
            : "/start/role";
        Nav.NavigateTo(target);
    }

    private ProStep FirstUnanswered()
    {
        if (Draft.CategoryId is null) return ProStep.Skills;
        if (Draft.ServiceIds.Count == 0) return ProStep.Services;
        if (Draft.AreaId is null) return ProStep.Area;
        if (Draft.CityIds.Count == 0) return ProStep.Towns;
        if (Draft.ExperienceId is null) return ProStep.Experience;
        if (Draft.RateTierId is null) return ProStep.Rate;
        if (Draft.IncomeGoalId is null) return ProStep.Goal;
        return ProStep.Preview;
    }

    private static void Toggle(List<string> ids, string id)
    {
        if (!ids.Remove(id))
        {
            ids.Add(id);
        }
    }

    private static string StepPath(ProStep step) => $"/start/pro/{(int)step}";

    private static IReadOnlyCollection<string> Selected(string? id) =>
        id is null ? Array.Empty<string>() : new[] { id };
}
