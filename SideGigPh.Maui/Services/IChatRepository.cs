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

    /// <summary>The chat's messages, oldest first.</summary>
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string chatId);

    /// <summary>
    /// Adds the message and makes it the chat's last message; the sender has read it. The first
    /// message creates the chat from <paramref name="chat"/> (names, job). Returns the chat as saved.
    /// </summary>
    Task<Chat> SendAsync(Chat chat, ChatMessage message);

    /// <summary>That side has now read everything in the chat (clears its unread badge).</summary>
    Task MarkReadAsync(string chatId, ChatSide side);

    /// <summary>Calls <paramref name="changed"/> whenever the chat gets a message, until disposed.</summary>
    IDisposable Watch(string chatId, Action changed);

    /// <summary>Deletes every chat this person is in, with its messages (Start over / delete account).</summary>
    Task DeleteForPersonAsync(string personId);
}
