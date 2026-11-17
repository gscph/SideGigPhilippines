using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Admin;

/// <summary>
/// One badge request for the operator: the worker's profile photo and name, the badge, what the AI read from an ID,
/// the proof photo (loaded only on a tap, since every look is logged) and Approve / Reject (a rejection needs a reason
/// the worker will see). Raises <see cref="OnDecided"/> once the server took the decision.
/// </summary>
public partial class PendingBadgeCard : ComponentBase
{
    [Inject] private BadgeReview Review { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public PendingBadge Request { get; set; } = default!;

    [Parameter] public EventCallback OnDecided { get; set; }

    private string? _document;
    private string? _reason;
    private bool _busy;
    private LocalizedText? _error;

    private LocalizedText ScanNote => Request.Scan.Result switch
    {
        BadgeScanResult.Unreadable => Copy.AdminScanUnreadable,
        BadgeScanResult.Failed => Copy.AdminScanFailed,
        _ => Copy.AdminScanNone,
    };

    private async Task ShowDocumentAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _error = null;
        try
        {
            _document = await Review.DocumentAsync(Request);
            _error = _document is null ? Copy.AdminProofFailed : null;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DecideAsync(bool approve)
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _error = null;
        try
        {
            if (await Review.DecideAsync(Request, approve, _reason))
            {
                await OnDecided.InvokeAsync();
            }
            else
            {
                _error = Copy.AdminDecideFailed;
            }
        }
        finally
        {
            _busy = false;
        }
    }
}
