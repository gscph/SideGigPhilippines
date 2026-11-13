using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>Who gets a new-message notification: the person's phones, their language and how many chats they haven't read.</summary>
/// <param name="Tokens">The person's phones (Firebase Cloud Messaging tokens).</param>
/// <param name="Language"><see cref="Wire.English"/> or <see cref="Wire.Taglish"/>.</param>
/// <param name="UnreadChats">Chats on that side where the other person wrote last and this person hasn't opened it since (the badge).</param>
public sealed record PushTarget(IReadOnlyList<string> Tokens, string Language, int UnreadChats);

/// <summary>
/// The phones that get a person's new-message notifications (<c>PushDevices</c>, one row per phone token). A token
/// belongs to whoever registered it last, so a phone that changes account stops getting the old account's messages.
/// </summary>
public sealed class PushDeviceStore(SideGigPhDb db)
{
    public const int MaxToken = 512;

    /// <summary>This phone now gets the caller's notifications. False for a malformed token or platform.</summary>
    public async Task<bool> SaveAsync(string uid, SaveDeviceRequest request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > MaxToken || token.Any(char.IsWhiteSpace)
            || request.Platform is not (Wire.Android or Wire.Ios))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var changed = await db.PushDevices.Where(d => d.Token == token)
            .ExecuteUpdateAsync(set => set
                .SetProperty(d => d.Uid, uid)
                .SetProperty(d => d.Platform, request.Platform)
                .SetProperty(d => d.UpdatedAt, now), cancellationToken);
        if (changed > 0)
        {
            return true;
        }

        db.PushDevices.Add(new PushDeviceRow { Token = token, Uid = uid, Platform = request.Platform, UpdatedAt = now });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.PushDeviceKey))
        {
            // The same phone registered twice at once: the other request already saved it.
            db.ChangeTracker.Clear();
        }

        return true;
    }

    /// <summary>
    /// Where to send a notification for a message to <paramref name="uid"/> on one side of a chat, or null when they
    /// have no phone registered (nothing to send).
    /// </summary>
    public async Task<PushTarget?> TargetAsync(string uid, bool asSeeker, CancellationToken cancellationToken)
    {
        var tokens = await db.PushDevices.AsNoTracking().Where(d => d.Uid == uid).Select(d => d.Token).ToListAsync(cancellationToken);
        if (tokens.Count == 0)
        {
            return null;
        }

        var language = await db.Users.AsNoTracking().Where(u => u.Uid == uid).Select(u => u.Language).FirstOrDefaultAsync(cancellationToken);
        var chats = db.Chats.AsNoTracking().Where(c => c.LastMessageSenderId != uid);
        var unread = asSeeker
            ? await chats.CountAsync(c => c.SeekerId == uid && (c.SeekerReadAt == null || c.SeekerReadAt < c.LastMessageAt), cancellationToken)
            : await chats.CountAsync(c => c.ProfessionalId == uid && (c.ProfessionalReadAt == null || c.ProfessionalReadAt < c.LastMessageAt), cancellationToken);
        return new PushTarget(tokens, language ?? Wire.English, unread);
    }

    /// <summary>Forgets tokens Google says no longer reach a phone (app removed, data cleared).</summary>
    public Task RemoveAsync(IReadOnlyCollection<string> tokens, CancellationToken cancellationToken) =>
        tokens.Count == 0 ? Task.CompletedTask : db.PushDevices.Where(d => tokens.Contains(d.Token)).ExecuteDeleteAsync(cancellationToken);
}
