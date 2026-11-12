using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>The signed-in person's profile (<c>Users</c>). Only that person reads or writes it.</summary>
public sealed class UserStore(SideGigPhDb db)
{
    public async Task<UserDto?> GetAsync(string uid, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Uid == uid).Select(u => ToDto(u)).FirstOrDefaultAsync(cancellationToken);

    /// <summary>Adds or replaces the profile. The uid is the caller's; the first save's time is kept.</summary>
    public async Task<UserDto> SaveAsync(string uid, UserDto profile, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await db.Users.FirstOrDefaultAsync(u => u.Uid == uid, cancellationToken);
        if (row is null)
        {
            row = new UserRow { Uid = uid, Language = Wire.English, CreatedAt = now };
            db.Users.Add(row);
        }

        row.DisplayName = StoreText.Limit(profile.DisplayName, 80);
        row.PhoneNumber = StoreText.Limit(profile.PhoneNumber, 16);
        row.Email = StoreText.Limit(profile.Email, 254);
        row.Language = profile.Language == Wire.Taglish ? Wire.Taglish : Wire.English;
        row.Role = profile.Role is Wire.Seeker or Wire.Professional ? profile.Role : null;
        row.IncomeGoalId = StoreText.IsCode(profile.IncomeGoalId) ? profile.IncomeGoalId : null;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(row);
    }

    public Task DeleteAsync(string uid, CancellationToken cancellationToken) =>
        db.Users.Where(u => u.Uid == uid).ExecuteDeleteAsync(cancellationToken);

    private static UserDto ToDto(UserRow u) =>
        new(u.Uid, u.DisplayName, u.PhoneNumber, u.Email, u.Language, u.Role, u.IncomeGoalId, u.CreatedAt, u.UpdatedAt);
}
