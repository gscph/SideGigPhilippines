using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// A one-line light red strip under the top bar while the listing isn't published: customers can't see the worker
/// yet. It names the next thing publishing needs (<see cref="ProfileProgress.NextStep"/>: own photo, the confirmed
/// number, then Publish) and a tap opens that page. Once live it shows only when the server refused the own photo
/// (<see cref="ProfileNextStep.NewSelfie"/>): the listing stays up, but edits wait for a new selfie. Always one line
/// (long text is cut with "…").
/// </summary>
public partial class NotLiveBanner : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter, EditorRequired] public ProfileNextStep Step { get; set; }

    private LocalizedText Text => Step switch
    {
        ProfileNextStep.Photo => Copy.NotLiveBannerPhoto,
        ProfileNextStep.Phone => Copy.NotLiveBannerNumber,
        ProfileNextStep.NewSelfie => Copy.LiveBannerNewSelfie,
        _ => Copy.NotLiveBannerPublish,
    };

    private string Href => Step switch
    {
        ProfileNextStep.Photo or ProfileNextStep.NewSelfie => ProRoutes.Me, // the photo button sits on the Me page's profile card
        ProfileNextStep.Phone => AuthRoutes.AddPhone(ProRoutes.Home, canSkip: true),
        _ => ProRoutes.Publish,
    };

    private void GoToStep() => Nav.NavigateTo(Href);
}
