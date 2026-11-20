using System.Globalization;
using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's Me tab (also the avatar in the top bar): their photo (add or change), their profile level (red, green,
/// verified blue), Edit listing and Publish, the amber badge reminder, rows for work photos and badges
/// each with its mark (<see cref="ProfileProgress"/>), their card exactly as customers see
/// it, the switch to the customer side (<c>SwitchSideRow</c>),
/// then the shared <see cref="Shared.MePage"/> parts (language, Delete account).
/// </summary>
public partial class ProMePage : ComponentBase
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private ProBadges Badges { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;

    private PhotoAddResult? _photoResult;
    private bool _busy;

    /// <summary>Where the profile stands: the level under the name, the mark on each row, the badge reminder.</summary>
    private ProfileProgress Progress => ProfileProgress.Of(Workspace, Badges);

    /// <summary>Where the confirm-number step leads: the add-phone page, coming back here.</summary>
    private static string AddPhonePath => AuthRoutes.AddPhone(ProRoutes.Me, canSkip: true);

    private string Name => Workspace.Draft.DisplayName ?? string.Empty;

    private string PhotosCountLabel => T.Format(Copy.PhotosCount, Workspace.WorkPhotos.Count, ProWorkspace.MaxWorkPhotos);

    private string BadgesCountLabel => T.Format(Copy.BadgesCount, Badges.ApprovedKinds, BadgeCatalog.Kinds);

    private string? JoinedLabel => Workspace.JoinedAt is { } joined
        ? T.Format(Copy.ProfileSince, joined.ToString("MMM yyyy", CultureInfo.InvariantCulture))
        : null;

    protected override async Task OnInitializedAsync() => await Badges.LoadAsync();

    private async Task ChangePhotoAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

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
