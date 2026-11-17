using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// Blind two-way reviews as SQL filters (<see cref="ReviewRules.IsHidden"/> is the same rule in C#). After a completed
/// job (picked, both marked done), a review either side wrote since stays hidden until the other side has reviewed
/// since that job too, or until <see cref="ReviewRules.BlindFor"/> after it. Everything that shows reviews to others
/// (a worker's reviews, their stars and count on the listing, so search ranking, a household's stars on its posts)
/// reads through these; only the writer sees their own hidden review. Computed from the existing times: no column.
/// </summary>
internal static class BlindReviewFilter
{
    /// <summary>The Seekers' reviews of workers that show now; <paramref name="writerUid"/>'s own always do.</summary>
    public static IQueryable<ReviewRow> Shown(SideGigPhDb db, IQueryable<ReviewRow> reviews, DateTimeOffset now, string? writerUid = null)
    {
        var since = now - ReviewRules.BlindFor;
        var writer = writerUid ?? string.Empty; // no uid is empty
        return reviews.Where(r => r.SeekerId == writer || !db.JobOffers.Any(o =>
            o.ProUid == r.ProfessionalId && o.Status == JobOfferStatuses.Picked
            && o.HouseholdDoneAt != null && o.WorkerDoneAt != null
            && (o.HouseholdDoneAt > since || o.WorkerDoneAt > since)
            && r.UpdatedAt >= o.HouseholdDoneAt && r.UpdatedAt >= o.WorkerDoneAt
            && db.JobPosts.Any(p => p.Id == o.PostId && p.SeekerUid == r.SeekerId)
            && !db.HouseholdReviews.Any(h => h.ProUid == r.ProfessionalId && h.SeekerUid == r.SeekerId
                                             && h.UpdatedAt >= o.HouseholdDoneAt && h.UpdatedAt >= o.WorkerDoneAt)));
    }

    /// <summary>
    /// When each completed job (picked, both marked done) between <paramref name="seekerUid"/> as the household and
    /// each of <paramref name="proUids"/> as the worker was completed (its later "done" mark), by worker.
    /// </summary>
    public static async Task<Dictionary<string, List<DateTimeOffset>>> CompletedAtAsync(SideGigPhDb db, string seekerUid,
        IReadOnlyCollection<string> proUids, CancellationToken cancellationToken)
    {
        if (proUids.Count == 0)
        {
            return [];
        }

        var marks = await (from o in db.JobOffers.AsNoTracking()
                           join p in db.JobPosts.AsNoTracking() on o.PostId equals p.Id
                           where p.SeekerUid == seekerUid && proUids.Contains(o.ProUid) && o.Status == JobOfferStatuses.Picked
                                 && o.HouseholdDoneAt != null && o.WorkerDoneAt != null
                           select new { o.ProUid, Household = o.HouseholdDoneAt!.Value, Worker = o.WorkerDoneAt!.Value })
            .ToListAsync(cancellationToken);
        return marks.GroupBy(m => m.ProUid)
            .ToDictionary(g => g.Key, g => g.Select(m => m.Household > m.Worker ? m.Household : m.Worker).ToList());
    }

    /// <summary>
    /// Whether the review of one side about the other is locked (<see cref="ReviewRules.IsLocked"/>): read from both
    /// reviews of the pair and its completed jobs. <paramref name="byHousehold"/>: the household's review of the worker
    /// (else the worker's rating of the household).
    /// </summary>
    public static async Task<bool> IsLockedAsync(SideGigPhDb db, string seekerUid, string proUid, bool byHousehold, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var jobs = (await CompletedAtAsync(db, seekerUid, [proUid], cancellationToken)).GetValueOrDefault(proUid);
        if (jobs is null)
        {
            return false;
        }

        var ofWorker = await db.Reviews.AsNoTracking().Where(r => r.SeekerId == seekerUid && r.ProfessionalId == proUid)
            .Select(r => (DateTimeOffset?)r.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
        var ofHousehold = await db.HouseholdReviews.AsNoTracking().Where(r => r.SeekerUid == seekerUid && r.ProUid == proUid)
            .Select(r => (DateTimeOffset?)r.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
        var (mine, theirs) = byHousehold ? (ofWorker, ofHousehold) : (ofHousehold, ofWorker);
        return mine is { } written && ReviewRules.IsLocked(written, theirs, jobs, now);
    }

    /// <summary>The workers' reviews of households that show now.</summary>
    public static IQueryable<HouseholdReviewRow> Shown(SideGigPhDb db, IQueryable<HouseholdReviewRow> reviews, DateTimeOffset now)
    {
        var since = now - ReviewRules.BlindFor;
        return reviews.Where(h => !db.JobOffers.Any(o =>
            o.ProUid == h.ProUid && o.Status == JobOfferStatuses.Picked
            && o.HouseholdDoneAt != null && o.WorkerDoneAt != null
            && (o.HouseholdDoneAt > since || o.WorkerDoneAt > since)
            && h.UpdatedAt >= o.HouseholdDoneAt && h.UpdatedAt >= o.WorkerDoneAt
            && db.JobPosts.Any(p => p.Id == o.PostId && p.SeekerUid == h.SeekerUid)
            && !db.Reviews.Any(r => r.ProfessionalId == h.ProUid && r.SeekerId == h.SeekerUid
                                    && r.UpdatedAt >= o.HouseholdDoneAt && r.UpdatedAt >= o.WorkerDoneAt)));
    }
}
