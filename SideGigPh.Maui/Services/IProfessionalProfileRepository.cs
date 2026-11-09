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
}
