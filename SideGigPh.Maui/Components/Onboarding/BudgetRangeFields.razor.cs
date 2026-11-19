using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// A Seeker's budget in two peso boxes ("From", optional, / "Up to"). "Up to" can't be under "From"; any guide shown is
/// only a guide, never a rule (the household pays after the work, at the price both agree on). The page keeps Next off
/// until the range holds (<see cref="Pricing.IsValidBudget"/>), and "Up to" says why meanwhile.
/// </summary>
public partial class BudgetRangeFields : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public int? Min { get; set; }

    [Parameter] public EventCallback<int?> MinChanged { get; set; }

    [Parameter] public int? Max { get; set; }

    [Parameter] public EventCallback<int?> MaxChanged { get; set; }

    /// <summary>An optional line under "From", e.g. the job's usual price (<see cref="Pricing.GuideNote"/>).</summary>
    [Parameter] public string? Help { get; set; }

    /// <summary>
    /// Why Next is off, shown under "Up to" once a box has an amount (an open-ended guide tile fills only "From"), so a
    /// grey Next is never left unexplained; nothing while both boxes are empty.
    /// </summary>
    private BudgetProblem? Problem => Min is null && Max is null ? null : Pricing.BudgetProblemOf(Min, Max);

    private string? ProblemText => Problem is { } problem ? T.Get(Pricing.BudgetProblemNote(problem)) : null;
}
