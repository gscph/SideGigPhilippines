using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Sending the proof for one badge: what to photograph, for a past project which company (a well-known developer or
/// any other, typed), then the consent box, unticked. Only with it ticked does the photo picker open and the photo
/// leave the phone. The phone's own photo permission is not consent: the tick is. An ID or a clearance is
/// photographed now with the back camera (never the gallery), with a guide first; after sending, the server's photo
/// check may ask "take it again?" (<see cref="PhotoRetakePrompt"/>), only advice.
/// </summary>
public partial class BadgeProofPage : ComponentBase
{
    [Inject] private ProBadges Badges { get; set; } = default!;
    [Inject] private IPhotoPicker Picker { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The badge's kind from the URL: <c>id</c>, <c>clearance</c>, <c>tesda</c> or <c>project</c>.</summary>
    [Parameter] public string Kind { get; set; } = string.Empty;

    private string? _developerId;
    private string? _companyName;
    private string? _projectName;
    private bool _consent;
    private bool _busy;
    private BadgeSubmitResult? _result;
    private LocalizedText? _problem;
    private string? _shownKind;
    private IReadOnlyList<ProofProblem>? _problems;

    private BadgeKind BadgeKind => Enum.TryParse<BadgeKind>(Kind, ignoreCase: true, out var kind) ? kind : BadgeKind.Id;

    private Badge Badge => new(BadgeKind, _developerId, _companyName, _projectName);

    private bool CanSend => _consent && BadgeCatalog.HasCompany(Badge);

    private bool UsesCamera => ProBadges.UsesCamera(BadgeKind);

    private LocalizedText Title => BadgeKind switch
    {
        BadgeKind.Clearance => Copy.BadgeClearanceTitle,
        BadgeKind.Tesda => Copy.BadgeTesdaTitle,
        BadgeKind.Project => Copy.BadgeProjectTitle,
        _ => Copy.BadgeIdTitle,
    };

    private LocalizedText What => BadgeKind switch
    {
        BadgeKind.Clearance => Copy.BadgeClearanceWhat,
        BadgeKind.Tesda => Copy.BadgeTesdaWhat,
        BadgeKind.Project => Copy.BadgeProjectWhat,
        _ => Copy.BadgeIdWhat,
    };

    /// <summary>The consent the worker ticks: an ID is read by AI, a clearance can show a record, anything else is a plain document.</summary>
    private LocalizedText Consent => BadgeKind switch
    {
        BadgeKind.Id => Copy.BadgeConsentId,
        BadgeKind.Clearance => Copy.BadgeConsentClearance,
        _ => Copy.BadgeConsentDocument,
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
        _problems = null;
    }

    private Task SendAsync() => SendAsync(retake: false);

    private Task RetakeAsync() => SendAsync(retake: true);

    /// <summary>The worker keeps the photo the check had doubts about: it is already with the checker.</summary>
    private void Keep() => Navigation.NavigateTo(ProRoutes.Badges);

    private async Task SendAsync(bool retake)
    {
        if (_busy || !CanSend)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        try
        {
            // The camera or picker first, outside the loading screen: the splash covers only the upload.
            var pick = UsesCamera ? await Picker.TakeDocumentPhotoAsync() : await Picker.PickAsync(1);
            _result = pick.Status == PhotoPickStatus.Picked
                ? await Busy.RunAsync(() => Badges.SubmitAsync(Badge, _consent, pick, retake))
                : await Badges.SubmitAsync(Badge, _consent, pick, retake);
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
                _problems = Badges.ProblemsOf(Badge) is { Count: > 0 } found ? found : null;
                if (_problems is null)
                {
                    Navigation.NavigateTo(ProRoutes.Badges);
                }
            }
        }
        finally
        {
            _busy = false;
        }
    }
}
