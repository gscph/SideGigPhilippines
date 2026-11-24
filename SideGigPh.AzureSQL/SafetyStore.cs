using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

public enum ReportOutcome
{
    Saved,
    Invalid,
}

/// <summary>
/// Reports and blocks (<c>Reports</c>, <c>Blocks</c>; App Store 1.2, Google Play user-generated content). A report waits
/// for the operator (answered within 5 business days); a block works at once: either direction hides the two people
/// from each other (search, public profile, Inbox, new messages).
/// </summary>
public sealed class SafetyStore(SideGigPhDb db, AppEventStore events)
{
    public const int MaxNote = 500;

    /// <summary>How long after a report the same report sent again is not kept a second time.</summary>
    public static readonly TimeSpan RepeatWindow = TimeSpan.FromMinutes(2);

    /// <summary>The caller reports another person, and blocks them too when asked.</summary>
    public async Task<ReportOutcome> ReportAsync(string uid, ReportRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetUid is not { Length: > 0 and <= ChatStore.MaxUid } || request.TargetUid == uid
            || !ReportKinds.All.Contains(request.Kind) || !ReportReasons.All.Contains(request.Reason)
            || request.SubjectId is { Length: > 260 })
        {
            return ReportOutcome.Invalid;
        }

        var now = DateTimeOffset.UtcNow;
        var since = now - RepeatWindow;
        var subject = StoreText.Limit(request.SubjectId, 260);
        // The phone sends a write once more when its answer was lost on the way (ApiClient.WriteTries): the same
        // report a moment ago is kept once.
        if (!await db.Reports.AnyAsync(r => r.ReporterUid == uid && r.TargetUid == request.TargetUid && r.Kind == request.Kind
                                            && r.SubjectId == subject && r.Reason == request.Reason && r.CreatedAt >= since, cancellationToken))
        {
            db.Reports.Add(new ReportRow
            {
                ReporterUid = uid,
                TargetUid = request.TargetUid,
                Kind = request.Kind,
                SubjectId = subject,
                Reason = request.Reason,
                Note = StoreText.Limit(request.Note, MaxNote),
                CreatedAt = now,
            });
            await db.SaveChangesAsync(cancellationToken);
            await events.LogAsync(new AppEvent(AppEventNames.ReportUser) { UserId = uid, Reason = request.Reason });
        }
        if (request.AlsoBlock)
        {
            await BlockAsync(uid, request.TargetUid, cancellationToken, request.Reason);
        }

        return ReportOutcome.Saved;
    }

    /// <summary>
    /// The caller blocks <paramref name="blockedUid"/>. False for an id that can't be someone else. Blocking twice changes
    /// nothing. <paramref name="reason"/> is the report's, when the block came with one.
    /// </summary>
    public async Task<bool> BlockAsync(string uid, string blockedUid, CancellationToken cancellationToken, string? reason = null)
    {
        if (blockedUid is not { Length: > 0 and <= ChatStore.MaxUid } || blockedUid == uid)
        {
            return false;
        }

        if (!await db.Blocks.AnyAsync(b => b.BlockerUid == uid && b.BlockedUid == blockedUid, cancellationToken))
        {
            db.Blocks.Add(new BlockRow { BlockerUid = uid, BlockedUid = blockedUid, CreatedAt = DateTimeOffset.UtcNow });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await events.LogAsync(new AppEvent(AppEventNames.BlockUser) { UserId = uid, Reason = reason });
            }
            catch (DbUpdateException ex) when (SqlErrors.Violates(ex, "PK_Blocks"))
            {
                // The same block saved by a retry at the same moment: already done.
            }
        }

        return true;
    }

    /// <summary>Everyone hidden from the caller: the people they blocked and the people who blocked them.</summary>
    public async Task<IReadOnlySet<string>> HiddenForAsync(string uid, CancellationToken cancellationToken)
    {
        var blocked = await db.Blocks.AsNoTracking().Where(b => b.BlockerUid == uid).Select(b => b.BlockedUid).ToListAsync(cancellationToken);
        var blockedBy = await db.Blocks.AsNoTracking().Where(b => b.BlockedUid == uid).Select(b => b.BlockerUid).ToListAsync(cancellationToken);
        return blocked.Concat(blockedBy).ToHashSet();
    }
}
