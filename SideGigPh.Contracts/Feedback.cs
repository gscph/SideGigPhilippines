namespace SideGigPh.Contracts;

/// <summary>
/// One suggestion on the feedback board, as the caller sees it. The writer's name is never sent: the board is
/// anonymous. <paramref name="Status"/> is one of <see cref="FeedbackStatuses"/> (only the ones on the board).
/// </summary>
/// <param name="Votes">How many people want it (the writer counts as the first).</param>
/// <param name="VotedByMe">The caller is one of them.</param>
/// <param name="Mine">The caller wrote it.</param>
public sealed record FeedbackPostDto(
    long Id,
    string Title,
    string? Details,
    int Votes,
    string Status,
    bool VotedByMe,
    bool Mine,
    DateTimeOffset CreatedAt);

/// <summary><c>GET feedback</c>: one page of the board, most voted first (finished ones last). <paramref name="HasMore"/>: another page follows.</summary>
public sealed record FeedbackPageDto(List<FeedbackPostDto> Posts, bool HasMore);

/// <summary>
/// <c>POST feedback</c>: a new suggestion. <paramref name="Side"/> is <see cref="Wire.Seeker"/> or
/// <see cref="Wire.Professional"/> (which kind of user asked), or null.
/// </summary>
public sealed record NewFeedbackRequest(string Title, string? Details, string? Side);

/// <summary><c>PUT</c> / <c>DELETE feedback/{id}/vote</c>: the suggestion's votes after the caller's change.</summary>
public sealed record FeedbackVoteDto(long Id, int Votes, bool VotedByMe);

/// <summary>Where a suggestion stands. The operator changes it in the database; <see cref="Declined"/> and <see cref="Hidden"/> leave the board.</summary>
public static class FeedbackStatuses
{
    /// <summary>New: people can vote.</summary>
    public const string Open = "open";

    /// <summary>We will build it.</summary>
    public const string Planned = "planned";

    /// <summary>It is in the app.</summary>
    public const string Done = "done";

    /// <summary>We won't build it (off the board).</summary>
    public const string Declined = "declined";

    /// <summary>Removed by the operator: spam, abuse or a duplicate (off the board).</summary>
    public const string Hidden = "hidden";
}

/// <summary>The limits both sides keep for a suggestion (the columns of <c>FeedbackPosts</c>).</summary>
public static class FeedbackRules
{
    /// <summary>The shortest title that says something ("Add GCash").</summary>
    public const int MinTitle = 6;

    public const int MaxTitle = 100;

    public const int MaxDetails = 600;

    /// <summary>How many look-alikes the "is it one of these?" step shows at most.</summary>
    public const int MaxSimilar = 5;

    /// <summary>True when <paramref name="title"/> (trimmed) is long enough and fits.</summary>
    public static bool IsTitle(string? title) => title?.Trim() is { Length: >= MinTitle and <= MaxTitle };
}
