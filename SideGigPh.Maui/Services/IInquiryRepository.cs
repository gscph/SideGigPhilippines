using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Calls and texts from Seekers to Professionals: written when a Seeker contacts a pro, read as
/// the pro's Inbox and, grouped by pro, the Seeker's Inbox.
/// </summary>
public interface IInquiryRepository
{
    Task RecordAsync(Inquiry inquiry);

    /// <summary>The Professional's inquiries, newest first.</summary>
    Task<IReadOnlyList<Inquiry>> GetForProfessionalAsync(string professionalId);

    /// <summary>Everything the Seeker sent, oldest first.</summary>
    Task<IReadOnlyList<Inquiry>> GetForSeekerAsync(string seekerId);
}
