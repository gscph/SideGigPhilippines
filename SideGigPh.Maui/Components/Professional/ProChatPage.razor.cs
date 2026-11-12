using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's chat with one customer: who they are, the job they need (what, where, how soon,
/// budget, from the customer's answers), the messages, and a box to reply. New messages appear
/// as they arrive. Opening it clears the chat's unread badge.
/// </summary>
public partial class ProChatPage : ComponentBase, IDisposable
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<ProChatPage> Logger { get; set; } = default!;

    [Parameter] public string ChatId { get; set; } = string.Empty;

    private Chat? _chat;
    private IReadOnlyList<ChatMessage> _messages = [];
    private IDisposable? _watch;
    private string? _message;
    private string? _error;
    private bool _busy;

    protected override async Task OnParametersSetAsync()
    {
        _error = null;
        _watch?.Dispose();
        _chat = Workspace.Chats.FirstOrDefault(c => c.Id == ChatId) ?? await Workspace.GetChatAsync(ChatId);
        if (_chat is null)
        {
            Nav.NavigateTo(ProRoutes.Inbox, replace: true);
            return;
        }

        var read = Workspace.MarkReadAsync(ChatId);
        _messages = await Workspace.GetMessagesAsync(ChatId);
        _watch = Workspace.WatchChat(ChatId, _messages.LastOrDefault()?.Id, chat => InvokeAsync(() => ReloadAsync(chat)));
        await read;
    }

    private async Task SendAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            var reply = await Workspace.ReplyAsync(_chat!, _message!);
            _messages = [.. _messages.Where(m => m.Id != reply.Id), reply];
            _message = null;
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
        if (_messages.Any(m => m.Id == chat.LastMessage.Id))
        {
            return;
        }

        _messages = await Workspace.GetMessagesAsync(ChatId);
        await Workspace.MarkReadAsync(ChatId);
        StateHasChanged();
    }

    public void Dispose() => _watch?.Dispose();
}
