namespace SideGigPh.Maui.Models;

/// <summary>
/// Find Professionals in a category, limited to those working in an area (only Rizal for the MVP)
/// when one is given. Which towns inside the area they go to is sorted out by <c>SearchRanker</c>.
/// </summary>
public sealed record ProfessionalSearchQuery(string CategoryId, string? AreaId);
