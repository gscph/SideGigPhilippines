namespace SideGigPh.Maui.Models;

/// <summary>
/// The signed-in person, whichever side they're on (<c>Users</c> in Azure SQL, through SideGigPh.Api). Name and
/// phone come from the account; language and role from the app.
/// The interview answers themselves stay on the phone (<c>IOnboardingStore</c>). <see cref="ProfilePhotoId"/> is the
/// account's one profile photo: the Seeker's own, or the Skilled Worker's listing photo. <see cref="SeekerFinishedAt"/>
/// (the household side's first finish) and the home town and barangay are kept so a reinstall brings the household
/// side back (<c>AccountRestore</c>); a save that leaves them null never clears them.
/// </summary>
public sealed record UserProfile(
    string Uid,
    string? DisplayName,
    string? PhoneNumber,
    string? Email,
    AppLanguage Language,
    UserRole? Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ProfilePhotoId = null,
    DateTimeOffset? SeekerFinishedAt = null,
    string? HomeTownId = null,
    string? HomeBarangayId = null);
