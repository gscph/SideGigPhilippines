using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

public enum ReviewOutcome
{
    Saved,

    /// <summary>Only a Seeker who chatted with the Skilled Worker can review them.</summary>
    NoChat,

    Invalid,
}

/// <summary>Seekers' reviews of the Skilled Workers they chatted with: one per pair, writing again replaces it.</summary>
public sealed class ReviewStore(SideGigPhDb db)
{
    /// <summary>Most pros one summaries request may ask about (a page of search results is far fewer).</summary>
    public const int MaxSummaries = 500;

    public async Task<IReadOnlyList<ReviewDto>> ForProfessionalAsync(string professionalId, CancellationToken cancellationToken) =>
        await db.Reviews.AsNoTracking()
            .Where(r => r.ProfessionalId == professionalId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReviewDto>> BySeekerAsync(string seekerId, CancellationToken cancellationToken) =>
        await db.Reviews.AsNoTracking()
            .Where(r => r.SeekerId == seekerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync(cancellationToken);

    /// <summary>Average stars and count per pro, for ranking. Pros with no reviews are left out.</summary>
    public async Task<IReadOnlyList<ReviewSummaryDto>> SummariesAsync(IReadOnlyCollection<string> professionalIds, CancellationToken cancellationToken)
    {
        var ids = professionalIds.Distinct().Take(MaxSummaries).ToList();
        return await db.Reviews.AsNoTracking()
            .Where(r => ids.Contains(r.ProfessionalId))
            .GroupBy(r => r.ProfessionalId)
            .Select(g => new ReviewSummaryDto(g.Key, g.Average(r => (double)r.Rating), g.Count()))
            .ToListAsync(cancellationToken);
    }

    /// <summary>The caller's review of a Skilled Worker they chatted with. The first write's time is kept.</summary>
    public async Task<ReviewOutcome> SaveAsync(string seekerUid, SaveReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.ProfessionalId) || request.ProfessionalId == seekerUid
            || (request.ServiceId is not null && !StoreText.IsCode(request.ServiceId)))
        {
            return ReviewOutcome.Invalid;
        }

        if (!await db.Chats.AnyAsync(c => c.ProfessionalId == request.ProfessionalId && c.SeekerId == seekerUid, cancellationToken))
        {
            return ReviewOutcome.NoChat;
        }

        var now = DateTimeOffset.UtcNow;
        var row = await db.Reviews.FirstOrDefaultAsync(r => r.ProfessionalId == request.ProfessionalId && r.SeekerId == seekerUid, cancellationToken);
        if (row is null)
        {
            row = new ReviewRow { ProfessionalId = request.ProfessionalId, SeekerId = seekerUid, ReviewerName = string.Empty, CreatedAt = now };
            db.Reviews.Add(row);
        }

        row.ReviewerName = StoreText.Name(request.ReviewerName, 60);
        row.Rating = (byte)request.Rating;
        row.Comment = StoreText.Limit(request.Comment, 1000);
        row.ServiceId = request.ServiceId;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ReviewOutcome.Saved;
    }

    /// <summary>
    /// Deletes the reviews this person wrote. The ones about them stay, or a Skilled Worker could wipe their bad
    /// reviews by starting over. They show again only if the worker publishes again.
    /// </summary>
    public Task DeleteWrittenByAsync(string seekerUid, CancellationToken cancellationToken) =>
        db.Reviews.Where(r => r.SeekerId == seekerUid).ExecuteDeleteAsync(cancellationToken);

    private static ReviewDto ToDto(ReviewRow r) =>
        new(r.ProfessionalId, r.SeekerId, r.ReviewerName, r.Rating, r.Comment, r.ServiceId, r.CreatedAt, r.UpdatedAt);
}
