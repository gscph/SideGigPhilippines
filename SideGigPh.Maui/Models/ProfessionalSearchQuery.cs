namespace SideGigPh.Maui.Models;

/// <summary>
/// One Seeker search, as the server ranks it: a trade's workers in an area (only Rizal for the MVP; null = any),
/// for one job (null = the whole trade), ranked for the Seeker's town and budget.
/// </summary>
public sealed record ProfessionalSearchQuery(string CategoryId, string? AreaId, string? ServiceId, string? CityId, string? BudgetId, string? OtherText = null);
