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

    /// <summary>The offers on one of the household's posts; under <see cref="Mine"/> so the Post tab stays lit.</summary>
    public static string Offers(long postId) => $"{Mine}/{postId}/offers";

    /// <summary>What customers nearby posted for the worker's trade (the pro's Requests tab).</summary>
    public const string Feed = "/professional/requests";

    /// <summary>The worker's own offers and how each stands (<c>MyOffersPage</c>), under the Requests tab.</summary>
    public const string MyOffers = Feed + "/offers";
}
