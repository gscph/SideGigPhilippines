using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>The signed-in person's profile (<c>Users</c>). Only that person reads or writes it.</summary>
public sealed class UserStore(SideGigPhDb db, ListingCache listings, AppEventStore events)
{
    public async Task<UserDto?> GetAsync(string uid, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Uid == uid).Select(u => ToDto(u)).FirstOrDefaultAsync(cancellationToken);

    /// <summary>Adds or replaces the profile. The uid is the caller's; the first save's time is kept.</summary>
    public async Task<UserDto> SaveAsync(string uid, UserDto profile, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await db.Users.FirstOrDefaultAsync(u => u.Uid == uid, cancellationToken);
        var hadRole = row?.Role is not null;
        if (row is null)
        {
            row = new UserRow { Uid = uid, Language = Wire.English, CreatedAt = now };
            db.Users.Add(row);
        }

        // A name with a blocked word never shows on reviews or chats: the one already saved stays (the app's name page
        // refuses it before sending, so only an old app or a script gets here).
        if (!BlockedWords.Contains(profile.DisplayName))
        {
            row.DisplayName = StoreText.Limit(profile.DisplayName, 80);
        }
        row.PhoneNumber = StoreText.Limit(profile.PhoneNumber, 16);
        row.Email = StoreText.Limit(profile.Email, 254);
        row.Language = profile.Language == Wire.Taglish ? Wire.Taglish : Wire.English;
        row.Role = profile.Role is Wire.Seeker or Wire.Professional ? profile.Role : null;
        row.IncomeGoalId = StoreText.IsCode(profile.IncomeGoalId) ? profile.IncomeGoalId : null;
        row.ProfilePhotoId = StoreText.IsPhotoId(profile.ProfilePhotoId) ? profile.ProfilePhotoId : null;
        row.UpdatedAt = now;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.UserProfilePhoto))
        {
            // Another account has this photo id (only a copied id can do that: every upload gets a new id). Keep the
            // rest of the profile and leave the photo off.
            row.ProfilePhotoId = null;
            await db.SaveChangesAsync(cancellationToken);
        }

        // Signed up = the account's first side picked (a profile saved before the role pick isn't counted yet).
        if (!hadRole && row.Role is not null && !await events.HasAsync(AppEventNames.SignUp, uid))
        {
            await events.LogAsync(new AppEvent(AppEventNames.SignUp) { UserId = uid, UserType = AppEventUserTypes.Of(row.Role) });
        }

        return ToDto(row);
    }

    /// <summary>How often <see cref="TouchAsync"/> writes at most: activity is ranked in days, so an hour is plenty.</summary>
    public static readonly TimeSpan ActivityResolution = TimeSpan.FromHours(1);

    /// <summary>
    /// The person is using the app now (<c>Users.LastActiveAt</c>, which search ranking reads). Writes at most once per
    /// <see cref="ActivityResolution"/>, so calling it on every Inbox read costs one cheap no-op update.
    /// </summary>
    public Task TouchAsync(string uid, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var stale = now - ActivityResolution;
        return db.Users.Where(u => u.Uid == uid && (u.LastActiveAt == null || u.LastActiveAt < stale))
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.LastActiveAt, now), cancellationToken);
    }

    /// <summary>Deletes the profile and the phones registered for its notifications (Start over).</summary>
    public async Task DeleteAsync(string uid, CancellationToken cancellationToken)
    {
        await db.PushDevices.Where(d => d.Uid == uid).ExecuteDeleteAsync(cancellationToken);
        // Their "Others" notes stay for analytics, without saying who wrote them.
        await db.OtherJobNotes.Where(n => n.Uid == uid).ExecuteUpdateAsync(set => set.SetProperty(n => n.Uid, (string?)null), cancellationToken);
        await db.Users.Where(u => u.Uid == uid).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Delete account: everything the person has in the database, in one transaction. Their listing (with its
    /// services, towns and work photos, and any references saved before that feature was dropped), every chat they are in (for both people), the reviews they wrote
    /// AND the reviews about them, their blocks both ways, their phones and profile. Reports they made lose their name;
    /// reports about them stay for the operator. "Others" notes stay without their uid, and so do the features they
    /// suggested on the feedback board (shown without a name anyway); their votes there are removed. Answers with the photo ids
    /// the person used, so the caller can delete the stored photos too.
    /// </summary>
    public async Task<IReadOnlyList<string>> DeleteAccountAsync(string uid, CancellationToken cancellationToken)
    {
        var photoIds = await db.InTransactionAsync<IReadOnlyList<string>>(async ct =>
        {
            var photos = await db.ProfessionalWorkPhotos.Where(w => w.ProfessionalId == uid).Select(w => w.PhotoId).ToListAsync(ct);
            photos.AddRange(await db.Professionals.Where(p => p.Id == uid && p.ProfilePhotoId != null).Select(p => p.ProfilePhotoId!).ToListAsync(ct));
            photos.AddRange(await db.Users.Where(u => u.Uid == uid && u.ProfilePhotoId != null).Select(u => u.ProfilePhotoId!).ToListAsync(ct));

            // References are no longer collected, but rows saved before then are personal data: purge them by name
            // rather than rely only on the database's cascade from Professionals.
            await db.ProfessionalReferences.Where(r => r.ProfessionalId == uid).ExecuteDeleteAsync(ct);
            await db.Professionals.Where(p => p.Id == uid).ExecuteDeleteAsync(ct);
            await db.Chats.Where(c => c.SeekerId == uid || c.ProfessionalId == uid).ExecuteDeleteAsync(ct);
            await db.Reviews.Where(r => r.SeekerId == uid || r.ProfessionalId == uid).ExecuteDeleteAsync(ct);
            await db.HouseholdReviews.Where(r => r.SeekerUid == uid || r.ProUid == uid).ExecuteDeleteAsync(ct);
            await db.Blocks.Where(b => b.BlockerUid == uid || b.BlockedUid == uid).ExecuteDeleteAsync(ct);
            await db.Reports.Where(r => r.ReporterUid == uid).ExecuteUpdateAsync(set => set.SetProperty(r => r.ReporterUid, (string?)null), ct);
            await db.PushDevices.Where(d => d.Uid == uid).ExecuteDeleteAsync(ct);
            await db.OtherJobNotes.Where(n => n.Uid == uid).ExecuteUpdateAsync(set => set.SetProperty(n => n.Uid, (string?)null), ct);
            await AppEventStore.ForgetAsync(db, uid, ct);
            await FeedbackStore.ForgetAsync(db, uid, ct);
            photos.AddRange(await JobPostStore.ForgetAsync(db, uid, ct));
            await db.Users.Where(u => u.Uid == uid).ExecuteDeleteAsync(ct);
            return photos.Distinct().ToList();
        }, cancellationToken);

        listings.Clear(); // search results carry the listing and the stars
        return photoIds;
    }

    private static UserDto ToDto(UserRow u) =>
        new(u.Uid, u.DisplayName, u.PhoneNumber, u.Email, u.Language, u.Role, u.IncomeGoalId, u.CreatedAt, u.UpdatedAt, u.ProfilePhotoId);
}
