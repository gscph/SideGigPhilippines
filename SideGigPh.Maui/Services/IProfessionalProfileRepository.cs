namespace SideGigPh.Maui.Services;

/// <summary>
/// Profile data port. "Professional" = the side-gig service provider (aircon tech,
/// plumber, and also non-trade pros like web developers) — deliberately NOT named
/// "tradesman". Used here only for post-auth routing; full CRUD and geo-search will
/// be added behind this (or a related) port later.
/// </summary>
public interface IProfessionalProfileRepository
{
    /// <summary>Whether the authenticated user already has a Professional profile.</summary>
    Task<bool> HasProfileAsync(string uid);
}
