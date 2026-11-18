using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// A Seeker's budget in two peso boxes ("From" / "Up to"). "Up to" can't be under "From"; any guide shown is only a
/// guide, never a rule (the household pays after the work, at the price both agree on). The page keeps Next off until
/// the range holds (<see cref="Pricing.IsValidBudget"/>).
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

    private bool MaxTooLow => Min is { } from && Max is { } to && to < from;
}
