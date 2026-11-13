using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Search;
using static SideGigPh.Tests.TestSupport.TestData;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// The order of search results decides which worker gets the customer. These tests pin down the promises the app
/// makes: workers who go to your town first, workers in your budget before others, many good reviews beating one
/// lucky review, and only workers who do the job you asked for.
/// </summary>
public class SearchRankerTests
{
    private static SearchRequest Request(string? service = "aircon-cleaning", string? city = "taytay", string? budget = "500-1000") =>
        new("aircon", service, city, budget);

    private static string[] Names(IEnumerable<SearchHit> hits) => hits.Select(h => h.Pro.DisplayName).ToArray();

    [Fact]
    public void Rank_WorkersInYourTownComeFirst_NearbyTownsAfter()
    {
        var pros = new[] { Listing("Antipolo Ana", cities: ["antipolo"]), Listing("Taytay Tess", cities: ["taytay"]) };

        var result = SearchRanker.Rank(pros, Request());

        Names(result.InTown).Should().Equal("Taytay Tess");
        Names(result.OtherTowns).Should().Equal("Antipolo Ana");
    }

    [Fact]
    public void Rank_WorkerOnlyInATownWeDontServe_IsLeftOut()
    {
        var pros = new[] { Listing("Makati Mark", cities: ["makati"]), Listing("Taytay Tess") };

        var result = SearchRanker.Rank(pros, Request());

        result.Count.Should().Be(1);
        Names(result.InTown).Should().Equal("Taytay Tess");
    }

    [Fact]
    public void Rank_WorkerInYourBudget_BeatsWorkerOverIt()
    {
        var pros = new[] { Listing("Pricey Pia", rate: "2000-5000"), Listing("Budget Ben", rate: "under-500") };

        var result = SearchRanker.Rank(pros, Request(budget: "500-1000"));

        Names(result.InTown).Should().Equal("Budget Ben", "Pricey Pia");
        result.InTown[0].FitsBudget.Should().BeTrue();
        result.InTown[1].FitsBudget.Should().BeFalse();
    }

    [Fact]
    public void Rank_ManyGoodReviews_BeatOnePerfectReview()
    {
        var pros = new[]
        {
            Listing("Lucky Lito", reviews: new ReviewSummary(5.0, 1)),
            Listing("Steady Sol", reviews: new ReviewSummary(4.8, 40)),
        };

        Names(SearchRanker.Rank(pros, Request()).InTown).Should().Equal("Steady Sol", "Lucky Lito");
    }

    [Fact]
    public void Rank_OnlyWorkersWhoDoTheJob_AreShown()
    {
        var pros = new[] { Listing("Cleaner Cora", services: ["aircon-cleaning"]), Listing("Installer Ike", services: ["aircon-install"]) };

        var result = SearchRanker.Rank(pros, Request(service: "aircon-install"));

        Names(result.InTown).Should().Equal("Installer Ike");
        result.ServiceFallback.Should().BeFalse();
    }

    [Fact]
    public void Rank_NobodyDoesTheJobYet_ShowsTheWholeTradeFlagged()
    {
        // Better to show aircon techs than an empty page; the page says so.
        var pros = new[] { Listing("Cleaner Cora", services: ["aircon-cleaning"]) };

        var result = SearchRanker.Rank(pros, Request(service: "aircon-freon"));

        result.ServiceFallback.Should().BeTrue();
        Names(result.InTown).Should().Equal("Cleaner Cora");
    }

    [Fact]
    public void Rank_NoTownChosen_PutsEveryoneInOneList()
    {
        var pros = new[] { Listing("A", cities: ["antipolo"]), Listing("B", cities: ["angono"]) };

        var result = SearchRanker.Rank(pros, Request(city: null));

        result.InTown.Should().HaveCount(2);
        result.OtherTowns.Should().BeEmpty();
    }

    [Fact]
    public void Rank_SameScore_IsOrderedByName_SoResultsDontJumpAround()
    {
        var pros = new[] { Listing("Carlo"), Listing("ana"), Listing("Bea") };

        Names(SearchRanker.Rank(pros, Request()).InTown).Should().Equal("ana", "Bea", "Carlo");
    }

    [Fact]
    public void Rank_MoreExperience_RanksHigher_UnknownExperienceCountsAsNone()
    {
        var pros = new[] { Listing("New Nico", experience: null), Listing("Veteran Vic", experience: "10-plus"), Listing("Mid Mel", experience: "3-10") };

        Names(SearchRanker.Rank(pros, Request()).InTown).Should().Equal("Veteran Vic", "Mid Mel", "New Nico");
    }

    [Fact]
    public void Rank_NoBudgetGiven_DoesNotPenaliseAnyPrice()
    {
        var pros = new[] { Listing("Pricey Pia", rate: "5000-up"), Listing("Quote Quin", rate: "quote") };

        var hits = SearchRanker.Rank(pros, Request(budget: "unsure")).InTown;

        hits.Select(h => h.Score).Distinct().Should().ContainSingle("without a budget, price doesn't change the order");
    }

    [Fact]
    public void Rank_QuoteOnly_SitsBetweenInBudgetAndOverBudget()
    {
        var pros = new[] { Listing("Over Olga", rate: "5000-up"), Listing("Quote Quin", rate: "quote"), Listing("Fits Fe", rate: "under-500") };

        Names(SearchRanker.Rank(pros, Request(budget: "500-1000")).InTown).Should().Equal("Fits Fe", "Quote Quin", "Over Olga");
    }

    [Fact]
    public void Rank_NoWorkers_IsEmpty() =>
        SearchRanker.Rank([], Request()).Count.Should().Be(0);
}
