using System.Globalization;
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Opened from the avatar in the top bar: the pro's photo (add or change), their status,
/// Edit listing and Publish, and their card exactly as customers see it.
/// </summary>
public partial class ProfilePage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private PhotoAddResult? _photoResult;
    private bool _busy;

    private string Name => Workspace.Draft.DisplayName ?? string.Empty;

    private string? JoinedLabel => Workspace.JoinedAt is { } joined
        ? T.Format(Copy.ProfileSince, joined.ToString("MMM yyyy", CultureInfo.InvariantCulture))
        : null;

    private async Task ChangePhotoAsync()
    {
        _busy = true;
        try
        {
            _photoResult = await Workspace.ChangeProfilePhotoAsync();
        }
        finally
        {
            _busy = false;
        }
    }
}
