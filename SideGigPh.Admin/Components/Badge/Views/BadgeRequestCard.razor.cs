using Microsoft.AspNetCore.Components;
using MudBlazor;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Badge.Views;

/// <summary>
/// One waiting request: profile photo beside the badge asked for, what the AI read (a clearance: its checklist, QR code
/// and the "valid until" approving needs, <see cref="ClearanceChecklist"/>), the proof photo only on a click
/// (each look is logged by the API), and Approve / Reject with a reason. Approve asks once more.
/// </summary>
public partial class BadgeRequestCard
{
    [Inject] public required BadgeService Badges { get; set; }
    [Inject] public required IDialogService Dialogs { get; set; }

    [Parameter, EditorRequired] public AdminVerificationDto Request { get; set; } = default!;
    [Parameter] public EventCallback OnDecided { get; set; }

    private string? _document;
    private string? _reason;
    private DateOnly? _validUntil;
    private string? _error;
    private bool _busy;

    private async Task ShowDocumentAsync()
    {
        if (_busy) return;
        _busy = true;
        _error = null;
        try
        {
            _document = await Badges.DocumentAsync(Request) ?? throw new AdminApiException("The proof photo is gone.");
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DecideAsync(bool approve)
    {
        if (_busy) return;
        _busy = true;
        _error = null;
        try
        {
            var sure = await Dialogs.ShowMessageBoxAsync(
                approve ? "Approve this badge?" : "Reject this badge?",
                approve
                    ? _validUntil is { } until && BadgeService.NeedsValidUntil(Request)
                        ? $"The badge shows on the worker's listing until {until:d MMM yyyy}. The proof photo is deleted."
                        : "The badge shows on the worker's listing. The proof photo is deleted."
                    : "The worker sees your reason. The proof photo is deleted.",
                yesText: approve ? "Approve" : "Reject", cancelText: "Cancel");
            if (sure != true)
            {
                return;
            }

            await Badges.DecideAsync(Request, approve, _reason, _validUntil);
            await OnDecided.InvokeAsync();
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
