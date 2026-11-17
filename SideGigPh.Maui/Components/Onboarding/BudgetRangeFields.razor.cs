using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// A price range in two peso boxes ("From" / "Up to"): the Seeker's budget, the Skilled Worker's rate and a job post's
/// budget. "Up to" can't be under "From"; the suggested minimum price shows under "From" as a guide only, never a rule
/// (the household pays after the work, at the price both agree on). The page keeps Next off until the range holds
/// (<see cref="Pricing.IsValidBudget"/>).
/// </summary>
public partial class BudgetRangeFields : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public int? Min { get; set; }

    [Parameter] public EventCallback<int?> MinChanged { get; set; }

    [Parameter] public int? Max { get; set; }

    [Parameter] public EventCallback<int?> MaxChanged { get; set; }

    /// <summary>The line under "From"; the suggested minimum price when not set.</summary>
    [Parameter] public string? Help { get; set; }

    private string GuideHelp => T.Format(Copy.BudgetFromHelp, Money.Peso(ReferenceData.MinimumWage));

    private bool MaxTooLow => Min is { } from && Max is { } to && to < from;
}
