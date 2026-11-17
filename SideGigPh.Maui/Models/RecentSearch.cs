namespace SideGigPh.Maui.Models;

/// <summary>
/// One of the household's recent searches or posts, kept only on the phone (in the onboarding file, never sent):
/// Seeker home shows them so one tap searches again or opens the post form filled with them.
/// Ids refer to entries in <c>ReferenceData</c>.
/// </summary>
/// <param name="OtherJobText">What they typed for an "Others" job, else null.</param>
/// <param name="BudgetId">A <c>BudgetRange</c> id, <c>ReferenceData.Unsure</c>, or null.</param>
public sealed record RecentSearch(string CategoryId, string? ServiceId, string? OtherJobText, string? CityId, string? BudgetId);
