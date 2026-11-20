using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's chat with one customer: who they are, the job they need (what, where, how soon,
/// budget, from the customer's answers), the messages, and a box to reply. New messages appear
/// as they arrive. Opening it clears the chat's unread badge. A full screen outside the tab shell
/// (<see cref="ChatScreen"/>), so it loads <see cref="ProWorkspace"/> and listens for notifications itself.
/// </summary>
public partial class ProChatPage : ComponentBase, IDisposable
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private IScrollManager Scroll { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<ProChatPage> Logger { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private IContactLauncher Phone { get; set; } = default!;
    [Inject] private AppVisibility Visibility { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private ChatAlerts Alerts { get; set; } = default!;

    [Parameter] public string ChatId { get; set; } = string.Empty;

    private Chat? _chat;

    /// <summary>The customer's number, only once they picked this worker's offer on a post (numbers stay private until then).</summary>
    private string? _number;

    /// <summary>The newest picked job with this customer (null when they never picked this worker's offer).</summary>
    private PickedJob? _job;
    private ChatHistory _history = ChatHistory.Empty;
    private IDisposable? _watch;
    private IDisposable? _alerts;

    /// <summary>Re-reads the picked job while it waits on the customer (marking done sends no message).</summary>
    private PickedJobWatch? _jobWatch;
    private IReadOnlyList<PickedJob> _jobs = [];
    private string? _message;
    private string? _error;
    private bool _busy;

    /// <summary>A message was just added at the bottom: scroll to it after the next render.</summary>
    private bool _scrollToEnd;

    /// <summary>The messages have loaded, so the list can open scrolled to the newest.</summary>
    private bool _loaded;

    protected override void OnInitialized()
    {
        // A push for another chat or a new request opens it; the Inbox and the Requests feed are read again.
        _alerts = Alerts.Attach(() => Task.WhenAll(Workspace.RefreshActivityAsync(), Board.LoadFeedAsync()), path => InvokeAsync(() => Nav.NavigateTo(path)));
        Workspace.Changed += ShowUnread;
    }

    /// <summary>A chat read here leaves the tray and the app icon's badge at once, as the tab layouts do.</summary>
    private void ShowUnread() => Alerts.ShowUnread(Workspace.UnreadChatIds);

    protected override async Task OnParametersSetAsync()
    {
        // Signed out (e.g. "Sign in with this number", then Back to this chat): sign in first, as the tab layouts do.
        if (await Gate.RedirectAsync("/") is { } signIn)
        {
            Nav.NavigateTo(signIn, replace: true);
            return;
        }

        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        _error = null;
        _loaded = false;
        _watch?.Dispose();
        _jobWatch?.Dispose();
        await Busy.RunAsync(LoadAsync);
        if (_chat is null)
        {
            Nav.NavigateTo(ProRoutes.Inbox, replace: true);
            return;
        }

        _loaded = true;
        _watch = Workspace.WatchChat(ChatId, _history.LastId, chat => InvokeAsync(() => ReloadAsync(chat)));
        WatchJobs(_jobs);
    }

    /// <summary>
    /// The picked jobs as the server now has them: the card shows the newest, and while it waits on the customer a fresh
    /// watch re-reads it from this state (so a read started before the worker's own Mark done never undoes it).
    /// </summary>
    private void WatchJobs(IReadOnlyList<PickedJob> jobs)
    {
        _jobs = jobs;
        _job = jobs.FirstOrDefault();
        _jobWatch?.Dispose();
        PickedJobWatch? watch = null;
        watch = _jobWatch = PickedJobWatch.Start(Board, Visibility, _chat!.SeekerId, jobs, changed => InvokeAsync(() =>
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

    private async Task LoadAsync()
    {
        _chat = Workspace.Chats.FirstOrDefault(c => c.Id == ChatId) ?? await Workspace.GetChatAsync(ChatId);
        if (_chat is null)
        {
            return;
        }

        var read = Workspace.MarkReadAsync(ChatId);
        var number = Board.ContactNumberAsync(_chat.SeekerId);
        var jobs = Board.PickedJobsAsync(_chat.SeekerId);
        _history = await Workspace.GetMessagesAsync(ChatId);
        _number = await number;
        _jobs = await jobs;
        _job = _jobs.FirstOrDefault();
        await read;
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
            if (!await Phone.CallAsync(_number!))
            {
                _error = T.Get(Copy.ContactFailed);
            }
        }
        finally
        {
            _busy = false;
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
            var reply = await Workspace.ReplyAsync(_chat!, _message!);
            _history = _history.With(reply);
            _scrollToEnd = true;
            _message = null;
        }
        catch (TextNotAllowedException)
        {
            // Nothing was sent; the message stays in the box to be rewritten.
            _error = T.Get(Copy.TextNotAllowed);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Replying in chat {ChatId} failed.", ChatId);
            _error = T.Get(Copy.SendFailed);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>The chat got a message: show it and mark it read. Our own reply, already shown, needs nothing.</summary>
    private async Task ReloadAsync(Chat chat)
    {
        Workspace.ChatChanged(chat);
        if (_history.Contains(chat.LastMessage.Id))
        {
            return;
        }

        _history = await Workspace.NewMessagesAsync(ChatId, _history);
        _scrollToEnd = true;
        await Workspace.MarkReadAsync(ChatId);

        // The pick's first message: the card appears without reopening (an empty answer is a failed read: keep the card).
        if (await Board.PickedJobsAsync(chat.SeekerId) is { Count: > 0 } jobs)
        {
            WatchJobs(jobs);
        }

        StateHasChanged();
    }

    /// <summary>The person scrolled to the top: the page of messages before the oldest one goes above.</summary>
    private async Task LoadOlderAsync()
    {
        var chatId = ChatId;
        var older = await Workspace.OlderMessagesAsync(chatId, _history);
        if (chatId == ChatId)
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
        Workspace.Changed -= ShowUnread;
        _alerts?.Dispose();
        _watch?.Dispose();
        _jobWatch?.Dispose();
    }
}
