using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Pro home, Slack style: the setup checklist until the listing is published (then the amber badge reminder while a
/// badge can still be asked for), then
/// collapsible sections for recent messages and latest reviews.
/// The + button adds work photos.
/// </summary>
public partial class ProfessionalHome : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ProBadges Badges { get; set; } = default!;

    private PhotoAddResult? _photoResult;

    /// <summary>Where the confirm-number step leads: the add-phone page, coming back here.</summary>
    private static string AddPhonePath => AuthRoutes.AddPhone(ProRoutes.Home, canSkip: true);

    /// <summary>Once the listing is live, the amber badge reminder takes the setup card's place while a badge can still be asked for.</summary>
    private ProfileProgress Progress => ProfileProgress.Of(Workspace, Badges);

    protected override async Task OnInitializedAsync()
    {
        await Workspace.RefreshIfStaleAsync();
        if (Workspace.IsPublished)
        {
            await Badges.RefreshIfStaleAsync();
        }
    }

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
