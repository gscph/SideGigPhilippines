using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>
/// The Seeker's thread with one pro, where Contact lands. A message is already written from
/// their search (job, town, how soon), so contacting is one tap: Send text opens the SMS app
/// with it, Call opens the dialer. Either way it's logged here and on the pro's Inbox.
/// The pro replies by SMS or call, outside the app, and the page says so.
/// </summary>
public partial class ThreadPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private ProfessionalListing? _pro;
    private string? _message;
    private bool _loading = true;
    private bool _busy;
    private bool _contactFailed;

    private IReadOnlyList<Inquiry> Contacts => Workspace.ThreadWith(Id)?.Contacts ?? [];

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _contactFailed = false;
        _pro = Workspace.ThreadWith(Id)?.Pro ?? await Workspace.GetProAsync(Id);
        _message = _pro is null ? null : Workspace.DefaultMessage(_pro);
        _loading = false;
    }

    private Task TextAsync() => ContactAsync(pro => Workspace.TextAsync(pro, _message!.Trim()));

    private Task CallAsync() => ContactAsync(Workspace.CallAsync);

    private async Task ContactAsync(Func<ProfessionalListing, Task<bool>> contact)
    {
        _busy = true;
        try
        {
            _contactFailed = !await contact(_pro!);
        }
        finally
        {
            _busy = false;
        }
    }
}
