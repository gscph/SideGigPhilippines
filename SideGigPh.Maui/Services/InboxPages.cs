using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The Inbox in pages of <see cref="Paging.Inbox"/> chats, most recent message first (each workspace keeps the chats
/// it has loaded). A refresh re-reads the first page and keeps the older pages already loaded below it; scrolling to
/// the end adds the page after the oldest chat.
/// </summary>
public static class InboxPages
{
    /// <summary>A page this long may have more after it.</summary>
    public static bool IsFull(int count) => count >= Paging.Inbox;

    /// <summary>
    /// A fresh first page with the older chats already loaded that come after it. When the server's page wasn't full
    /// (<paramref name="pageWasFull"/>), it is the whole Inbox.
    /// </summary>
    public static IReadOnlyList<T> WithFirstPage<T>(IReadOnlyList<T> page, bool pageWasFull, IReadOnlyList<T> held, Func<T, Chat> chatOf)
    {
        if (!pageWasFull || page.Count == 0)
        {
            return page;
        }

        var last = chatOf(page[^1]);
        var ids = page.Select(item => chatOf(item).Id).ToHashSet();
        return [.. page, .. held.Where(item => !ids.Contains(chatOf(item).Id) && IsOlder(chatOf(item), last))];
    }

    /// <summary>The loaded chats with an older page added under them (a chat already shown isn't repeated).</summary>
    public static IReadOnlyList<T> WithOlder<T>(IReadOnlyList<T> held, IReadOnlyList<T> page, Func<T, Chat> chatOf)
    {
        var ids = held.Select(item => chatOf(item).Id).ToHashSet();
        return [.. held, .. page.Where(item => !ids.Contains(chatOf(item).Id))];
    }

    private static bool IsOlder(Chat chat, Chat than) =>
        chat.LastMessage.At < than.LastMessage.At
        || (chat.LastMessage.At == than.LastMessage.At && string.CompareOrdinal(chat.Id, than.Id) < 0);
}
