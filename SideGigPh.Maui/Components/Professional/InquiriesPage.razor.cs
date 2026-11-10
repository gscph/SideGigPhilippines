using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Everyone who tapped Call or Text on the pro's listing, newest first. Opening the page
/// marks them seen (clearing the tab badge); they stay tagged "New" while the pro is here.
/// </summary>
public partial class InquiriesPage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private HashSet<string> _newIds = [];

    protected override async Task OnInitializedAsync()
    {
        await Workspace.RefreshActivityAsync();
        _newIds = Workspace.Inquiries.Where(Workspace.IsNew).Select(i => i.Id).ToHashSet();
        await Workspace.MarkInquiriesSeenAsync();
    }
}
