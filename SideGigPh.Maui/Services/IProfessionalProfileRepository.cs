using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Profile data port. "Professional" = the side-gig service provider (aircon tech,
/// plumber, and also non-trade pros like web developers) — deliberately NOT named
/// "tradesman". Geo-search and full CRUD will grow behind this port.
/// </summary>
public interface IProfessionalProfileRepository
{
    /// <summary>Whether the authenticated user already has a Professional profile.</summary>
    Task<bool> HasProfileAsync(string uid);

    /// <summary>Professionals offering a category, limited to a town when one is given.</summary>
    Task<IReadOnlyList<ProfessionalListing>> SearchAsync(ProfessionalSearchQuery query);

    /// <summary>One published listing, or null if it's gone.</summary>
    Task<ProfessionalListing?> GetByIdAsync(string professionalId);

    /// <summary>
    /// Makes the listing searchable, or replaces the pro's earlier version of it.
    /// Enforces anti-fraud rule 1: one mobile number, one listing.
    /// </summary>
    Task<PublishResult> PublishAsync(ProfessionalListing listing);

    /// <summary>Takes the listing out of search, e.g. when the pro starts over. Frees its mobile number.</summary>
    Task RemoveAsync(string professionalId);
}
