using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Profile data port. "Professional" = the side-gig service provider (aircon tech,
/// plumber, and also non-trade pros like web developers) — deliberately NOT named
/// "tradesman". Geo-search and full CRUD will grow behind this port.
/// </summary>
public interface IProfessionalProfileRepository
{
    /// <summary>
    /// The signed-in worker's own live listing as interview answers, photo ids and private references included, read
    /// fresh from the server (a reinstalled app restores it: <see cref="ListingRestore"/>). Null when they have no
    /// listing or it couldn't be read (logged).
    /// </summary>
    Task<ProfessionalDraft?> GetOwnDraftAsync(string uid);

    /// <summary>
    /// One page of a search, ranked by the server: the <c>Paging.Search</c> results after the first
    /// <paramref name="skip"/> (the ones already shown).
    /// </summary>
    Task<SearchResult> SearchAsync(ProfessionalSearchQuery query, int skip);

    /// <summary>One published listing, or null if it's gone.</summary>
    Task<ProfessionalListing?> GetByIdAsync(string professionalId);

    /// <summary>
    /// Makes the listing searchable, or replaces the pro's earlier version of it.
    /// Enforces anti-fraud rule 1: one mobile number, one listing. <paramref name="references"/> are stored privately
    /// with it (never shown to customers); the server needs a profile photo and 1 reference (work photos are optional).
    /// </summary>
    Task<PublishResult> PublishAsync(ProfessionalListing listing, IReadOnlyList<WorkReference> references);

    /// <summary>Takes the listing out of search, e.g. when the pro starts over. Frees its mobile number.</summary>
    Task RemoveAsync(string professionalId);

    /// <summary>Drops what is kept about this listing and every kept search, so the next reads ask the server (after a block).</summary>
    void Forget(string professionalId);
}
