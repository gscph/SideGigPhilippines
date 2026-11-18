using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Inbox;

/// <summary>
/// The Seeker's chat with one skilled worker, where Contact lands: full screen outside the tabs, the message box pinned
/// at the bottom (an empty chat too, so the ready first message sits above the keyboard, not mid-screen). The first message is already
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
    [Inject] private NavigationManager Nav { get; set; } = default!;

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
    private IReadOnlyList<PickedJob> _jobs = [];
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
        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

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
            WatchJobs(await jobs);
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
    /// The picked jobs as the server now has them: the card shows the newest, any completed one (an earlier job too)
    /// lets the Seeker review at once, and while the newest waits on the worker a fresh watch re-reads it from this
    /// state (so a read started before the Seeker's own Mark done never undoes it).
    /// </summary>
    private void WatchJobs(IReadOnlyList<PickedJob> jobs)
    {
        _jobs = jobs;
        _job = jobs.FirstOrDefault();
        if (jobs.Any(job => job.IsCompleted))
        {
            Workspace.CompletedJobWith(Id);
        }

        _jobWatch?.Dispose();
        PickedJobWatch? watch = null;
        watch = _jobWatch = PickedJobWatch.Start(Board, Visibility, Id, jobs, changed => InvokeAsync(() =>
        {
            if (!ReferenceEquals(watch, _jobWatch))
            {
                return; // a newer state replaced this watch: its last read is stale
            }

            WatchJobs(changed);
            StateHasChanged();
        }));
    }

    /// <summary>The card's own change (Mark done, a rating): the newest job as the server answered.</summary>
    private void JobChanged(PickedJob job) => WatchJobs([job, .. _jobs.Skip(1)]);

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

        // A new message may come with a pick made meanwhile (an empty answer is a failed read: keep the card).
        if (await Board.PickedJobsAsync(Id) is { Count: > 0 } jobs)
        {
            WatchJobs(jobs);
        }

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
