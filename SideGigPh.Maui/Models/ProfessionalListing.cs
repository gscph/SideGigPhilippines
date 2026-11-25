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

    /// <summary>Their average price per service, in pesos; null only on a draft before the price question.</summary>
    public int? RatePesos { get; init; }

    /// <summary>What the worker typed under "Others" (a kind of work we don't list, or a job their trade doesn't list); shown in place of "Others".</summary>
    public string? OtherWork { get; init; }

    /// <summary>E.164 format, e.g. +639171234567.</summary>
    public required string MobileNumber { get; init; }

    /// <summary>The pro's own photo as an image source (URL or data URL), or null for initials.</summary>
    public string? PhotoUrl { get; init; }

    /// <summary>Photos of their work as image sources, newest first.</summary>
    public IReadOnlyList<string> WorkPhotoUrls { get; init; } = [];

    /// <summary>Average stars and review count, or null with no reviews yet. Search ranks by it.</summary>
    public ReviewSummary? Reviews { get; init; }

    /// <summary>The badges a person at Tarbaho approved (ID, clearance), oldest first. Search ranks by them.</summary>
    public IReadOnlyList<Badge> Badges { get; init; } = [];

    /// <summary>Jobs done through Tarbaho: a household picked this worker's offer and both marked it done.</summary>
    public int CompletedJobs { get; init; }

    /// <summary>
    /// The worker's own base point, from their phone (only on their own listing, never sent to others; null = the
    /// server keeps what it has, or the middle of their towns).
    /// </summary>
    public GeoPoint? BasePoint { get; init; }

    /// <summary>How far they travel, in km (only on their own listing; null = the server keeps what it has).</summary>
    public int? RadiusKm { get; init; }
}
