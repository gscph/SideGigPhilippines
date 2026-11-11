using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Dev;

/// <summary>
/// Stand-in until Firestore is wired. Reviews Seekers write are kept in a file on this phone
/// (<c>dev-reviews.json</c>). When <see cref="DevSamples.Enabled"/>, the sample pros and the
/// pro's own listing also get SAMPLE reviews, so ranking by stars can be seen working.
/// </summary>
public sealed class StubReviewRepository(ILogger<StubReviewRepository> logger) : IReviewRepository
{
    // Stars per sample pro (ids from StubProfessionalProfileRepository); missing = no reviews.
    private static readonly IReadOnlyDictionary<string, int[]> SampleRatings = new Dictionary<string, int[]>
    {
        ["1"] = [5, 5, 4], ["2"] = [4, 3], ["4"] = [5, 5, 5, 4], ["5"] = [4], ["6"] = [5, 4],
        ["8"] = [5, 5], ["9"] = [3, 4], ["10"] = [5, 4, 5], ["11"] = [4], ["12"] = [5, 5, 4],
        ["13"] = [5], ["15"] = [5, 5, 5],
    };

    private static readonly string[] SampleNames = ["Maricel D.", "Joel R.", "Ana P.", "Rowena S.", "Mark T.", "Liza G.", "Ben C."];

    private static readonly string?[] SampleComments =
    [
        "Mabilis dumating at malinis gumawa. Salamat po!",
        "Ayos ang trabaho, sulit ang bayad.",
        null,
        "Mabait at maayos kausap.",
        "Medyo late dumating pero maganda ang gawa.",
        "Recommended! Babalikan ko ulit.",
    ];

    private readonly JsonFile<List<Review>> _file = new("dev-reviews.json", DevJsonContext.Default.ListReview, logger);
    private List<Review>? _reviews;

    public async Task<IReadOnlyList<Review>> GetForProfessionalAsync(string professionalId) =>
        (await AllAsync())
            .Where(r => r.ProfessionalId == professionalId)
            .Concat(Samples(professionalId))
            .OrderByDescending(r => r.At)
            .ToList();

    public async Task<IReadOnlyList<Review>> GetBySeekerAsync(string seekerId) =>
        (await AllAsync())
            .Where(r => r.SeekerId == seekerId)
            .OrderByDescending(r => r.At)
            .ToList();

    public async Task<IReadOnlyDictionary<string, ReviewSummary>> GetSummariesAsync(IReadOnlyCollection<string> professionalIds)
    {
        var summaries = new Dictionary<string, ReviewSummary>();
        foreach (var id in professionalIds.Distinct())
        {
            var reviews = await GetForProfessionalAsync(id);
            if (reviews.Count > 0)
            {
                summaries[id] = new ReviewSummary(reviews.Average(r => r.Rating), reviews.Count);
            }
        }

        return summaries;
    }

    public async Task SaveAsync(Review review)
    {
        var reviews = await AllAsync();
        reviews.RemoveAll(r => r.SeekerId == review.SeekerId && r.ProfessionalId == review.ProfessionalId);
        reviews.Add(review);
        await _file.WriteAsync(reviews);
    }

    public async Task DeleteForPersonAsync(string personId)
    {
        var reviews = await AllAsync();
        if (reviews.RemoveAll(r => r.SeekerId == personId || r.ProfessionalId == personId) > 0)
        {
            await _file.WriteAsync(reviews);
        }
    }

    private async Task<List<Review>> AllAsync() => _reviews ??= await _file.ReadAsync() ?? [];

    private static IEnumerable<Review> Samples(string professionalId)
    {
        if (!DevSamples.Enabled)
        {
            return [];
        }

        var today = DateTimeOffset.Now.Date;
        if (professionalId == ListingMapper.OwnListingId)
        {
            return
            [
                new("sample-r1", professionalId, "sample-seeker-1", "Maricel D.", 5, "Mabilis dumating at malinis gumawa. Salamat po!",
                    "aircon-cleaning", today.AddDays(-1).AddHours(15)),
                new("sample-r2", professionalId, "sample-seeker-2", "Joel R.", 4, "Ayos ang trabaho, medyo late lang ng konti.",
                    "aircon-repair", today.AddDays(-6).AddHours(11)),
                new("sample-r3", professionalId, "sample-seeker-3", "Ana P.", 5, null,
                    "aircon-freon", today.AddDays(-20).AddHours(9)),
            ];
        }

        if (!SampleRatings.TryGetValue(professionalId, out var ratings))
        {
            return [];
        }

        var seed = professionalId.Sum(c => c);
        return ratings.Select((stars, i) => new Review(
            $"sample-{professionalId}-{i}",
            professionalId,
            $"sample-seeker-{i}",
            SampleNames[(seed + i) % SampleNames.Length],
            stars,
            SampleComments[(seed + i) % SampleComments.Length],
            null,
            today.AddDays(-(2 + (i * 6) + (seed % 5))).AddHours(10)));
    }
}
