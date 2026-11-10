using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Seekers' reviews of the Professionals they contacted.</summary>
public interface IReviewRepository
{
    /// <summary>The Professional's reviews, newest first.</summary>
    Task<IReadOnlyList<Review>> GetForProfessionalAsync(string professionalId);

    /// <summary>The reviews a Seeker wrote, newest first.</summary>
    Task<IReadOnlyList<Review>> GetBySeekerAsync(string seekerId);

    /// <summary>Average and count per pro, for ranking search results. Pros without reviews are left out.</summary>
    Task<IReadOnlyDictionary<string, ReviewSummary>> GetSummariesAsync(IReadOnlyCollection<string> professionalIds);

    /// <summary>Adds the review, or replaces this Seeker's earlier review of the same pro.</summary>
    Task SaveAsync(Review review);
}
