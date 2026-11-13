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

    /// <summary>
    /// The caller's review of a Skilled Worker they chatted with, at least <see cref="ReviewRules.WaitAfterChatStarts"/>
    /// after the chat started (an earlier review can always be changed). The first write's time is kept.
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
        if (chatStartedAt is not { } startedAt)
        {
            return ReviewOutcome.NoChat;
        }

        var now = DateTimeOffset.UtcNow;
        var row = await db.Reviews.FirstOrDefaultAsync(r => r.ProfessionalId == request.ProfessionalId && r.SeekerId == seekerUid, cancellationToken);
        if (row is null)
        {
            if (!ReviewRules.CanRate(startedAt, now))
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
        listings.Clear(); // search results carry the stars
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

    private static ReviewDto ToDto(ReviewRow r) =>
        new(r.ProfessionalId, r.SeekerId, r.ReviewerName, r.Rating, r.Comment, r.ServiceId, r.CreatedAt, r.UpdatedAt);
}
