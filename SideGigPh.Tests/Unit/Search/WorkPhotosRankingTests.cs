using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using static SideGigPh.Tests.TestSupport.TestData;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// Work photos are optional (a listing publishes without them), so they earn a bonus on top of the 100 instead of a
/// requirement: customers pick workers with photos first, and a worker who adds some moves up. A worker without
/// photos must score exactly as before, or making photos optional would quietly push them down.
/// </summary>
public class WorkPhotosRankingTests
{
    private static readonly RankRequest Request = new("aircon-cleaning", "taytay", "500-1000");

    private static ListingDto WithPhotos(string name, int count) =>
        Dto(name) with { WorkPhotos = [.. Enumerable.Range(1, count).Select(i => new PhotoDto($"w{i}", $"https://example/w{i}.jpg"))] };

    private static RankScore Score(ListingDto pro) => SearchRanker.Score(Candidate(pro), Request, Scales, RankNow);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1.5)]
    [InlineData(2, 3)]
    [InlineData(6, 3)] // capped: showing some photos counts, not piling them up
    public void PhotosBonus_HalfForOne_FullFromTwo(int count, double expected) =>
        Score(WithPhotos("Worker", count)).Photos.Should().Be(expected);

    [Fact]
    public void PhotosBonus_IsOnTopOfTheSameScore()
    {
        var without = Score(WithPhotos("Plain", 0));
        var with = Score(WithPhotos("Shown", 3));

        with.Photos.Should().Be(SearchRanker.WorkPhotosWeight);
        (with.Total - with.Photos).Should().BeApproximately(without.Total, 1e-9, "a bonus: nothing else in the score changes");
    }

    [Fact]
    public void WorkerWithoutPhotos_ScoresExactlyAsBeforeTheBonus()
    {
        var score = Score(WithPhotos("Plain", 0));

        score.Photos.Should().Be(0);
        score.Total.Should().Be(score.Reviews + score.Responsiveness + score.Activity + score.PriceFit + score.Experience + score.Trust);
    }

    [Fact]
    public void Rank_AmongEqualWorkers_TheOneWithPhotosComesFirst()
    {
        // "Aaron" would win the name tie-break; the photos put "Zed" first.
        var pros = new[] { Candidate(WithPhotos("Aaron", 0)), Candidate(WithPhotos("Zed", 2)) };

        var result = SearchRanker.Rank(pros, Request, Scales, RankNow);

        result.InTown.Select(h => h.Listing.DisplayName).Should().Equal("Zed", "Aaron");
    }

    [Fact]
    public void Rank_TwoPhotosBeatOne()
    {
        var pros = new[] { Candidate(WithPhotos("Aaron", 1)), Candidate(WithPhotos("Zed", 2)) };

        var result = SearchRanker.Rank(pros, Request, Scales, RankNow);

        result.InTown.Select(h => h.Listing.DisplayName).Should().Equal("Zed", "Aaron");
    }
}
