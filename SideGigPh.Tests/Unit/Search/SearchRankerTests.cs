using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using static SideGigPh.Tests.TestSupport.TestData;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// The order of search results decides which worker gets the customer. These tests pin down the promises the app
/// makes: workers who go to your town first, workers in your budget before others, many good reviews beating one
/// lucky review, and only workers who do the job you asked for.
/// </summary>
public class SearchRankerTests
{
    private static RankRequest Request(string? service = "aircon-cleaning", string? city = "taytay", string? budget = "500-1000") =>
        new(service, city, budget);

    private static RankedResult Rank(IReadOnlyList<ListingDto> pros, RankRequest request) => SearchRanker.Rank(pros, request, Scales);

    private static string[] Names(IEnumerable<SearchHitDto> hits) => hits.Select(h => h.Listing.DisplayName).ToArray();

    [Fact]
    public void Rank_WorkersInYourTownComeFirst_NearbyTownsAfter()
    {
        var pros = new ListingDto[] { Dto("Antipolo Ana", cities: ["antipolo"]), Dto("Taytay Tess", cities: ["taytay"]) };

        var result = Rank(pros, Request());

        Names(result.InTown).Should().Equal("Taytay Tess");
        Names(result.OtherTowns).Should().Equal("Antipolo Ana");
    }

    [Fact]
    public void Rank_WorkerOnlyInATownWeDontServe_IsLeftOut()
    {
        var pros = new ListingDto[] { Dto("Makati Mark", cities: ["makati"]), Dto("Taytay Tess") };

        var result = Rank(pros, Request());

        (result.InTown.Count + result.OtherTowns.Count).Should().Be(1);
        Names(result.InTown).Should().Equal("Taytay Tess");
    }

    [Fact]
    public void Rank_WorkerInYourBudget_BeatsWorkerOverIt()
    {
        var pros = new ListingDto[] { Dto("Pricey Pia", rate: "2000-5000"), Dto("Budget Ben", rate: "under-500") };

        var result = Rank(pros, Request(budget: "500-1000"));

        Names(result.InTown).Should().Equal("Budget Ben", "Pricey Pia");
        result.InTown[0].FitsBudget.Should().BeTrue();
        result.InTown[1].FitsBudget.Should().BeFalse();
    }

    [Fact]
    public void Rank_ManyGoodReviews_BeatOnePerfectReview()
    {
        var pros = new ListingDto[]
        {
            Dto("Lucky Lito", reviews: new ReviewSummary(5.0, 1)),
            Dto("Steady Sol", reviews: new ReviewSummary(4.8, 40)),
        };

        Names(Rank(pros, Request()).InTown).Should().Equal("Steady Sol", "Lucky Lito");
    }

    [Fact]
    public void Rank_OnlyWorkersWhoDoTheJob_AreShown()
    {
        var pros = new ListingDto[] { Dto("Cleaner Cora", services: ["aircon-cleaning"]), Dto("Installer Ike", services: ["aircon-install"]) };

        var result = Rank(pros, Request(service: "aircon-install"));

        Names(result.InTown).Should().Equal("Installer Ike");
        result.ServiceFallback.Should().BeFalse();
    }

    [Fact]
    public void Rank_NobodyDoesTheJobYet_ShowsTheWholeTradeFlagged()
    {
        // Better to show aircon techs than an empty page; the page says so.
        var pros = new ListingDto[] { Dto("Cleaner Cora", services: ["aircon-cleaning"]) };

        var result = Rank(pros, Request(service: "aircon-freon"));

        result.ServiceFallback.Should().BeTrue();
        Names(result.InTown).Should().Equal("Cleaner Cora");
    }

    [Fact]
    public void Rank_NoTownChosen_PutsEveryoneInOneList()
    {
        var pros = new ListingDto[] { Dto("A", cities: ["antipolo"]), Dto("B", cities: ["angono"]) };

        var result = Rank(pros, Request(city: null));

        result.InTown.Should().HaveCount(2);
        result.OtherTowns.Should().BeEmpty();
    }

    [Fact]
    public void Rank_SameScore_IsOrderedByName_SoResultsDontJumpAround()
    {
        var pros = new ListingDto[] { Dto("Carlo"), Dto("ana"), Dto("Bea") };

        Names(Rank(pros, Request()).InTown).Should().Equal("ana", "Bea", "Carlo");
    }

    [Fact]
    public void Rank_MoreExperience_RanksHigher_UnknownExperienceCountsAsNone()
    {
        var pros = new ListingDto[] { Dto("New Nico", experience: null), Dto("Veteran Vic", experience: "10-plus"), Dto("Mid Mel", experience: "3-10") };

        Names(Rank(pros, Request()).InTown).Should().Equal("Veteran Vic", "Mid Mel", "New Nico");
    }

    [Fact]
    public void Rank_NoBudgetGiven_DoesNotPenaliseAnyPrice()
    {
        var pros = new ListingDto[] { Dto("Pricey Pia", rate: "5000-up"), Dto("Quote Quin", rate: "quote") };

        var hits = Rank(pros, Request(budget: "unsure")).InTown;

        hits.Select(h => SearchRanker.Score(h.Listing, Request(budget: "unsure"), Scales)).Distinct().Should().ContainSingle("without a budget, price doesn't change the order");
    }

    [Fact]
    public void Rank_QuoteOnly_SitsBetweenInBudgetAndOverBudget()
    {
        var pros = new ListingDto[] { Dto("Over Olga", rate: "5000-up"), Dto("Quote Quin", rate: "quote"), Dto("Fits Fe", rate: "under-500") };

        Names(Rank(pros, Request(budget: "500-1000")).InTown).Should().Equal("Fits Fe", "Quote Quin", "Over Olga");
    }

    [Fact]
    public void Rank_NoWorkers_IsEmpty() =>
        Rank([], Request()).InTown.Should().BeEmpty();
}
