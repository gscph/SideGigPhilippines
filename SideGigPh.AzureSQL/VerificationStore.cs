using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// What was read from a proof photo: a status from <see cref="ScanStatuses"/> and whichever fields were found (an ID's
/// name, number and expiry by AI, with its <paramref name="Confidence"/>; a clearance's QR code, <paramref name="Qr"/>, by the server itself).
/// </summary>
public sealed record IdScan(
    string Status, string? DocumentType = null, string? Name = null, string? Number = null, DateOnly? Expiry = null, double? Confidence = null,
    string? Qr = null)
{
    public static readonly IdScan None = new(ScanStatuses.None);
    public static readonly IdScan Failed = new(ScanStatuses.Failed);
}

/// <summary>What the server's photo check found on a proof (<c>PhotoQuality</c>): <c>PhotoProblems</c> codes and the sharpness number.</summary>
public sealed record ProofCheck(IReadOnlyList<string> Problems, double Sharpness)
{
    public static readonly ProofCheck None = new([], 0);

    /// <summary>The codes as stored, comma-separated; null when nothing was found.</summary>
    public string? Stored => Problems.Count == 0 ? null : string.Join(',', Problems);

    public static IReadOnlyList<string>? Parse(string? stored) =>
        string.IsNullOrEmpty(stored) ? null : stored.Split(',', StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>What a worker sends for a badge, as the function read it from the request.</summary>
public sealed record VerificationSubmission(string Kind, bool Consent);

public enum SubmitOutcome
{
    Saved,

    /// <summary>The kind isn't one of <see cref="VerificationKinds"/>.</summary>
    Invalid,

    /// <summary>The consent box wasn't ticked.</summary>
    ConsentRequired,

    /// <summary>This badge is already waiting for the operator, or already approved.</summary>
    AlreadySent,
}

/// <summary>
/// The operator's decision as saved: the worker it was about and the file to delete now, or null when there was
/// nothing to decide. <paramref name="ValidUntilNeeded"/>: nothing was saved, a clearance needs its "valid until" date
/// (<see cref="ClearanceRules.IsValidUntilAllowed"/>) to be approved.
/// </summary>
public sealed record Decided(string Uid, string? DocumentBlob, bool ValidUntilNeeded = false);

/// <summary>
/// Worker verification (<c>WorkerVerifications</c>, <c>VerificationAudit</c>): a worker sends a proof with their
/// consent, an operator looks at it and decides, an approved one is a badge on the listing. Government IDs,
/// certificates and payslips are sensitive personal information (Data Privacy Act, RA 10173), so: the file lives in a
/// private container, every look and decision is logged, and when the operator decides the file's name and
/// everything read from it are cleared here (the caller deletes the file): only the result is kept.
/// </summary>
public sealed class VerificationStore(SideGigPhDb db, PhotoLinks photoLinks, ListingCache listings)
{
    public const int MaxRejectReason = 200;

    /// <summary>The most of a clearance's QR code kept for the operator.</summary>
    public const int MaxQr = 1000;

    /// <summary>The most requests the operator's queue returns at once.</summary>
    public const int MaxQueue = 50;

    /// <summary>Null when <paramref name="submission"/> can be saved, else why not (before the file is stored anywhere).</summary>
    public async Task<SubmitOutcome?> RefusalAsync(string uid, VerificationSubmission submission, CancellationToken cancellationToken)
    {
        if (!VerificationKinds.All.Contains(submission.Kind))
        {
            return SubmitOutcome.Invalid;
        }

        if (!submission.Consent)
        {
            return SubmitOutcome.ConsentRequired;
        }

        var today = JobPostRules.PhilippineToday(DateTimeOffset.UtcNow);

        // A clearance whose "valid until" has passed no longer counts: the worker sends a new one.
        return await db.WorkerVerifications.AnyAsync(
            v => v.Uid == uid && v.Kind == submission.Kind
                && v.Status != VerificationStatuses.Rejected
                && (v.Status == VerificationStatuses.Pending || v.ValidUntil == null || v.ValidUntil >= today), cancellationToken)
            ? SubmitOutcome.AlreadySent
            : null;
    }

    /// <summary>
    /// Saves the request with the stored file's name and what was read from it. Null when the same badge was sent
    /// at the same moment (a double tap): the caller deletes the file it just stored.
    /// </summary>
    public async Task<VerificationDto?> SubmitAsync(
        string uid, VerificationSubmission submission, string documentBlob, IdScan scan, ProofCheck check, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var row = new WorkerVerificationRow
        {
            Uid = uid,
            Kind = submission.Kind,
            DocumentBlob = documentBlob,
            ScanStatus = scan.Status,
            ScannedType = StoreText.Limit(scan.DocumentType, 60),
            ScannedName = StoreText.Limit(scan.Name, 120),
            ScannedNumber = StoreText.Limit(scan.Number, 60),
            ScannedExpiry = scan.Expiry,
            ScanConfidence = scan.Confidence,
            PhotoProblems = check.Stored,
            PhotoSharpness = check.Sharpness,
            ScannedQr = StoreText.Limit(scan.Qr, MaxQr),
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

    /// <summary>
    /// A retake: the worker's own request for this badge, still waiting for the operator, gets the new photo and what
    /// was read and checked on it (logged as "retaken"). Answers the request and the old file's name for the caller to
    /// delete, or null when there is no waiting request to replace (approved already, or decided meanwhile).
    /// Sent twice (a lost answer), the second one simply replaces the photo with the same photo.
    /// </summary>
    public async Task<(VerificationDto Request, string? OldBlob)?> RetakeAsync(
        string uid, VerificationSubmission submission, string documentBlob, IdScan scan, ProofCheck check, CancellationToken cancellationToken)
    {
        return await db.InTransactionAsync<(VerificationDto, string?)?>(async ct =>
        {
            var row = await db.WorkerVerifications.FirstOrDefaultAsync(
                v => v.Uid == uid && v.Kind == submission.Kind && v.Status == VerificationStatuses.Pending, ct);
            if (row is null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            var old = row.DocumentBlob;
            row.DocumentBlob = documentBlob;
            row.ScanStatus = scan.Status;
            row.ScannedType = StoreText.Limit(scan.DocumentType, 60);
            row.ScannedName = StoreText.Limit(scan.Name, 120);
            row.ScannedNumber = StoreText.Limit(scan.Number, 60);
            row.ScannedExpiry = scan.Expiry;
            row.ScanConfidence = scan.Confidence;
            row.ScannedQr = StoreText.Limit(scan.Qr, MaxQr);
            row.PhotoProblems = check.Stored;
            row.PhotoSharpness = check.Sharpness;
            row.ConsentAt = now;
            db.VerificationAudit.Add(Audit(row.Id, uid, "retaken", now));
            await db.SaveChangesAsync(ct);
            return (ToDto(row), old == documentBlob ? null : old);
        }, cancellationToken);
    }

    /// <summary>The caller's own requests of the badges still offered (<see cref="VerificationKinds.All"/>), newest first.</summary>
    public async Task<IReadOnlyList<VerificationDto>> MineAsync(string uid, CancellationToken cancellationToken) =>
        [.. (await db.WorkerVerifications.AsNoTracking()
            .Where(v => v.Uid == uid && VerificationKinds.All.Contains(v.Kind))
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
                v.Kind, v.ScanStatus, v.ScannedType, v.ScannedName, v.ScannedNumber, v.ScannedExpiry,
                v.DocumentBlob is not null, v.CreatedAt,
                ProofCheck.Parse(v.PhotoProblems), v.PhotoSharpness, v.ScanConfidence, v.ScannedQr);
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

    /// <summary>How long after a decision the same decision sent again answers as decided instead of "not found".</summary>
    public static readonly TimeSpan RepeatWindow = TimeSpan.FromMinutes(2);

    /// <summary>
    /// The operator approves or rejects a waiting request. The file's name and everything read from it are cleared in
    /// the same transaction (logged as "deleted"); the caller deletes the file named in the answer. Null when the
    /// request doesn't exist or was already decided. Approving a clearance keeps the "valid until" the operator typed
    /// (<see cref="ClearanceRules"/>); without an allowed one nothing is saved (<see cref="Decided.ValidUntilNeeded"/>).
    /// </summary>
    public async Task<Decided?> DecideAsync(long id, string adminUid, VerificationDecisionRequest decision, CancellationToken cancellationToken)
    {
        var decided = await db.InTransactionAsync(async ct =>
        {
            var row = await db.WorkerVerifications.FirstOrDefaultAsync(v => v.Id == id && v.Status == VerificationStatuses.Pending, ct);
            var now = DateTimeOffset.UtcNow;
            if (row is null)
            {
                // The phone sends a write once more when its answer was lost on the way (ApiClient.WriteTries): the
                // same operator's same decision a moment ago answers as decided, with nothing left to delete.
                var since = now - RepeatWindow;
                var status = decision.Approve ? VerificationStatuses.Approved : VerificationStatuses.Rejected;
                return await db.WorkerVerifications.AsNoTracking()
                    .Where(v => v.Id == id && v.Status == status && v.DecidedBy == adminUid && v.DecidedAt >= since)
                    .Select(v => new Decided(v.Uid, null))
                    .FirstOrDefaultAsync(ct);
            }

            var isClearance = row.Kind == VerificationKinds.Clearance;
            if (decision.Approve && isClearance
                && (decision.ValidUntil is not { } until || !ClearanceRules.IsValidUntilAllowed(until, JobPostRules.PhilippineToday(now))))
            {
                return new Decided(row.Uid, null, ValidUntilNeeded: true);
            }

            var blob = row.DocumentBlob;
            row.ValidUntil = decision.Approve && isClearance ? decision.ValidUntil : null;
            row.Status = decision.Approve ? VerificationStatuses.Approved : VerificationStatuses.Rejected;
            row.RejectReason = decision.Approve ? null : StoreText.Limit(decision.Reason, MaxRejectReason);
            row.RejectCode = !decision.Approve && RejectCodes.All.Contains(decision.RejectCode ?? string.Empty) ? decision.RejectCode : null;
            row.DecidedAt = now;
            row.DecidedBy = adminUid;
            row.DocumentBlob = null;
            row.ScannedType = null;
            row.ScannedName = null;
            row.ScannedNumber = null;
            row.ScannedExpiry = null;
            row.ScanConfidence = null;
            row.ScannedQr = null;
            db.VerificationAudit.Add(Audit(id, adminUid, decision.Approve ? "approved" : "rejected", now));
            if (blob is not null)
            {
                db.VerificationAudit.Add(Audit(id, adminUid, "deleted", now));
            }

            await db.SaveChangesAsync(ct);
            return new Decided(row.Uid, blob);
        }, cancellationToken);

        if (decided is { ValidUntilNeeded: false } && decision.Approve)
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
        new(v.Id, v.Kind, v.Status, v.CreatedAt, v.DecidedAt, v.RejectReason,
            v.Status == VerificationStatuses.Pending ? ProofCheck.Parse(v.PhotoProblems) : null, v.ValidUntil);
}
