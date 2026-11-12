namespace SideGigPh.Maui.Models;

/// <summary>
/// The signed-in person, whichever side they're on (<c>Users</c> in Azure SQL, through SideGigPh.Api). Name and
/// phone come from the account; language, role and the private earnings goal from the app.
/// The interview answers themselves stay on the phone (<c>IOnboardingStore</c>).
/// </summary>
public sealed record UserProfile(
    string Uid,
    string? DisplayName,
    string? PhoneNumber,
    string? Email,
    AppLanguage Language,
    UserRole? Role,
    string? IncomeGoalId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
