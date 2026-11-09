namespace SideGigPh.Maui.Models;

/// <summary>Find Professionals in a category, optionally only those who go to a town.</summary>
public sealed record ProfessionalSearchQuery(string CategoryId, string? CityId);
