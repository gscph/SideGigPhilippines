using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The feedback board: features people suggested, with votes. Everyone signed in sees the same board, without names;
/// a person changes only their own suggestion and their own vote.
/// </summary>
public interface IFeedbackRepository
{
    /// <summary>
    /// One page of the board after the first <paramref name="skip"/> suggestions: open and planned ones most voted
    /// first (then newest), finished ones last. Empty when it can't be read.
    /// </summary>
    Task<FeedbackPage> GetPageAsync(int skip);

    /// <summary>Suggestions on the board that probably ask for the same thing as <paramref name="text"/>, closest first. Empty when none, or when it can't be read.</summary>
    Task<IReadOnlyList<FeedbackPost>> FindSimilarAsync(string text);

    /// <summary>Posts a suggestion (the writer's vote is its first) and answers with it. Throws when it didn't send.</summary>
    Task<FeedbackPost> AddAsync(string title, string? details, UserRole? role);

    /// <summary>Adds (<paramref name="on"/>) or takes back this person's vote; answers the suggestion's votes now. Throws when it didn't send.</summary>
    Task<int> VoteAsync(long postId, bool on);

    /// <summary>Removes this person's own suggestion (only while nobody else voted for it). Throws when it wasn't removed.</summary>
    Task RemoveAsync(long postId);

    /// <summary>Reports a suggestion to the operator (spam, abuse). Throws when it didn't send.</summary>
    Task ReportAsync(long postId);
}
