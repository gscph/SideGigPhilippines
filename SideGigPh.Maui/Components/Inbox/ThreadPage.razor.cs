using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>
/// The Seeker's chat with one skilled worker, where Contact lands. The first message is already
/// written from their search (job, town, how soon), so starting is one tap on Send. Call opens
/// the dialer and is logged in the chat. The worker's replies appear here as they arrive.
/// </summary>
public partial class ThreadPage : ComponentBase, IDisposable
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private ILogger<ThreadPage> Logger { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private ProfessionalListing? _pro;
    private IReadOnlyList<ChatMessage> _messages = [];
    private IDisposable? _watch;
    private string? _message;
    private string? _error;
    private bool _loading = true;
    private bool _busy;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _error = null;
        _watch?.Dispose();
        _pro = Workspace.ThreadWith(Id)?.Pro ?? await Workspace.GetProAsync(Id);
        if (_pro is not null)
        {
            _messages = await Workspace.GetMessagesAsync(Id);
            _message = _messages.Count == 0 ? Workspace.DefaultMessage(_pro) : null;
            _watch = Workspace.WatchChat(Id, () => InvokeAsync(ReloadAsync));
            await Workspace.MarkReadAsync(Id);
        }

        _loading = false;
    }

    private async Task SendAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            await Workspace.SendAsync(_pro!, _message!);
            _message = null;
            _messages = await Workspace.GetMessagesAsync(Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Sending a chat message to {ProfessionalId} failed.", Id);
            _error = T.Get(Copy.SendFailed);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task CallAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            if (await Workspace.CallAsync(_pro!))
            {
                _messages = await Workspace.GetMessagesAsync(Id);
            }
            else
            {
                _error = T.Get(Copy.ContactFailed);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>A new message arrived (theirs or ours): show it and mark it read.</summary>
    private async Task ReloadAsync()
    {
        _messages = await Workspace.GetMessagesAsync(Id);
        await Workspace.RefreshActivityAsync();
        await Workspace.MarkReadAsync(Id);
        StateHasChanged();
    }

    public void Dispose() => _watch?.Dispose();
}
