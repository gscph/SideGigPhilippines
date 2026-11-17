using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>What became of a worker's review of a household (<see cref="PickedJobStore.SaveHouseholdReviewAsync"/>).</summary>
public enum HouseholdReviewOutcome
{
    Saved,

    /// <summary>No completed job (picked, both marked done) between the worker and that household.</summary>
    NoCompletedJob,

    Invalid,
}

/// <summary>
/// Picked jobs: the record behind verified reviews (brief item 12). A household's picked offer is a job between the
/// two; each side marks it done, in any order. Both marks make it a <b>completed job</b>: the household's review of
/// that worker shows "Verified job" (<see cref="ReviewStore"/>) and can be written at once, the worker can rate the
/// household (<c>HouseholdReviews</c>), and it counts on the worker's listing (<c>ListingDto.CompletedJobs</c>).
/// No money passes through the app: done is each side's own word.
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
                          select new { o.PostId, p.ServiceId, o.PricePesos, o.UpdatedAt, o.HouseholdDoneAt, o.WorkerDoneAt, Household = p.SeekerUid == uid })
            .Take(MaxBetween)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        var asHousehold = await db.Reviews.AsNoTracking().Where(r => r.SeekerId == uid && r.ProfessionalId == otherUid)
            .Select(r => (int?)r.Rating).FirstOrDefaultAsync(cancellationToken);
        var asWorker = await db.HouseholdReviews.AsNoTracking().Where(r => r.ProUid == uid && r.SeekerUid == otherUid)
            .Select(r => (int?)r.Rating).FirstOrDefaultAsync(cancellationToken);
        return [.. rows.Select(r => new PickedJobDto(
            r.PostId, r.ServiceId, r.PricePesos, r.UpdatedAt, r.Household,
            r.HouseholdDoneAt is not null, r.WorkerDoneAt is not null,
            r.Household ? asHousehold : asWorker))];
    }

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

        await db.SaveChangesAsync(cancellationToken);
        if (job.Offer.HouseholdDoneAt is not null && job.Offer.WorkerDoneAt is not null)
        {
            listings.Clear(); // search results carry the completed jobs count
        }

        var other = job.SeekerUid == uid ? job.Offer.ProUid : job.SeekerUid;
        return (await BetweenAsync(uid, other, cancellationToken)).FirstOrDefault(j => j.PostId == postId);
    }

    /// <summary>
    /// The caller, a worker, rates a household after a completed job between them (one review per pair, writing
    /// again replaces it; the first write's time is kept).
    /// </summary>
    public async Task<HouseholdReviewOutcome> SaveHouseholdReviewAsync(string proUid, SaveHouseholdReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.SeekerUid) || request.SeekerUid == proUid
            || (request.Comment?.Trim().Length ?? 0) > 1000)
        {
            return HouseholdReviewOutcome.Invalid;
        }

        if (!await Completed(db, request.SeekerUid, proUid).AnyAsync(cancellationToken))
        {
            return HouseholdReviewOutcome.NoCompletedJob;
        }

        var now = DateTimeOffset.UtcNow;
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
        return HouseholdReviewOutcome.Saved;
    }

    /// <summary>The households' stars from workers, for the posts in a feed (a household with none is left out).</summary>
    public static async Task<Dictionary<string, ReviewSummaryDto>> HouseholdStarsAsync(SideGigPhDb db, IReadOnlyCollection<string> seekerUids, CancellationToken cancellationToken) =>
        seekerUids.Count == 0
            ? []
            : await db.HouseholdReviews.AsNoTracking()
                .Where(r => seekerUids.Contains(r.SeekerUid))
                .GroupBy(r => r.SeekerUid)
                .Select(g => new { g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => new ReviewSummaryDto(g.Average, g.Count), cancellationToken);

    /// <summary>The completed jobs (picked, both marked done) where <paramref name="seekerUid"/> picked <paramref name="proUid"/>.</summary>
    internal static IQueryable<JobOfferRow> Completed(SideGigPhDb db, string seekerUid, string proUid) =>
        db.JobOffers.Where(o => o.ProUid == proUid && o.Status == JobOfferStatuses.Picked
                                && o.HouseholdDoneAt != null && o.WorkerDoneAt != null
                                && db.JobPosts.Any(p => p.Id == o.PostId && p.SeekerUid == seekerUid));
}
