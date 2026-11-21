using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>What became of a worker's review of a household (<see cref="PickedJobStore.SaveHouseholdReviewAsync"/>).</summary>
public enum HouseholdReviewOutcome
{
    Saved,

    /// <summary>No completed job (picked, both marked done) between the worker and that household.</summary>
    NoCompletedJob,

    /// <summary>Both sides' reviews of the latest completed job show: the rating can't be changed (<see cref="ReviewRules.IsLocked"/>).</summary>
    Locked,

    Invalid,
}

/// <summary>What became of sending the picked worker the household's location (<see cref="PickedJobStore.ShareLocationAsync"/>).</summary>
public enum ShareLocationOutcome
{
    Shared,

    /// <summary>Nothing picked on that post with the caller as its household (a worker can't send one), or a block either way.</summary>
    NotFound,

    /// <summary>Both sides already marked it done: nobody needs to find the house any more.</summary>
    Completed,

    /// <summary>Not a point in the Philippines.</summary>
    Invalid,
}

/// <summary>What became of calling a picked job off (<see cref="PickedJobStore.CancelAsync"/>).</summary>
public enum CancelPickOutcome
{
    Cancelled,

    /// <summary>Nothing picked on that post with the caller as its household or worker.</summary>
    NotFound,

    /// <summary>Both sides already marked it done: a completed job stays.</summary>
    Completed,

    Invalid,
}

/// <summary>
/// Picked jobs: the record behind verified reviews (brief item 12). A household's picked offer is a job between the
/// two; each side marks it done, in any order. Both marks make it a <b>completed job</b>: the household's review of
/// that worker shows "Verified job" (<see cref="ReviewStore"/>) and can be written at once, the worker can rate the
/// household (<c>HouseholdReviews</c>), and it counts on the worker's listing (<c>ListingDto.CompletedJobs</c>).
/// Both reviews are blind (<see cref="BlindReviewFilter"/>): hidden until both sides reviewed or
/// <see cref="ReviewRules.BlindFor"/> passed. No money passes through the app: done is each side's own word.
/// </summary>
public sealed class PickedJobStore(SideGigPhDb db, ListingCache listings)
{
    /// <summary>The most picked jobs listed between two people (newest first).</summary>
    public const int MaxBetween = 20;

    /// <summary>The picked jobs between the caller and <paramref name="otherUid"/>, either way round, newest first.</summary>
    public async Task<List<PickedJobDto>> BetweenAsync(string uid, string otherUid, CancellationToken cancellationToken)
    {
        if (uid == otherUid)
        {
            return [];
        }

        var rows = await (from o in db.JobOffers.AsNoTracking()
                          join p in db.JobPosts.AsNoTracking() on o.PostId equals p.Id
                          where o.Status == JobOfferStatuses.Picked
                                && ((p.SeekerUid == uid && o.ProUid == otherUid) || (p.SeekerUid == otherUid && o.ProUid == uid))
                          orderby o.UpdatedAt descending, o.Id descending
                          select new
                          {
                              o.PostId, p.ServiceId, o.PricePesos, o.UpdatedAt, o.HouseholdDoneAt, o.WorkerDoneAt, Household = p.SeekerUid == uid,
                              o.MeetLatitude, o.MeetLongitude, o.MeetSharedAt,
                          })
            .Take(MaxBetween)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        // A block either way hides the household's exact point from the other side, as it hides the phone number.
        var blocked = await db.Blocks.AnyAsync(b => (b.BlockerUid == uid && b.BlockedUid == otherUid)
                                                    || (b.BlockerUid == otherUid && b.BlockedUid == uid), cancellationToken);

        // The reviews between the two either way round (household → worker, worker → household), for the blind-review rule.
        var ofWorkers = await db.Reviews.AsNoTracking()
            .Where(r => (r.SeekerId == uid && r.ProfessionalId == otherUid) || (r.SeekerId == otherUid && r.ProfessionalId == uid))
            .Select(r => new Written(r.SeekerId, (int)r.Rating, r.UpdatedAt)).ToListAsync(cancellationToken);
        var ofHouseholds = await db.HouseholdReviews.AsNoTracking()
            .Where(r => (r.SeekerUid == uid && r.ProUid == otherUid) || (r.SeekerUid == otherUid && r.ProUid == uid))
            .Select(r => new Written(r.SeekerUid, (int)r.Rating, r.UpdatedAt)).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        return [.. rows.Select(r =>
        {
            var householdUid = r.Household ? uid : otherUid;
            var ofWorker = ofWorkers.FirstOrDefault(w => w.HouseholdUid == householdUid);
            var ofHousehold = ofHouseholds.FirstOrDefault(w => w.HouseholdUid == householdUid);
            var (mine, theirs) = r.Household ? (ofWorker, ofHousehold) : (ofHousehold, ofWorker);
            var completed = rows.Where(j => j.Household == r.Household && j.HouseholdDoneAt is not null && j.WorkerDoneAt is not null)
                .Select(j => Later(j.HouseholdDoneAt!.Value, j.WorkerDoneAt!.Value));
            DateTimeOffset? completedAt = r.HouseholdDoneAt is { } h && r.WorkerDoneAt is { } w ? Later(h, w) : null;
            return new PickedJobDto(
                r.PostId, r.ServiceId, r.PricePesos, r.UpdatedAt, r.Household,
                r.HouseholdDoneAt is not null, r.WorkerDoneAt is not null,
                mine?.Rating,
                completedAt,
                TheyReviewed: theirs is not null && completedAt is { } done && theirs.At >= done,
                TheirRating: theirs is not null && !ReviewRules.IsHidden(theirs.At, mine?.At, completed, now) ? theirs.Rating : null,
                MyReviewLocked: mine is not null && ReviewRules.IsLocked(mine.At, theirs?.At, completed, now),
                // The household's exact point, only while the job is open (it is cleared once completed anyway) and
                // neither side has blocked the other.
                MeetLatitude: completedAt is null && !blocked ? r.MeetLatitude : null,
                MeetLongitude: completedAt is null && !blocked ? r.MeetLongitude : null,
                MeetSharedAt: completedAt is null && !blocked && r.MeetLatitude is not null ? r.MeetSharedAt : null);
        })];
    }

    /// <summary>One review between the two: the household it is about or by, its stars and when it was last written.</summary>
    private sealed record Written(string HouseholdUid, int Rating, DateTimeOffset At);

    private static DateTimeOffset Later(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    /// <summary>
    /// The caller marks the picked job on <paramref name="postId"/> done: as the household that posted it, or as the
    /// picked worker. Marking twice changes nothing. Answers with the job, or null when the caller is neither or
    /// nothing was picked there.
    /// </summary>
    public async Task<PickedJobDto?> MarkDoneAsync(string uid, long postId, CancellationToken cancellationToken)
    {
        var job = await (from o in db.JobOffers
                         join p in db.JobPosts on o.PostId equals p.Id
                         where o.PostId == postId && o.Status == JobOfferStatuses.Picked && (p.SeekerUid == uid || o.ProUid == uid)
                         select new { Offer = o, p.SeekerUid })
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (job.SeekerUid == uid)
        {
            job.Offer.HouseholdDoneAt ??= now;
        }
        else
        {
            job.Offer.WorkerDoneAt ??= now;
        }

        if (job.Offer.HouseholdDoneAt is not null && job.Offer.WorkerDoneAt is not null)
        {
            ForgetMeetLocation(job.Offer); // the worker found the house: the exact point isn't kept
        }

        await db.SaveChangesAsync(cancellationToken);
        if (job.Offer.HouseholdDoneAt is not null && job.Offer.WorkerDoneAt is not null)
        {
            listings.Clear(); // search results carry the completed jobs count
        }

        var other = job.SeekerUid == uid ? job.Offer.ProUid : job.SeekerUid;
        return (await BetweenAsync(uid, other, cancellationToken)).FirstOrDefault(j => j.PostId == postId);
    }

    /// <summary>
    /// The caller (the household that posted it, or the picked worker) calls off the picked job on
    /// <paramref name="postId"/> before both marked it done: the offer becomes <c>cancelled</c> with who, when and why
    /// (any one-sided done mark is dropped), and the post opens again so its other offers can be picked (a hidden post
    /// stays hidden). The numbers stop showing (<see cref="JobPostStore.ContactNumberAsync"/> needs a pick); the chat stays.
    /// </summary>
    public async Task<CancelPickOutcome> CancelAsync(string uid, long postId, CancelPickRequest request, CancellationToken cancellationToken)
    {
        if (!PickCancelReasons.IsKnown(request.Reason) || !PickCancelRules.IsNote(request.Note))
        {
            return CancelPickOutcome.Invalid;
        }

        return await db.InTransactionAsync(async ct =>
        {
            var job = await (from o in db.JobOffers
                             join p in db.JobPosts on o.PostId equals p.Id
                             where o.PostId == postId && o.Status == JobOfferStatuses.Picked && (p.SeekerUid == uid || o.ProUid == uid)
                             select new { Offer = o, Post = p })
                .FirstOrDefaultAsync(ct);
            if (job is null)
            {
                return CancelPickOutcome.NotFound;
            }

            if (job.Offer.HouseholdDoneAt is not null && job.Offer.WorkerDoneAt is not null)
            {
                return CancelPickOutcome.Completed;
            }

            var now = DateTimeOffset.UtcNow;
            job.Offer.Status = JobOfferStatuses.Cancelled;
            job.Offer.CancelledAt = now;
            job.Offer.CancelledBy = job.Post.SeekerUid == uid ? PickCancelSides.Household : PickCancelSides.Worker;
            job.Offer.CancelReason = request.Reason;
            job.Offer.CancelNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            job.Offer.HouseholdDoneAt = null;
            job.Offer.WorkerDoneAt = null;
            ForgetMeetLocation(job.Offer);
            job.Offer.UpdatedAt = now;
            if (job.Post.Status == JobPostStatuses.Closed)
            {
                job.Post.Status = JobPostStatuses.Open;
                job.Post.ClosedAt = null;
            }

            await db.SaveChangesAsync(ct);
            return CancelPickOutcome.Cancelled;
        }, cancellationToken);
    }

    /// <summary>
    /// The caller, the household that posted <paramref name="postId"/>, sends the worker it picked the exact place to
    /// come to (sending again replaces it). Only those two ever get it (<see cref="BetweenAsync"/>), and it is cleared
    /// when the job is called off or both mark it done. Answers with the job as the caller now sees it.
    /// </summary>
    public async Task<(ShareLocationOutcome Outcome, PickedJobDto? Job)> ShareLocationAsync(string uid, long postId, ShareMeetLocationRequest request, CancellationToken cancellationToken)
    {
        if (GeoRules.Point(request.Latitude, request.Longitude) is not { } point)
        {
            return (ShareLocationOutcome.Invalid, null);
        }

        var offer = await (from o in db.JobOffers
                           join p in db.JobPosts on o.PostId equals p.Id
                           where o.PostId == postId && o.Status == JobOfferStatuses.Picked && p.SeekerUid == uid
                           select o)
            .FirstOrDefaultAsync(cancellationToken);
        if (offer is null || await db.Blocks.AnyAsync(b => (b.BlockerUid == uid && b.BlockedUid == offer.ProUid)
                                                           || (b.BlockerUid == offer.ProUid && b.BlockedUid == uid), cancellationToken))
        {
            return (ShareLocationOutcome.NotFound, null);
        }

        if (offer.HouseholdDoneAt is not null && offer.WorkerDoneAt is not null)
        {
            return (ShareLocationOutcome.Completed, null);
        }

        offer.MeetLatitude = point.Latitude;
        offer.MeetLongitude = point.Longitude;
        offer.MeetSharedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (ShareLocationOutcome.Shared, (await BetweenAsync(uid, offer.ProUid, cancellationToken)).FirstOrDefault(j => j.PostId == postId));
    }

    private static void ForgetMeetLocation(JobOfferRow offer)
    {
        offer.MeetLatitude = null;
        offer.MeetLongitude = null;
        offer.MeetSharedAt = null;
    }

    /// <summary>
    /// The caller, a worker, rates a household after a completed job between them (one review per pair, writing
    /// again replaces it, unless it is locked: <see cref="ReviewRules.IsLocked"/>; the first write's time is kept).
    /// </summary>
    public async Task<HouseholdReviewOutcome> SaveHouseholdReviewAsync(string proUid, SaveHouseholdReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.SeekerUid) || request.SeekerUid == proUid
            || (request.Comment?.Trim().Length ?? 0) > 1000 || BlockedWords.Contains(request.Comment))
        {
            return HouseholdReviewOutcome.Invalid;
        }

        if (!await Completed(db, request.SeekerUid, proUid).AnyAsync(cancellationToken))
        {
            return HouseholdReviewOutcome.NoCompletedJob;
        }

        var now = DateTimeOffset.UtcNow;
        if (await BlindReviewFilter.IsLockedAsync(db, request.SeekerUid, proUid, byHousehold: false, now, cancellationToken))
        {
            return HouseholdReviewOutcome.Locked;
        }

        var row = await db.HouseholdReviews.FirstOrDefaultAsync(r => r.ProUid == proUid && r.SeekerUid == request.SeekerUid, cancellationToken);
        if (row is null)
        {
            row = new HouseholdReviewRow { ProUid = proUid, SeekerUid = request.SeekerUid, CreatedAt = now };
            db.HouseholdReviews.Add(row);
        }

        row.Rating = (byte)request.Rating;
        row.Comment = StoreText.Limit(request.Comment, 1000);
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        listings.Clear(); // this review may open the household's blind review, whose stars search results carry
        return HouseholdReviewOutcome.Saved;
    }

    /// <summary>
    /// The households' stars from workers, for the posts in a feed (a household with none is left out). Blind reviews
    /// (<see cref="BlindReviewFilter"/>) count only once they open.
    /// </summary>
    public static async Task<Dictionary<string, ReviewSummaryDto>> HouseholdStarsAsync(SideGigPhDb db, IReadOnlyCollection<string> seekerUids, CancellationToken cancellationToken) =>
        seekerUids.Count == 0
            ? []
            : await BlindReviewFilter.Shown(db, db.HouseholdReviews.AsNoTracking().Where(r => seekerUids.Contains(r.SeekerUid)), DateTimeOffset.UtcNow)
                .GroupBy(r => r.SeekerUid)
                .Select(g => new { g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => new ReviewSummaryDto(g.Average, g.Count), cancellationToken);

    /// <summary>The completed jobs (picked, both marked done) where <paramref name="seekerUid"/> picked <paramref name="proUid"/>.</summary>
    internal static IQueryable<JobOfferRow> Completed(SideGigPhDb db, string seekerUid, string proUid) =>
        db.JobOffers.Where(o => o.ProUid == proUid && o.Status == JobOfferStatuses.Picked
                                && o.HouseholdDoneAt != null && o.WorkerDoneAt != null
                                && db.JobPosts.Any(p => p.Id == o.PostId && p.SeekerUid == seekerUid));
}
