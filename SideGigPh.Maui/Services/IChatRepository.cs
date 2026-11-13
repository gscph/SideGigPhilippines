using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Chats between Seekers and Professionals: each side's Inbox, the messages in one chat, and
/// live updates while a chat is open.
/// </summary>
public interface IChatRepository
{
    /// <summary>The person's chats on that side, most recent message first.</summary>
    Task<IReadOnlyList<Chat>> GetInboxAsync(string personId, ChatSide side);

    /// <summary>One chat, or null if it doesn't exist (yet).</summary>
    Task<Chat?> GetAsync(string chatId);

    /// <summary>
    /// One page of the chat's messages, oldest first: the newest <c>Paging.Messages</c> before message
    /// <paramref name="beforeMessageId"/>, or the latest page without it. A full page means there may be older ones.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string chatId, string? beforeMessageId = null);

    /// <summary>
    /// Adds the message and makes it the chat's last message; the sender has read it. The first
    /// message creates the chat from <paramref name="chat"/> (names, job). Returns the chat as saved.
    /// </summary>
    Task<Chat> SendAsync(Chat chat, ChatMessage message);

    /// <summary>That side has now read everything in the chat (clears its unread badge).</summary>
    Task MarkReadAsync(string chatId, ChatSide side);

    /// <summary>
    /// Calls <paramref name="changed"/> with the chat whenever it gets a message after <paramref name="lastMessageId"/>
    /// (the last one the page already shows, or null), until disposed.
    /// </summary>
    IDisposable Watch(string chatId, string? lastMessageId, Action<Chat> changed);

    /// <summary>Deletes every chat this person is in, with its messages (Start over / delete account).</summary>
    Task DeleteForPersonAsync(string personId);
}
