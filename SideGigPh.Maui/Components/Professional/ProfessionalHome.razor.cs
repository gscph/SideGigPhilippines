using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Home for a Professional who finished the interview: their saved card, their earnings
/// goal, and a way to edit. Going live (auth + publish) is not built yet, and the page says so.
/// </summary>
public partial class ProfessionalHome : ComponentBase
{
    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private ProfessionalDraft? _draft;

    private string? GoalLabel => ReferenceData.Find(ReferenceData.IncomeGoals, _draft?.IncomeGoalId) is { } goal
        ? T.Get(goal.Label)
        : null;

    protected override async Task OnInitializedAsync()
    {
        var state = await Store.LoadAsync();
        if (state.Role != UserRole.Professional || state.CompletedAt is null)
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        _draft = state.Professional;
    }

    private void Edit() => Nav.NavigateTo("/start/pro/1");
}
