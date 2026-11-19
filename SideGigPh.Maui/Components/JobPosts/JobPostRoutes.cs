using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The job posts' routes (<c>SeekerRoutes.Posts</c> and <c>ProRoutes.Requests</c> point here): the household's own
/// posts and the form to post, and the worker's feed of posts for their trade nearby.
/// </summary>
public static class JobPostRoutes
{
    /// <summary>The household's own posts (the Seeker's Post tab).</summary>
    public const string Mine = "/posts";

    /// <summary>The form to post what they need done; under <see cref="Mine"/>, full screen.</summary>
    public const string New = Mine + "/new";

    /// <summary>The same form, filled in, to change one of the household's open posts; full screen.</summary>
    public static string Edit(long postId) => $"{Mine}/{postId}/edit";

    /// <summary>The new post form, or the edit form of <paramref name="postId"/>.</summary>
    public static string Form(long? postId) => postId is { } id ? Edit(id) : New;

    /// <summary>
    /// One Change sheet of the form (<c>/posts/new/where</c>), or the What sheet's jobs page for
    /// <paramref name="jobsOfTrade"/> (<c>/posts/new/what/aircon</c>): its own URL, so the phone's Back closes it.
    /// </summary>
    public static string Sheet(long? postId, PostPart part, string? jobsOfTrade = null) =>
        $"{Form(postId)}/{PostReview.Slug(part)}" + (jobsOfTrade is null ? string.Empty : $"/{Uri.EscapeDataString(jobsOfTrade)}");

    /// <summary>The offers on one of the household's posts; under <see cref="Mine"/> so the Post tab stays lit.</summary>
    public static string Offers(long postId) => $"{Mine}/{postId}/offers";

    /// <summary>What customers nearby posted for the worker's trade (the pro's Requests tab).</summary>
    public const string Feed = "/professional/requests";

    /// <summary>The worker's own offers and how each stands (<c>MyOffersPage</c>), under the Requests tab.</summary>
    public const string MyOffers = Feed + "/offers";

    /// <summary>One request's own page (<c>RequestDetailPage</c>): the description, photos and the worker's offer.</summary>
    public static string Request(long postId) => $"{Feed}/{postId}";
}
