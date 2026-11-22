using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

public enum ReviewOutcome
{
    Saved,

    /// <summary>Only a Seeker who chatted with the Skilled Worker can review them.</summary>
    NoChat,

    /// <summary>The chat started less than <see cref="ReviewRules.WaitAfterChatStarts"/> ago and there is no earlier review to change.</summary>
    TooSoon,

    /// <summary>The review is about a completed job and both sides' reviews show: it can't be changed (<see cref="ReviewRules.IsLocked"/>).</summary>
    Locked,

    Invalid,
}

/// <summary>Seekers' reviews of the Skilled Workers they chatted with: one per pair, writing again replaces it.</summary>
public sealed class ReviewStore(SideGigPhDb db, ListingCache listings)
{
    /// <summary>
    /// The reviews of a worker that <paramref name="viewerUid"/> may see, newest first: a blind review
    /// (<see cref="BlindReviewFilter"/>) only for its writer, marked <see cref="ReviewDto.Hidden"/>. One whose writer picked
    /// them for a completed job is a "Verified job".
    /// </summary>
    public async Task<IReadOnlyList<ReviewDto>> ForProfessionalAsync(string professionalId, string viewerUid, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var about = db.Reviews.AsNoTracking().Where(r => r.ProfessionalId == professionalId);
        var rows = await BlindReviewFilter.Shown(db, about, now, viewerUid)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        var mine = rows.Any(r => r.SeekerId == viewerUid);
        var mineHidden = mine && !await BlindReviewFilter.Shown(db, about.Where(r => r.SeekerId == viewerUid), now).AnyAsync(cancellationToken);
        var mineLocked = mine && await BlindReviewFilter.IsLockedAsync(db, viewerUid, professionalId, byHousehold: true, now, cancellationToken);
        var verified = (await CompletedJobs().Where(job => job.ProUid == professionalId).Select(job => job.SeekerUid).ToListAsync(cancellationToken)).ToHashSet();
        return [.. rows.Select(r => ToDto(r, verified.Contains(r.SeekerId), mineHidden && r.SeekerId == viewerUid, mineLocked && r.SeekerId == viewerUid))];
    }

    /// <summary>The reviews a Seeker wrote, newest first: all of them for the Seeker (hidden ones marked), only the shown ones for anyone else.</summary>
    public async Task<IReadOnlyList<ReviewDto>> BySeekerAsync(string seekerId, string viewerUid, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var written = db.Reviews.AsNoTracking().Where(r => r.SeekerId == seekerId);
        var shown = BlindReviewFilter.Shown(db, written, now);
        var rows = await (viewerUid == seekerId ? written : shown)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        var shownIds = viewerUid == seekerId
            ? (await shown.Select(r => r.ProfessionalId).ToListAsync(cancellationToken)).ToHashSet()
            : [.. rows.Select(r => r.ProfessionalId)];
        var verified = (await CompletedJobs().Where(job => job.SeekerUid == seekerId).Select(job => job.ProUid).ToListAsync(cancellationToken)).ToHashSet();
        var locked = viewerUid == seekerId ? await LockedAsync(seekerId, rows, now, cancellationToken) : [];
        return [.. rows.Select(r => ToDto(r, verified.Contains(r.ProfessionalId), !shownIds.Contains(r.ProfessionalId), locked.Contains(r.ProfessionalId)))];
    }

    /// <summary>The workers whose review by <paramref name="seekerUid"/> (among <paramref name="written"/>) is locked (<see cref="ReviewRules.IsLocked"/>).</summary>
    private async Task<HashSet<string>> LockedAsync(string seekerUid, IReadOnlyList<ReviewRow> written, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var jobs = await BlindReviewFilter.CompletedAtAsync(db, seekerUid, [.. written.Select(r => r.ProfessionalId)], cancellationToken);
        if (jobs.Count == 0)
        {
            return [];
        }

        var pros = jobs.Keys.ToList();
        var theirs = await db.HouseholdReviews.AsNoTracking()
            .Where(h => h.SeekerUid == seekerUid && pros.Contains(h.ProUid))
            .ToDictionaryAsync(h => h.ProUid, h => h.UpdatedAt, cancellationToken);
        return [.. written
            .Where(r => jobs.TryGetValue(r.ProfessionalId, out var done)
                        && ReviewRules.IsLocked(r.UpdatedAt, theirs.TryGetValue(r.ProfessionalId, out var at) ? at : null, done, now))
            .Select(r => r.ProfessionalId)];
    }

    /// <summary>The completed jobs (picked, both marked done) with the household that posted each.</summary>
    private IQueryable<CompletedJob> CompletedJobs() =>
        from o in db.JobOffers.AsNoTracking()
        join p in db.JobPosts.AsNoTracking() on o.PostId equals p.Id
        where o.Status == JobOfferStatuses.Picked && o.HouseholdDoneAt != null && o.WorkerDoneAt != null
        select new CompletedJob { SeekerUid = p.SeekerUid, ProUid = o.ProUid };

    /// <summary>One completed job's two sides (settable properties, so EF can filter on them).</summary>
    private sealed class CompletedJob
    {
        public required string SeekerUid { get; init; }
        public required string ProUid { get; init; }
    }

    /// <summary>
    /// The caller's review of a Skilled Worker they chatted with, at least <see cref="ReviewRules.WaitAfterChatStarts"/>
    /// after the chat started, or at once after a completed job with them (picked, both marked done: it then shows
    /// "Verified job"). An earlier review can be changed, unless it is locked (<see cref="ReviewRules.IsLocked"/>: about
    /// a completed job, and both sides' reviews show). The first write's time is kept.
    /// </summary>
    public async Task<ReviewOutcome> SaveAsync(string seekerUid, SaveReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.ProfessionalId) || request.ProfessionalId == seekerUid
            || (request.ServiceId is not null && !StoreText.IsCode(request.ServiceId))
            || BlockedWords.AnyIn(request.Comment, request.ReviewerName))
        {
            return ReviewOutcome.Invalid;
        }

        var chatStartedAt = await db.Chats
            .Where(c => c.ProfessionalId == request.ProfessionalId && c.SeekerId == seekerUid)
            .Select(c => (DateTimeOffset?)c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        // A completed job (picked, both marked done) is proof enough: no chat or wait needed.
        var completed = await PickedJobStore.Completed(db, seekerUid, request.ProfessionalId).AnyAsync(cancellationToken);
        if (chatStartedAt is null && !completed)
        {
            return ReviewOutcome.NoChat;
        }

        var now = DateTimeOffset.UtcNow;
        var row = await db.Reviews.FirstOrDefaultAsync(r => r.ProfessionalId == request.ProfessionalId && r.SeekerId == seekerUid, cancellationToken);
        if (row is not null && completed
            && await BlindReviewFilter.IsLockedAsync(db, seekerUid, request.ProfessionalId, byHousehold: true, now, cancellationToken))
        {
            // The phone sends a write once more when its answer was lost on the way (ApiClient.WriteTries): the
            // same review, which locked it, answers as saved.
            return row.Rating == request.Rating && row.Comment == StoreText.Limit(request.Comment, 1000)
                ? ReviewOutcome.Saved
                : ReviewOutcome.Locked;
        }

        if (row is null)
        {
            if (!completed && !ReviewRules.CanRate(chatStartedAt!.Value, now))
            {
                return ReviewOutcome.TooSoon;
            }

            row = new ReviewRow { ProfessionalId = request.ProfessionalId, SeekerId = seekerUid, ReviewerName = string.Empty, CreatedAt = now };
            db.Reviews.Add(row);
        }

        row.ReviewerName = StoreText.Name(request.ReviewerName, 60);
        row.Rating = (byte)request.Rating;
        row.Comment = StoreText.Limit(request.Comment, 1000);
        row.ServiceId = request.ServiceId;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        listings.Clear(); // search results carry the stars (and this review may open the worker's blind one)
        return ReviewOutcome.Saved;
    }

    /// <summary>
    /// Deletes the reviews this person wrote (Start over), except locked ones (<see cref="ReviewRules.IsLocked"/>: about
    /// a completed job, and both sides' reviews show), which nobody can take back after seeing the other's. The ones
    /// about them stay, or a Skilled Worker could wipe their bad reviews by starting over. Deleting the account still
    /// deletes everything (<c>UserStore.DeleteAccountAsync</c>).
    /// </summary>
    public async Task DeleteWrittenByAsync(string seekerUid, CancellationToken cancellationToken)
    {
        var written = await db.Reviews.AsNoTracking().Where(r => r.SeekerId == seekerUid).ToListAsync(cancellationToken);
        var locked = await LockedAsync(seekerUid, written, DateTimeOffset.UtcNow, cancellationToken);
        await db.Reviews.Where(r => r.SeekerId == seekerUid && !locked.Contains(r.ProfessionalId)).ExecuteDeleteAsync(cancellationToken);
        listings.Clear();
    }

    private static ReviewDto ToDto(ReviewRow r, bool verifiedJob, bool hidden, bool locked) =>
        new(r.ProfessionalId, r.SeekerId, r.ReviewerName, r.Rating, r.Comment, r.ServiceId, r.CreatedAt, r.UpdatedAt, verifiedJob, hidden, locked);
}
