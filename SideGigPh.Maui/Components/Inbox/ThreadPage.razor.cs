using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
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
    [Inject] private IScrollManager Scroll { get; set; } = default!;
    [Inject] private ILogger<ThreadPage> Logger { get; set; } = default!;
    [Inject] private ChatAlerts Alerts { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppVisibility Visibility { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private ProfessionalListing? _pro;

    /// <summary>The pro's number, only once the Seeker picked their offer on a post (numbers stay private until then).</summary>
    private string? _number;

    /// <summary>The newest picked job with this pro (null when the Seeker never picked their offer).</summary>
    private PickedJob? _job;
    private ChatHistory _history = ChatHistory.Empty;
    private IDisposable? _watch;

    /// <summary>Re-reads the picked job while it waits on the worker (marking done sends no message).</summary>
    private PickedJobWatch? _jobWatch;
    private string? _message;
    private string? _error;
    private bool _loading = true;
    private bool _busy;

    /// <summary>A message was just added at the bottom: scroll to it after the next render.</summary>
    private bool _scrollToEnd;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _error = null;
        _jobWatch?.Dispose();
        _watch?.Dispose();
        await Busy.RunAsync(LoadAsync);
        _loading = false;
    }

    private async Task LoadAsync()
    {
        _pro = Workspace.ThreadWith(Id)?.Pro ?? await Workspace.GetProAsync(Id);
        _number = null;
        _job = null;
        if (_pro is not null)
        {
            var read = Workspace.MarkReadAsync(Id);
            var number = Board.ContactNumberAsync(Id);
            var jobs = Board.PickedJobsAsync(Id);
            _history = await Workspace.GetMessagesAsync(Id);
            _message = _history.Count == 0 ? Workspace.DefaultMessage(_pro) : null;
            _watch = Workspace.WatchChat(Id, _history.LastId, chat => InvokeAsync(() => ReloadAsync(chat)));
            _number = await number;
            var picked = await jobs;
            JobsChanged(picked);
            _jobWatch = PickedJobWatch.Start(Board, Visibility, Id, picked, changed => InvokeAsync(() =>
            {
                JobsChanged(changed);
                StateHasChanged();
            }));
            await read;
        }
    }

    private async Task SendAsync()
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _error = null;
        try
        {
            Show(await Workspace.SendAsync(_pro!, _message!));
            _message = null;

            // Now there's a reply to wait for: allow the red dot on the app icon (asked once; the system remembers).
            _ = Alerts.AskAsync();
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
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _error = null;
        try
        {
            if (await Workspace.CallAsync(_pro!, _number!) is { } call)
            {
                Show(call);
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

    /// <summary>
    /// The picked jobs as the server now has them: the card shows the newest, and any completed one (an earlier job
    /// too) lets the Seeker review at once.
    /// </summary>
    private void JobsChanged(IReadOnlyList<PickedJob> jobs)
    {
        if (jobs.Any(job => job.IsCompleted))
        {
            Workspace.CompletedJobWith(Id);
        }

        JobChanged(jobs.FirstOrDefault());
    }

    /// <summary>The picked job as the server now has it; a completed one lets the Seeker review at once.</summary>
    private void JobChanged(PickedJob? job)
    {
        _job = job;
        if (job is { IsCompleted: true })
        {
            Workspace.CompletedJobWith(Id);
        }
    }

    /// <summary>The message the server just saved, added under the others (no need to read them all again).</summary>
    private void Show(ChatMessage message)
    {
        _history = _history.With(message);
        _scrollToEnd = true;
    }

    /// <summary>The chat got a message: show it and mark it read. Our own, already shown, needs nothing.</summary>
    private async Task ReloadAsync(Chat chat)
    {
        Workspace.ChatChanged(chat);
        if (_history.Contains(chat.LastMessage.Id))
        {
            return;
        }

        _history = await Workspace.NewMessagesAsync(Id, _history);
        _scrollToEnd = true;
        await Workspace.MarkReadAsync(Id);
        StateHasChanged();
    }

    /// <summary>The person scrolled to the top: the page of messages before the oldest one goes above.</summary>
    private async Task LoadOlderAsync()
    {
        var chatId = Id;
        var older = await Workspace.OlderMessagesAsync(chatId, _history);
        if (chatId == Id)
        {
            _history = older;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_scrollToEnd)
        {
            _scrollToEnd = false;
            await Scroll.ScrollToBottomAsync("html", ScrollBehavior.Smooth);
        }
    }

    public void Dispose()
    {
        _watch?.Dispose();
        _jobWatch?.Dispose();
    }
}
