namespace SideGigPh.Maui.Models;

/// <summary>A Professional as a Seeker sees them in search results.</summary>
public sealed class ProfessionalListing
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required IReadOnlyList<string> CategoryIds { get; init; }

    public required IReadOnlyList<string> ServiceIds { get; init; }

    public required IReadOnlyList<string> CityIds { get; init; }

    public string? ExperienceId { get; init; }

    public string? RateTierId { get; init; }

    /// <summary>E.164 format, e.g. +639171234567.</summary>
    public required string MobileNumber { get; init; }

    /// <summary>The pro's own photo as an image source (URL or data URL), or null for initials.</summary>
    public string? PhotoUrl { get; init; }

    /// <summary>Photos of their work as image sources, newest first.</summary>
    public IReadOnlyList<string> WorkPhotoUrls { get; init; } = [];

    /// <summary>Average stars and review count, or null with no reviews yet. Search ranks by it.</summary>
    public ReviewSummary? Reviews { get; init; }

    /// <summary>
    /// How many people vouch for this worker (their private references). Seekers see only this count,
    /// never the names or numbers.
    /// </summary>
    public int ReferenceCount { get; init; }

    /// <summary>The badges a person at Tarbaho approved (ID, TESDA, past projects), oldest first. Search ranks by them.</summary>
    public IReadOnlyList<Badge> Badges { get; init; } = [];
}
