using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The messages a chat page has loaded, oldest first. A chat opens on its newest page (<see cref="Paging.Messages"/>);
/// scrolling to the top adds the page before it (<see cref="WithOlder"/>), and new messages join at the bottom
/// (<see cref="WithLatest"/>, <see cref="With"/>). A message is never shown twice.
/// </summary>
public sealed record ChatHistory(IReadOnlyList<ChatMessage> Messages, bool HasOlder)
{
    public static ChatHistory Empty { get; } = new([], false);

    /// <summary>The chat's newest page, as it opens. A full page means there may be older messages.</summary>
    public static ChatHistory FromLatest(IReadOnlyList<ChatMessage> page) => new(page, IsFull(page));

    public int Count => Messages.Count;

    /// <summary>The oldest message loaded: the next older page comes before it.</summary>
    public string? OldestId => Messages.Count == 0 ? null : Messages[0].Id;

    public string? LastId => Messages.Count == 0 ? null : Messages[^1].Id;

    public bool Contains(string messageId) => Messages.Any(m => m.Id == messageId);

    /// <summary>The page before the oldest message, added above. Fewer than a full page means it reached the start.</summary>
    public ChatHistory WithOlder(IReadOnlyList<ChatMessage> page) => new(Merge(page, Messages), IsFull(page));

    /// <summary>
    /// The chat's newest page read again (a new message arrived), added under what is loaded. When it doesn't reach
    /// back to what is loaded (many messages came in at once), it replaces it, so there is never a gap in the middle.
    /// </summary>
    public ChatHistory WithLatest(IReadOnlyList<ChatMessage> page) =>
        Messages.Count == 0 || page.Count == 0 || page.Any(m => Contains(m.Id)) || page[0].At <= Messages[^1].At
            ? this with { Messages = Merge(Messages, page) }
            : FromLatest(page);

    /// <summary>A message just saved (sent or a call), added at the bottom.</summary>
    public ChatHistory With(ChatMessage message) => this with { Messages = Merge(Messages, [message]) };

    private static bool IsFull(IReadOnlyList<ChatMessage> page) => page.Count >= Paging.Messages;

    /// <summary>Both lists in time order, each message once (the later copy wins, e.g. the server's time stamp).</summary>
    private static IReadOnlyList<ChatMessage> Merge(IEnumerable<ChatMessage> first, IEnumerable<ChatMessage> second) =>
        [.. first.Concat(second)
            .GroupBy(m => m.Id)
            .Select(g => g.Last())
            .OrderBy(m => m.At)
            .ThenBy(m => m.Id, StringComparer.Ordinal)];
}
