namespace SideGigPh.Maui.Models;

/// <summary>Where a suggestion on the feedback board stands.</summary>
public enum FeedbackStatus
{
    /// <summary>New: people can vote.</summary>
    Open,

    /// <summary>We will build it.</summary>
    Planned,

    /// <summary>It is in the app.</summary>
    Done,
}

/// <summary>
/// One suggestion on the feedback board. Nobody sees who wrote it. <paramref name="Votes"/> is how many people want
/// it (the writer is the first); <paramref name="VotedByMe"/> and <paramref name="Mine"/> are about this phone's user.
/// </summary>
public sealed record FeedbackPost(
    long Id,
    string Title,
    string? Details,
    int Votes,
    FeedbackStatus Status,
    bool VotedByMe,
    bool Mine,
    DateTimeOffset CreatedAt)
{
    /// <summary>The writer may take it back while they are its only vote (nobody else wants it yet).</summary>
    public bool CanRemove => Mine && Votes <= 1;
}

/// <summary>One page of the board, most voted first; <paramref name="HasMore"/>: another page follows.</summary>
public sealed record FeedbackPage(IReadOnlyList<FeedbackPost> Posts, bool HasMore);
