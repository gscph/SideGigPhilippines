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
}
