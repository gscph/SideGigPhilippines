using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Sending the proof for one badge: what to photograph, for a past project which company (a well-known developer or
/// any other, typed), then the consent box, unticked. Only with it ticked does the photo picker open and the photo
/// leave the phone. The phone's own photo permission is not consent: the tick is.
/// </summary>
public partial class BadgeProofPage : ComponentBase
{
    [Inject] private ProBadges Badges { get; set; } = default!;
    [Inject] private IPhotoPicker Picker { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The badge's kind from the URL: <c>id</c>, <c>tesda</c> or <c>project</c>.</summary>
    [Parameter] public string Kind { get; set; } = string.Empty;

    private string? _developerId;
    private string? _companyName;
    private string? _projectName;
    private bool _consent;
    private bool _busy;
    private BadgeSubmitResult? _result;
    private LocalizedText? _problem;
    private string? _shownKind;

    private BadgeKind BadgeKind => Enum.TryParse<BadgeKind>(Kind, ignoreCase: true, out var kind) ? kind : BadgeKind.Id;

    private Badge Badge => new(BadgeKind, _developerId, _companyName, _projectName);

    private bool CanSend => _consent && BadgeCatalog.HasCompany(Badge);

    private LocalizedText Title => BadgeKind switch
    {
        BadgeKind.Tesda => Copy.BadgeTesdaTitle,
        BadgeKind.Project => Copy.BadgeProjectTitle,
        _ => Copy.BadgeIdTitle,
    };

    private LocalizedText What => BadgeKind switch
    {
        BadgeKind.Tesda => Copy.BadgeTesdaWhat,
        BadgeKind.Project => Copy.BadgeProjectWhat,
        _ => Copy.BadgeIdWhat,
    };

    protected override async Task OnInitializedAsync() => await Badges.LoadAsync();

    protected override void OnParametersSet()
    {
        // The same page instance serves another kind when only the URL's kind changes: nothing ticked carries over.
        if (_shownKind == Kind)
        {
            return;
        }

        _shownKind = Kind;
        _consent = false;
        _result = null;
        _problem = null;
    }

    private async Task SendAsync()
    {
        if (_busy || !CanSend)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        try
        {
            // The picker first, outside the loading screen: the splash covers only the upload.
            var pick = await Picker.PickAsync(1);
            _result = pick.Status == PhotoPickStatus.Picked
                ? await Busy.RunAsync(() => Badges.SubmitAsync(Badge, _consent, pick))
                : await Badges.SubmitAsync(Badge, _consent, pick);
            _problem = _result switch
            {
                BadgeSubmitResult.Sent or BadgeSubmitResult.NoPhoto => null,
                BadgeSubmitResult.AlreadySent => Copy.BadgeAlreadySent,
                BadgeSubmitResult.NoCompany => Copy.BadgeNoCompany,
                BadgeSubmitResult.PhotoDenied => Copy.BadgePhotoDenied,
                _ => Copy.BadgeFailed,
            };
            if (_result == BadgeSubmitResult.Sent)
            {
                Navigation.NavigateTo(ProRoutes.Badges);
            }
        }
        finally
        {
            _busy = false;
        }
    }
}
