using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Job posts: a household posts the work it needs done; workers of that trade who travel that far see it. A person
/// changes only their own posts.
/// </summary>
public interface IJobPostRepository
{
    /// <summary>Posts the household's work (open at once) and answers with it. Throws when it didn't send.</summary>
    Task<JobPost> AddAsync(JobPostDraft draft);

    /// <summary>This person's own posts, newest first, closed ones too. Empty when it can't be read.</summary>
    Task<IReadOnlyList<JobPost>> GetMineAsync();

    /// <summary>Takes this person's own post down (it stays in their list, closed). Throws when it didn't send.</summary>
    Task CloseAsync(long postId);

    /// <summary>
    /// One page of the open posts this worker can take, after the first <paramref name="skip"/>: their trade, within
    /// the distance they travel; a service they offer first, then nearest, then newest. Empty when it can't be read.
    /// </summary>
    Task<JobPostFeed> GetFeedAsync(int skip);

    /// <summary>Reports a fake or prank post to the operator. Throws when it didn't send.</summary>
    Task ReportAsync(long postId);
}
