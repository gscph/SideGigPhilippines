using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Firestore;

/// <summary><see cref="IUserRepository"/> on <c>users/{uid}</c>. The rules let only that person read or write it.</summary>
public sealed class FirestoreUserRepository(ILogger<FirestoreUserRepository> logger) : IUserRepository
{
    public Task<UserProfile?> GetAsync(string uid) => FirestoreAccess.ReadAsync(async () =>
    {
        var snapshot = await FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Users}/{uid}").GetDocumentSnapshotAsync<UserDoc>();
        return snapshot.Data is { } doc ? ToProfile(uid, doc) : null;
    }, null, logger, $"the profile of {uid}");

    public async Task SaveAsync(UserProfile profile)
    {
        try
        {
            await FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Users}/{profile.Uid}").SetDataAsync(ToDoc(profile));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't save the profile of {Uid}.", profile.Uid);
            throw;
        }
    }

    public async Task DeleteAsync(string uid)
    {
        try
        {
            await FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Users}/{uid}").DeleteDocumentAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't delete the profile of {Uid}.", uid);
            throw;
        }
    }

    private static UserDoc ToDoc(UserProfile profile) => new()
    {
        DisplayName = FirestoreAccess.Limit(profile.DisplayName, 80),
        PhoneNumber = profile.PhoneNumber,
        Email = FirestoreAccess.Limit(profile.Email, 254),
        Language = profile.Language.ToString().ToLowerInvariant(),
        Role = profile.Role?.ToString().ToLowerInvariant(),
        IncomeGoalId = profile.IncomeGoalId,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt,
    };

    private static UserProfile ToProfile(string uid, UserDoc doc) => new(
        uid,
        doc.DisplayName,
        doc.PhoneNumber,
        doc.Email,
        Enum.TryParse<AppLanguage>(doc.Language, ignoreCase: true, out var language) ? language : AppLanguage.English,
        Enum.TryParse<UserRole>(doc.Role, ignoreCase: true, out var role) ? role : null,
        doc.IncomeGoalId,
        doc.CreatedAt,
        doc.UpdatedAt);
}
