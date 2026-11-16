using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Pro home, Slack style: the setup checklist until the listing is published, then
/// collapsible sections for recent messages and latest reviews.
/// The + button adds work photos.
/// </summary>
public partial class ProfessionalHome : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private PhotoAddResult? _photoResult;

    /// <summary>Where the confirm-number step leads: the add-phone page, coming back here.</summary>
    private static string AddPhonePath => AuthRoutes.AddPhone(ProRoutes.Home, canSkip: true);

    protected override Task OnInitializedAsync() => Workspace.RefreshIfStaleAsync();

    private async Task AddPhotosAsync()
    {
        var from = Nav.Uri;
        _photoResult = await Busy.RunAsync(Workspace.AddWorkPhotosAsync);

        // Unless those photos finished the checklist and the layout already opened Publish.
        if (_photoResult is { Added: > 0, Duplicates: 0, Failed: false } && Nav.Uri == from)
        {
            Nav.NavigateTo(ProRoutes.Photos);
        }
    }
}
