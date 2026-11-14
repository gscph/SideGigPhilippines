using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The Seeker's own budget range, two peso boxes ("From" / "Up to"), shown when they pick "Other amount". "From" can't
/// go under the minimum wage and "Up to" can't be under "From" (<see cref="Pricing.IsValidBudget"/>); the page keeps
/// Next off until both hold.
/// </summary>
public partial class BudgetRangeFields : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public int? Min { get; set; }

    [Parameter] public EventCallback<int?> MinChanged { get; set; }

    [Parameter] public int? Max { get; set; }

    [Parameter] public EventCallback<int?> MaxChanged { get; set; }

    private string MinHelp => T.Format(Copy.BudgetFromHelp, Money.Peso(ReferenceData.MinimumWage));

    private bool MaxTooLow => Min is { } from && Max is { } to && to < from;
}
