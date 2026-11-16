using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>What the AI read from an ID photo: a status from <see cref="ScanStatuses"/> and whichever fields it found.</summary>
public sealed record IdScan(string Status, string? DocumentType = null, string? Name = null, string? Number = null, DateOnly? Expiry = null)
{
    public static readonly IdScan None = new(ScanStatuses.None);
    public static readonly IdScan Failed = new(ScanStatuses.Failed);
}

/// <summary>What a worker sends for a badge, as the function read it from the request.</summary>
public sealed record VerificationSubmission(string Kind, string? BadgeId, string? ProjectName, bool Consent);

public enum SubmitOutcome
{
    Saved,

    /// <summary>The kind, the developer or the project name isn't valid.</summary>
    Invalid,

    /// <summary>The consent box wasn't ticked.</summary>
    ConsentRequired,

    /// <summary>This badge is already waiting for the operator, or already approved.</summary>
    AlreadySent,
}

/// <summary>The operator's decision as saved: the worker it was about and the file to delete now, or null when there was nothing to decide.</summary>
public sealed record Decided(string Uid, string? DocumentBlob);

/// <summary>
/// Worker verification (<c>WorkerVerifications</c>, <c>VerificationAudit</c>): a worker sends a proof with their
/// consent, an operator looks at it and decides, an approved one is a badge on the listing. Government IDs,
/// certificates and payslips are sensitive personal information (Data Privacy Act, RA 10173), so: the file lives in a
/// private container, every look and decision is logged, and when the operator decides the file's name and
/// everything read from it are cleared here (the caller deletes the file): only the result is kept.
/// </summary>
public sealed class VerificationStore(SideGigPhDb db, PhotoLinks photoLinks, ListingCache listings)
{
    public const int MaxProjectName = 100;
    public const int MaxRejectReason = 200;

    /// <summary>The most requests the operator's queue returns at once.</summary>
    public const int MaxQueue = 50;

    /// <summary>Null when <paramref name="submission"/> can be saved, else why not (before the file is stored anywhere).</summary>
    public async Task<SubmitOutcome?> RefusalAsync(string uid, VerificationSubmission submission, CancellationToken cancellationToken)
    {
        if (!VerificationKinds.All.Contains(submission.Kind)
            || (submission.Kind == VerificationKinds.Developer) != DeveloperBadges.All.Contains(submission.BadgeId ?? string.Empty)
            || (submission.Kind != VerificationKinds.Developer && !string.IsNullOrEmpty(submission.BadgeId)))
        {
            return SubmitOutcome.Invalid;
        }

        if (!submission.Consent)
        {
            return SubmitOutcome.ConsentRequired;
        }

        var badgeId = submission.BadgeId ?? string.Empty;
        return await db.WorkerVerifications.AnyAsync(
            v => v.Uid == uid && v.Kind == submission.Kind && v.BadgeId == badgeId && v.Status != VerificationStatuses.Rejected, cancellationToken)
            ? SubmitOutcome.AlreadySent
            : null;
    }

    /// <summary>
    /// Saves the request with the stored file's name and what was read from it. Null when the same badge was sent
    /// at the same moment (a double tap): the caller deletes the file it just stored.
    /// </summary>
    public async Task<VerificationDto?> SubmitAsync(
        string uid, VerificationSubmission submission, string documentBlob, IdScan scan, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var row = new WorkerVerificationRow
        {
            Uid = uid,
            Kind = submission.Kind,
            BadgeId = submission.BadgeId ?? string.Empty,
            ProjectName = submission.Kind == VerificationKinds.Developer ? StoreText.Limit(submission.ProjectName, MaxProjectName) : null,
            DocumentBlob = documentBlob,
            ScanStatus = scan.Status,
            ScannedType = StoreText.Limit(scan.DocumentType, 60),
            ScannedName = StoreText.Limit(scan.Name, 120),
            ScannedNumber = StoreText.Limit(scan.Number, 60),
            ScannedExpiry = scan.Expiry,
            ConsentAt = now,
            CreatedAt = now,
        };

        try
        {
            return await db.InTransactionAsync(async ct =>
            {
                row.Id = 0; // a retried transaction inserts it again
                db.WorkerVerifications.Add(row);
                await db.SaveChangesAsync(ct);
                db.VerificationAudit.Add(Audit(row.Id, uid, "submitted", now));
                if (scan.Status != ScanStatuses.None)
                {
                    db.VerificationAudit.Add(Audit(row.Id, uid, "scanned", now));
                }

                await db.SaveChangesAsync(ct);
                return ToDto(row);
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.VerificationPending))
        {
            return null;
        }
    }

    /// <summary>The caller's own requests, newest first.</summary>
    public async Task<IReadOnlyList<VerificationDto>> MineAsync(string uid, CancellationToken cancellationToken) =>
        [.. (await db.WorkerVerifications.AsNoTracking()
            .Where(v => v.Uid == uid)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken)).Select(ToDto)];

    /// <summary>The operator's queue: requests still waiting, oldest first, with each worker's name and profile photo.</summary>
    public async Task<IReadOnlyList<AdminVerificationDto>> PendingAsync(int take, CancellationToken cancellationToken)
    {
        var rows = await db.WorkerVerifications.AsNoTracking()
            .Where(v => v.Status == VerificationStatuses.Pending)
            .OrderBy(v => v.CreatedAt)
            .Take(Math.Clamp(take, 1, MaxQueue))
            .ToListAsync(cancellationToken);
        var uids = rows.Select(v => v.Uid).Distinct().ToList();
        var people = uids.Count == 0
            ? []
            : await db.Users.AsNoTracking()
                .Where(u => uids.Contains(u.Uid))
                .Select(u => new { u.Uid, u.DisplayName, u.ProfilePhotoId })
                .ToDictionaryAsync(u => u.Uid, cancellationToken);
        var listed = uids.Count == 0
            ? []
            : await db.Professionals.AsNoTracking()
                .Where(p => uids.Contains(p.Id))
                .Select(p => new { p.Id, p.DisplayName, p.ProfilePhotoId })
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        return [.. rows.Select(v =>
        {
            var person = people.GetValueOrDefault(v.Uid);
            var listing = listed.GetValueOrDefault(v.Uid);
            var photoId = listing?.ProfilePhotoId ?? person?.ProfilePhotoId;
            return new AdminVerificationDto(
                v.Id, v.Uid, listing?.DisplayName ?? person?.DisplayName, photoId is null ? null : photoLinks.For(photoId),
                v.Kind, v.BadgeId, v.ProjectName, v.ScanStatus, v.ScannedType, v.ScannedName, v.ScannedNumber, v.ScannedExpiry,
                v.DocumentBlob is not null, v.CreatedAt);
        })];
    }

    /// <summary>The request's file name for the operator to look at, logged as "viewed"; null when there is no file (any more).</summary>
    public async Task<string?> ViewDocumentAsync(long id, string adminUid, CancellationToken cancellationToken)
    {
        var blob = await db.WorkerVerifications.AsNoTracking().Where(v => v.Id == id).Select(v => v.DocumentBlob).FirstOrDefaultAsync(cancellationToken);
        if (blob is null)
        {
            return null;
        }

        db.VerificationAudit.Add(Audit(id, adminUid, "viewed", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(cancellationToken);
        return blob;
    }

    /// <summary>
    /// The operator approves or rejects a waiting request. The file's name and everything read from it are cleared in
    /// the same transaction (logged as "deleted"); the caller deletes the file named in the answer. Null when the
    /// request doesn't exist or was already decided.
    /// </summary>
    public async Task<Decided?> DecideAsync(long id, string adminUid, VerificationDecisionRequest decision, CancellationToken cancellationToken)
    {
        var decided = await db.InTransactionAsync(async ct =>
        {
            var row = await db.WorkerVerifications.FirstOrDefaultAsync(v => v.Id == id && v.Status == VerificationStatuses.Pending, ct);
            if (row is null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            var blob = row.DocumentBlob;
            row.Status = decision.Approve ? VerificationStatuses.Approved : VerificationStatuses.Rejected;
            row.RejectReason = decision.Approve ? null : StoreText.Limit(decision.Reason, MaxRejectReason);
            row.DecidedAt = now;
            row.DecidedBy = adminUid;
            row.DocumentBlob = null;
            row.ScannedType = null;
            row.ScannedName = null;
            row.ScannedNumber = null;
            row.ScannedExpiry = null;
            db.VerificationAudit.Add(Audit(id, adminUid, decision.Approve ? "approved" : "rejected", now));
            if (blob is not null)
            {
                db.VerificationAudit.Add(Audit(id, adminUid, "deleted", now));
            }

            await db.SaveChangesAsync(ct);
            return new Decided(row.Uid, blob);
        }, cancellationToken);

        if (decided is not null && decision.Approve)
        {
            listings.Clear(); // search results carry the badges
        }

        return decided;
    }

    /// <summary>
    /// Delete account: the person's requests go (their badges with them) and the files still stored are named for the
    /// caller to delete. The audit rows stay: they hold no personal data beyond the uid of who acted.
    /// </summary>
    public async Task<IReadOnlyList<string>> ForgetAsync(string uid, CancellationToken cancellationToken)
    {
        var blobs = await db.WorkerVerifications.Where(v => v.Uid == uid && v.DocumentBlob != null).Select(v => v.DocumentBlob!).ToListAsync(cancellationToken);
        await db.WorkerVerifications.Where(v => v.Uid == uid).ExecuteDeleteAsync(cancellationToken);
        return blobs;
    }

    private static VerificationAuditRow Audit(long verificationId, string actorUid, string action, DateTimeOffset at) =>
        new() { VerificationId = verificationId, ActorUid = actorUid, Action = action, At = at };

    private static VerificationDto ToDto(WorkerVerificationRow v) =>
        new(v.Id, v.Kind, v.BadgeId, v.ProjectName, v.Status, v.CreatedAt, v.DecidedAt, v.RejectReason);
}
