using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Seekers' reviews of the Professionals they contacted.</summary>
public interface IReviewRepository
{
    /// <summary>The Professional's reviews, newest first.</summary>
    Task<IReadOnlyList<Review>> GetForProfessionalAsync(string professionalId);

    /// <summary>The reviews a Seeker wrote, newest first.</summary>
    Task<IReadOnlyList<Review>> GetBySeekerAsync(string seekerId);

    /// <summary>Adds the review, or replaces this Seeker's earlier review of the same pro.</summary>
    Task SaveAsync(Review review);

    /// <summary>Deletes every review this person wrote (as a Seeker) or got (as a Professional), e.g. on Start over.</summary>
    Task DeleteForPersonAsync(string personId);
}
