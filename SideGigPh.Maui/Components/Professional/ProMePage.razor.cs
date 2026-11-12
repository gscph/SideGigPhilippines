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
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;

    private PhotoAddResult? _photoResult;
    private bool _busy;

    private string Name => Workspace.Draft.DisplayName ?? string.Empty;

    private string PhotosCountLabel => T.Format(Copy.PhotosCount, Workspace.WorkPhotos.Count, ProWorkspace.MaxWorkPhotos);

    private string ReferencesCountLabel => T.Format(Copy.ReferencesCount, Workspace.Draft.References.Count, ProfessionalDraft.MaxReferences);

    private string? JoinedLabel => Workspace.JoinedAt is { } joined
        ? T.Format(Copy.ProfileSince, joined.ToString("MMM yyyy", CultureInfo.InvariantCulture))
        : null;

    private async Task ChangePhotoAsync()
    {
        _busy = true;
        try
        {
            _photoResult = await Busy.RunAsync(async () =>
            {
                var result = await Workspace.ChangeProfilePhotoAsync();
                if (result.Added > 0)
                {
                    await Account.SaveProfileAsync(); // the account's one profile photo, kept with the profile
                }

                return result;
            });
        }
        finally
        {
            _busy = false;
        }
    }
}
