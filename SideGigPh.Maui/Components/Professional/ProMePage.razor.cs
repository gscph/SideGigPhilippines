using System.Globalization;
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's Me tab (also the avatar in the top bar): their photo (add or change), status,
/// Edit listing and Publish, a link to their work photos, their card exactly as customers see
/// it, then the shared <see cref="Shared.MePage"/> parts (language, Start over).
/// </summary>
public partial class ProMePage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private PhotoAddResult? _photoResult;
    private bool _busy;

    private string Name => Workspace.Draft.DisplayName ?? string.Empty;

    private string PhotosCountLabel => T.Format(Copy.PhotosCount, Workspace.WorkPhotos.Count, ProWorkspace.MaxWorkPhotos);

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
