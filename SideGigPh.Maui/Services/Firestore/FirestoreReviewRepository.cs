using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Firestore;

/// <summary>
/// <see cref="IReviewRepository"/> on <c>reviews/{proId}_{seekerUid}</c>: one review per Seeker per pro,
/// so writing again replaces it. The rules allow a review only after a chat between the two exists, and
/// never let the pro edit or delete the reviews about them.
/// </summary>
public sealed class FirestoreReviewRepository(ILogger<FirestoreReviewRepository> logger) : IReviewRepository
{
    public Task<IReadOnlyList<Review>> GetForProfessionalAsync(string professionalId) => FirestoreAccess.ReadAsync<IReadOnlyList<Review>>(async () =>
    {
        var snapshot = await FirestoreAccess.Db.GetCollection(FirestoreAccess.Reviews)
            .WhereEqualsTo("professionalId", professionalId)
            .OrderBy("createdAt", descending: true)
            .GetDocumentsAsync<ReviewDoc>();
        return snapshot.Documents.Select(d => ToReview(d.Data)).ToList();
    }, [], logger, $"the reviews of {professionalId}");

    public Task<IReadOnlyList<Review>> GetBySeekerAsync(string seekerId) => FirestoreAccess.ReadAsync<IReadOnlyList<Review>>(async () =>
    {
        var snapshot = await FirestoreAccess.Db.GetCollection(FirestoreAccess.Reviews)
            .WhereEqualsTo("seekerUid", seekerId)
            .OrderBy("createdAt", descending: true)
            .GetDocumentsAsync<ReviewDoc>();
        return snapshot.Documents.Select(d => ToReview(d.Data)).ToList();
    }, [], logger, $"the reviews written by {seekerId}");

    /// <summary>
    /// The stars of many pros at once, for ranking. Reads each pro's reviews (30 pros per query) and averages
    /// them here; a Cloud Function that keeps the average on the listing replaces this when volumes grow.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, ReviewSummary>> GetSummariesAsync(IReadOnlyCollection<string> professionalIds)
    {
        var ratings = new Dictionary<string, List<long>>();
        foreach (var batch in professionalIds.Distinct().Chunk(FirestoreAccess.InQueryLimit))
        {
            var reviews = await FirestoreAccess.ReadAsync(async () =>
            {
                var snapshot = await FirestoreAccess.Db.GetCollection(FirestoreAccess.Reviews)
                    .WhereFieldIn("professionalId", batch.Cast<object>().ToArray())
                    .GetDocumentsAsync<ReviewDoc>();
                return snapshot.Documents.Select(d => d.Data).ToList();
            }, [], logger, "review summaries");

            foreach (var review in reviews)
            {
                if (!ratings.TryGetValue(review.ProfessionalId, out var list))
                {
                    ratings[review.ProfessionalId] = list = [];
                }

                list.Add(review.Rating);
            }
        }

        return ratings.ToDictionary(pair => pair.Key, pair => new ReviewSummary(pair.Value.Average(), pair.Value.Count));
    }

    public async Task SaveAsync(Review review)
    {
        try
        {
            // The doc id is what the rules check: {professionalId}_{seekerUid}.
            var document = FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Reviews}/{Chat.IdFor(review.ProfessionalId, review.SeekerId)}");

            // createdAt can't change once written, so a replacement carries the one that's already there.
            var existing = (await document.GetDocumentSnapshotAsync<ReviewDoc>()).Data;
            await document.SetDataAsync(new ReviewDoc
            {
                ProfessionalId = review.ProfessionalId,
                SeekerUid = review.SeekerId,
                ReviewerName = FirestoreAccess.Name(review.ReviewerName),
                Rating = review.Rating,
                Comment = FirestoreAccess.Limit(review.Comment, 1000),
                ServiceId = review.ServiceId,
                CreatedAt = existing?.CreatedAt ?? review.At,
                UpdatedAt = review.At,
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't save the review of {ProfessionalId} by {SeekerId}.", review.ProfessionalId, review.SeekerId);
            throw;
        }
    }

    /// <summary>
    /// Deletes the reviews this person wrote. The ones about them stay: the rules don't let a pro delete
    /// reviews, or they could wipe the bad ones. They stay hidden while the listing is gone.
    /// </summary>
    public async Task DeleteForPersonAsync(string personId)
    {
        try
        {
            var snapshot = await FirestoreAccess.Db.GetCollection(FirestoreAccess.Reviews)
                .WhereEqualsTo("seekerUid", personId)
                .GetDocumentsAsync<ReviewDoc>();
            foreach (var batch in snapshot.Documents.Chunk(FirestoreAccess.BatchSize))
            {
                var writes = FirestoreAccess.Db.CreateBatch();
                foreach (var document in batch)
                {
                    writes.DeleteDocument(document.Reference);
                }

                await writes.CommitAsync();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't delete the reviews written by {PersonId}.", personId);
            throw;
        }
    }

    private static Review ToReview(ReviewDoc doc) => new(
        doc.Id ?? Chat.IdFor(doc.ProfessionalId, doc.SeekerUid),
        doc.ProfessionalId,
        doc.SeekerUid,
        doc.ReviewerName,
        (int)doc.Rating,
        doc.Comment,
        doc.ServiceId,
        doc.UpdatedAt);
}
