using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Pro home, Slack style: the setup checklist until the listing is published, then
/// collapsible sections for recent messages, latest reviews and the earnings goal.
/// The + button adds work photos.
/// </summary>
public partial class ProfessionalHome : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private PhotoAddResult? _photoResult;

    private string? GoalLabel => ReferenceData.Find(ReferenceData.IncomeGoals, Workspace.Draft.IncomeGoalId) is { } goal
        ? T.Get(goal.Label)
        : null;

    protected override Task OnInitializedAsync() => Workspace.RefreshActivityAsync();

    private async Task AddPhotosAsync()
    {
        _photoResult = await Workspace.AddWorkPhotosAsync();
        if (_photoResult is { Added: > 0, Duplicates: 0, Failed: false })
        {
            Nav.NavigateTo(ProRoutes.Photos);
        }
    }
}
