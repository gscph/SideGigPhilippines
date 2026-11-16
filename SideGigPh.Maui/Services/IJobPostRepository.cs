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

    /// <summary>
    /// A worker takes the post's budget or names another price, with an optional note; sending again replaces their
    /// earlier offer. Throws when it didn't send (status 404: the post is no longer open).
    /// </summary>
    Task SendOfferAsync(long postId, int pricePesos, string? note);

    /// <summary>A worker takes back their offer. Throws when it didn't send (status 404: nothing waiting there).</summary>
    Task WithdrawOfferAsync(long postId);

    /// <summary>The offers on this person's own post, the picked one first, then the lowest price. Empty when it can't be read.</summary>
    Task<IReadOnlyList<JobOffer>> GetOffersAsync(long postId);

    /// <summary>The household picks an offer, which closes its post. Throws when it didn't send (status 404: no longer possible).</summary>
    Task<JobOffer> PickOfferAsync(long postId, long offerId);

    /// <summary>
    /// The other person's number, once a picked offer links the two (numbers stay private until then). Null before
    /// that, when they have none, or when it can't be read.
    /// </summary>
    Task<string?> GetContactNumberAsync(string otherUid);

    /// <summary>The picked jobs between this person and <paramref name="otherUid"/>, newest first. Empty when none or it can't be read.</summary>
    Task<IReadOnlyList<PickedJob>> GetPickedJobsAsync(string otherUid);

    /// <summary>This person marks a picked job done; answers with it. Throws when it didn't send (status 404: not theirs).</summary>
    Task<PickedJob> MarkDoneAsync(long postId);

    /// <summary>
    /// A worker rates a household after a completed job between them; writing again replaces it. Throws when it didn't
    /// send (status 403: no completed job yet).
    /// </summary>
    Task RateHouseholdAsync(string seekerUid, int rating, string? comment);
}
