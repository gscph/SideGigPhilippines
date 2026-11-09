using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Turns a Professional's monthly goal and rate into something concrete
/// ("about 4 jobs a week at around ₱750"). Built only from their own answers, never invented.
/// </summary>
public partial class GoalPayoff : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string? RateTierId { get; set; }

    [Parameter] public string? IncomeGoalId { get; set; }

    private string? Message
    {
        get
        {
            if (ReferenceData.Find(ReferenceData.IncomeGoals, IncomeGoalId) is not { } goal)
            {
                return null;
            }

            if (ReferenceData.Find(ReferenceData.PriceTiers, RateTierId) is not { } tier)
            {
                return T.Get(Copy.GoalPayoffQuote);
            }

            var jobs = Money.JobsPerWeek(goal.Amount, tier.Amount);
            var price = Money.Peso(tier.Amount);
            return jobs == 1 ? T.Format(Copy.GoalPayoffOne, price) : T.Format(Copy.GoalPayoff, jobs, price);
        }
    }
}
