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
public sealed class SafetyStore(SideGigPhDb db)
{
    public const int MaxNote = 500;

    /// <summary>The caller reports another person, and blocks them too when asked.</summary>
    public async Task<ReportOutcome> ReportAsync(string uid, ReportRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetUid is not { Length: > 0 and <= ChatStore.MaxUid } || request.TargetUid == uid
            || !ReportKinds.All.Contains(request.Kind) || !ReportReasons.All.Contains(request.Reason)
            || request.SubjectId is { Length: > 260 })
        {
            return ReportOutcome.Invalid;
        }

        db.Reports.Add(new ReportRow
        {
            ReporterUid = uid,
            TargetUid = request.TargetUid,
            Kind = request.Kind,
            SubjectId = StoreText.Limit(request.SubjectId, 260),
            Reason = request.Reason,
            Note = StoreText.Limit(request.Note, MaxNote),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        if (request.AlsoBlock)
        {
            await BlockAsync(uid, request.TargetUid, cancellationToken);
        }

        return ReportOutcome.Saved;
    }

    /// <summary>The caller blocks <paramref name="blockedUid"/>. False for an id that can't be someone else. Blocking twice changes nothing.</summary>
    public async Task<bool> BlockAsync(string uid, string blockedUid, CancellationToken cancellationToken)
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
