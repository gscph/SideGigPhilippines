using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IChatRepository"/> on SideGigPh.Api (<c>chats</c>). The server saves a message and the chat's new last
/// message in one transaction, and only lets the two people in a chat read or write it. Live updates while a chat is
/// open are a light poll of the chat (<see cref="WatchInterval"/>); push notifications come later.
/// </summary>
public sealed class ApiChatRepository(ApiClient api, ILogger<ApiChatRepository> logger) : IChatRepository
{
    /// <summary>How often an open chat checks for a new message.</summary>
    public static readonly TimeSpan WatchInterval = TimeSpan.FromSeconds(5);

    public Task<IReadOnlyList<Chat>> GetInboxAsync(string personId, ChatSide side) => api.ReadAsync<IReadOnlyList<Chat>>(async () =>
        (await api.GetAsync($"chats?side={side.ToWire()}", ApiJson.Default.ListChatDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], $"the {side} Inbox of {personId}");

    public Task<Chat?> GetAsync(string chatId) => api.ReadAsync(async () =>
        (await api.GetAsync($"chats/{ApiClient.Escape(chatId)}", ApiJson.Default.ChatDto))?.ToModel(),
        null, $"chat {chatId}");

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string chatId) => api.ReadAsync<IReadOnlyList<ChatMessage>>(async () =>
        (await api.GetAsync($"chats/{ApiClient.Escape(chatId)}/messages", ApiJson.Default.ListMessageDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], $"the messages of chat {chatId}");

    public async Task<Chat> SendAsync(Chat chat, ChatMessage message)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, $"chats/{ApiClient.Escape(chat.Id)}/messages",
            chat.ToSendRequest(message), ApiJson.Default.SendMessageRequest);
        api.EnsureWritten(response, $"send a message in chat {chat.Id}");
        var saved = await response.Content.ReadFromJsonAsync(ApiJson.Default.ChatDto)
            ?? throw new HttpRequestException("The API sent an empty answer to a message.");
        return saved.ToModel();
    }

    public async Task MarkReadAsync(string chatId, ChatSide side)
    {
        try
        {
            using var response = await api.SendAsync(HttpMethod.Post, $"chats/{ApiClient.Escape(chatId)}/read?side={side.ToWire()}");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("API: couldn't mark chat {ChatId} read: {Status}.", chatId, (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Harmless: the next time the chat opens marks it read.
            logger.LogWarning(ex, "API: couldn't mark chat {ChatId} read.", chatId);
        }
    }

    /// <summary>Calls <paramref name="changed"/> when the chat's last message changes, checking every <see cref="WatchInterval"/>.</summary>
    public IDisposable Watch(string chatId, Action changed) => new ChatPoll(this, chatId, changed);

    public async Task DeleteForPersonAsync(string personId)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, "chats/mine");
        api.EnsureWritten(response, "delete my chats");
    }

    /// <summary>Polls one chat until disposed. A failed check (offline) is skipped; the next one tries again.</summary>
    private sealed class ChatPoll : IDisposable
    {
        private readonly CancellationTokenSource _stop = new();

        public ChatPoll(ApiChatRepository chats, string chatId, Action changed) => _ = RunAsync(chats, chatId, changed, _stop.Token);

        public void Dispose() => _stop.Cancel();

        private static async Task RunAsync(ApiChatRepository chats, string chatId, Action changed, CancellationToken stop)
        {
            try
            {
                // The page has just read the chat, so only a later message counts.
                var last = (await chats.GetAsync(chatId))?.LastMessage.Id;
                using var timer = new PeriodicTimer(WatchInterval);
                while (await timer.WaitForNextTickAsync(stop))
                {
                    var now = (await chats.GetAsync(chatId))?.LastMessage.Id;
                    if (now is not null && now != last)
                    {
                        last = now;
                        changed();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Disposed: the chat page closed.
            }
        }
    }
}
