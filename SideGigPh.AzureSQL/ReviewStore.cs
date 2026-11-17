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
        var mineHidden = rows.Any(r => r.SeekerId == viewerUid)
            && !await BlindReviewFilter.Shown(db, about.Where(r => r.SeekerId == viewerUid), now).AnyAsync(cancellationToken);
        var verified = (await CompletedJobs().Where(job => job.ProUid == professionalId).Select(job => job.SeekerUid).ToListAsync(cancellationToken)).ToHashSet();
        return [.. rows.Select(r => ToDto(r, verified.Contains(r.SeekerId), mineHidden && r.SeekerId == viewerUid))];
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
        return [.. rows.Select(r => ToDto(r, verified.Contains(r.ProfessionalId), !shownIds.Contains(r.ProfessionalId)))];
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
    /// "Verified job"). An earlier review can always be changed. The first write's time is kept.
    /// </summary>
    public async Task<ReviewOutcome> SaveAsync(string seekerUid, SaveReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.ProfessionalId) || request.ProfessionalId == seekerUid
            || (request.ServiceId is not null && !StoreText.IsCode(request.ServiceId)))
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
    /// Deletes the reviews this person wrote. The ones about them stay, or a Skilled Worker could wipe their bad
    /// reviews by starting over. They show again only if the worker publishes again.
    /// </summary>
    public async Task DeleteWrittenByAsync(string seekerUid, CancellationToken cancellationToken)
    {
        await db.Reviews.Where(r => r.SeekerId == seekerUid).ExecuteDeleteAsync(cancellationToken);
        listings.Clear();
    }

    private static ReviewDto ToDto(ReviewRow r, bool verifiedJob, bool hidden) =>
        new(r.ProfessionalId, r.SeekerId, r.ReviewerName, r.Rating, r.Comment, r.ServiceId, r.CreatedAt, r.UpdatedAt, verifiedJob, hidden);
}
