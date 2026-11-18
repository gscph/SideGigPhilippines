using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using static SideGigPh.Tests.TestSupport.TestData;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// The order of search results decides which worker gets the customer. These tests pin down the promises the app
/// makes: only workers who do the job you asked for, workers who go to your town first, then a score out of 100 from
/// reviews (many good reviews beat one lucky review), how well they answer chats, how active they are, budget fit and
/// experience, with fair starting points for new workers.
/// </summary>
public class SearchRankerTests
{
    private static RankRequest Request(string? service = "aircon-cleaning", string? city = "taytay", string? budget = "500-1000") =>
        new(service, city, budget);

    private static RankedResult Rank(IReadOnlyList<ListingDto> pros, RankRequest request) =>
        SearchRanker.Rank([.. pros.Select(p => Candidate(p))], request, Scales, RankNow);

    private static RankedResult Rank(IReadOnlyList<RankCandidate> pros, RankRequest? request = null) =>
        SearchRanker.Rank(pros, request ?? Request(), Scales, RankNow);

    private static RankScore Score(RankCandidate pro, RankRequest? request = null) =>
        SearchRanker.Score(pro, request ?? Request(), Scales, RankNow);

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
    public void Rank_ABetterWorkerNearby_StillComesAfterAWorkerInYourTown()
    {
        // The town is a hard rule: the score only orders workers inside each group.
        var pros = new[]
        {
            Candidate(Dto("Star Antipolo", cities: ["antipolo"], experience: "10-plus", reviews: new ReviewSummary(5, 50)),
                Signals(received: 20, answered: 20, replyMinutes: 5, activeDaysAgo: 0, joinedDaysAgo: 400)),
            Candidate(Dto("Plain Taytay", experience: null), Signals(activeDaysAgo: 30, joinedDaysAgo: 1)),
        };

        var result = Rank(pros);

        Names(result.InTown).Should().Equal("Plain Taytay");
        Names(result.OtherTowns).Should().Equal("Star Antipolo");
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

    // Aircon cleaning's guide: ₱755 / ₱1,000 / ₱1,500. A Seeker budget of ₱755–₱1,000 fits the minimum and average rates.
    [Fact]
    public void Rank_PriceIsTheJobsGuideAtTheWorkersRate_ComparedWithTheTopOfTheBudget()
    {
        var pros = new ListingDto[]
        {
            Dto("Above Abe", rate: PriceLevels.AboveAverage),
            Dto("Average Ava", rate: PriceLevels.Average),
            Dto("Minimum Mia", rate: PriceLevels.Minimum),
        };

        var result = Rank(pros, Request(budget: "755-1000"));

        result.InTown.Where(h => h.FitsBudget).Select(h => h.Listing.DisplayName).Should().BeEquivalentTo("Average Ava", "Minimum Mia");
        Names(result.InTown).Last().Should().Be("Above Abe");
        Score(Candidate(pros[0]), Request(budget: "755-1000")).BudgetFit
            .Should().BeApproximately(SearchRanker.BudgetFitWeight * 0.5, 0.001, "₱1,500 is 50% over a ₱1,000 top: half the fit left");
        Score(Candidate(pros[0]), Request(budget: "500-750")).BudgetFit
            .Should().Be(0, "₱1,500 is double a ₱750 top: no fit left");
    }

    [Fact]
    public void Rank_SameRate_DifferentJob_DifferentPrice()
    {
        var pro = Dto("Ana", services: ["aircon-cleaning", "aircon-install"], rate: PriceLevels.Minimum);

        Rank([pro], Request(service: "aircon-cleaning", budget: "755-1000")).InTown.Single().FitsBudget.Should().BeTrue();
        Rank([pro], Request(service: "aircon-install", budget: "755-1000")).InTown.Single().FitsBudget
            .Should().BeFalse("installation starts at ₱2,500");
    }

    [Fact]
    public void Rank_OpenTopBudget_EveryPriceFits_OldAndNewSeekersAlike()
    {
        var pricey = Candidate(Dto("Pricey Pia", rate: PriceLevels.AboveAverage));

        Score(pricey, Request(budget: "1500-up")).BudgetFit.Should().Be(SearchRanker.BudgetFitWeight);
        Score(pricey, Request(budget: "5000-up")).BudgetFit.Should().Be(SearchRanker.BudgetFitWeight, "an old app's top tier");
        Rank([pricey.Listing], Request(budget: "1500-up")).InTown.Single().FitsBudget.Should().BeTrue();
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
    public void Rank_BadReviews_RankBelowNoReviewsYet()
    {
        var pros = new ListingDto[] { Dto("Poor Pete", reviews: new ReviewSummary(2.0, 10)), Dto("New Nina") };

        Names(Rank(pros, Request()).InTown).Should().Equal("New Nina", "Poor Pete");
    }

    [Fact]
    public void Rank_OnlyWorkersWhoDoTheJob_AreShown()
    {
        var pros = new ListingDto[] { Dto("Cleaner Cora", services: ["aircon-cleaning"]), Dto("Installer Ike", services: ["aircon-install"]) };

        var result = Rank(pros, Request(service: "aircon-install"));

        Names(result.InTown).Should().Equal("Installer Ike");
        result.OtherTowns.Should().BeEmpty();
    }

    [Fact]
    public void Rank_NobodyDoesTheJobYet_ShowsNobody()
    {
        // The job is a hard filter: a worker who doesn't list it is never shown for it.
        var pros = new ListingDto[] { Dto("Cleaner Cora", services: ["aircon-cleaning"]) };

        var page = Rank(pros, Request(service: "aircon-freon")).Page(0, Paging.Search);

        page.Hits.Should().BeEmpty();
        page.ServiceFallback.Should().BeFalse("kept in the contract for older apps, never set now");
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
        var pros = new[] { Candidate(Dto("Pricey Pia", rate: "5000-up")), Candidate(Dto("Quote Quin", rate: "quote")) };

        pros.Select(p => Score(p, Request(budget: "unsure")).BudgetFit).Distinct()
            .Should().ContainSingle("without a budget, price doesn't change the order");
    }

    [Fact]
    public void Rank_QuoteOnly_SitsBetweenInBudgetAndOverBudget()
    {
        var pros = new ListingDto[] { Dto("Over Olga", rate: "5000-up"), Dto("Quote Quin", rate: "quote"), Dto("Fits Fe", rate: "under-500") };

        Names(Rank(pros, Request(budget: "500-1000")).InTown).Should().Equal("Fits Fe", "Quote Quin", "Over Olga");
    }

    [Fact]
    public void Rank_WorkerWhoAnswersChats_BeatsOneWhoIgnoresThem()
    {
        var pros = new[]
        {
            Candidate(Dto("Silent Sam"), Signals(received: 10, answered: 2, replyMinutes: 300)),
            Candidate(Dto("Replies Rita"), Signals(received: 10, answered: 10, replyMinutes: 300)),
        };

        Names(Rank(pros).InTown).Should().Equal("Replies Rita", "Silent Sam");
    }

    [Fact]
    public void Rank_FastReplies_BeatSlowReplies()
    {
        var pros = new[]
        {
            Candidate(Dto("Slow Sid"), Signals(received: 5, answered: 5, replyMinutes: 600)),
            Candidate(Dto("Quick Quela"), Signals(received: 5, answered: 5, replyMinutes: 10)),
        };

        Names(Rank(pros).InTown).Should().Equal("Quick Quela", "Slow Sid");
    }

    [Fact]
    public void Rank_NewWorkerWithNoChatsYet_BeatsOneWhoIgnoresMostChats()
    {
        // Cold start: a new worker starts from a fair guess instead of zero, so they can win their first customers.
        var pros = new[]
        {
            Candidate(Dto("Ghost Gary"), Signals(received: 12, answered: 2, replyMinutes: 900)),
            Candidate(Dto("New Nena"), Signals()),
        };

        Names(Rank(pros).InTown).Should().Equal("New Nena", "Ghost Gary");
    }

    [Fact]
    public void Rank_ActiveThisWeek_BeatsAwayForAMonth()
    {
        var pros = new[]
        {
            Candidate(Dto("Away Ami"), Signals(activeDaysAgo: 30)),
            Candidate(Dto("Here Hana"), Signals(activeDaysAgo: 0)),
        };

        Names(Rank(pros).InTown).Should().Equal("Here Hana", "Away Ami");
    }

    [Fact]
    public void Rank_LongerOnTheApp_BeatsJustJoined_WhenAllElseIsEqual()
    {
        var pros = new[]
        {
            Candidate(Dto("Fresh Fely"), Signals(joinedDaysAgo: 2)),
            Candidate(Dto("Old Oscar"), Signals(joinedDaysAgo: 365)),
        };

        Names(Rank(pros).InTown).Should().Equal("Old Oscar", "Fresh Fely");
    }

    [Fact]
    public void Score_IsOutOf100_AndTheBestWorkerGetsNearlyAll()
    {
        var weights = SearchRanker.ReviewStarsWeight + SearchRanker.ReviewCountWeight + SearchRanker.ReplyRateWeight
            + SearchRanker.ReplySpeedWeight + SearchRanker.RecentActivityWeight + SearchRanker.TenureWeight
            + SearchRanker.BudgetFitWeight + SearchRanker.ExperienceWeight;
        var best = Candidate(
            Dto("Best Bea", rate: "under-500", experience: "10-plus", reviews: new ReviewSummary(5, 200)),
            Signals(received: 200, answered: 200, replyMinutes: 0, activeDaysAgo: 0, joinedDaysAgo: 400));

        weights.Should().Be(100);
        var score = Score(best);
        (score.Total - score.Trust - score.Photos).Should().BeInRange(97, 100, "badges and work photos are bonuses on top of the 100");
    }

    [Fact]
    public void Score_ExplainsEachPart()
    {
        var score = Score(Candidate(Dto("Pricey Pia", rate: "5000-up")));

        score.BudgetFit.Should().Be(0, "far over the Seeker's budget");
        score.Total.Should().Be(score.Reviews + score.Responsiveness + score.Activity + score.BudgetFit + score.Experience + score.Trust + score.Photos);
    }

    [Fact]
    public void Rank_NoWorkers_IsEmpty() =>
        Rank(Array.Empty<ListingDto>(), Request()).InTown.Should().BeEmpty();

    [Fact]
    public void Rank_OthersSearch_WorkersWhoTypedTheSameWordsComeFirst()
    {
        // "Others" has no fixed job, so what both sides typed is how they find each other.
        var pros = new ListingDto[]
        {
            Dto("Ace Plaster", services: ["aircon-other"]) with { OtherWork = "Paint ng pader", Reviews = new ReviewSummaryDto(5, 40) },
            Dto("Ben Kisame", services: ["aircon-other"]) with { OtherWork = "Kisame at plaster" },
        };

        var result = Rank(pros, Request(service: "aircon-other") with { OtherText = "plaster sa kisame" });

        Names(result.InTown).Should().Equal("Ben Kisame", "Ace Plaster");
    }

    [Fact]
    public void Rank_NotAnOthersSearch_IgnoresTypedText()
    {
        var pros = new ListingDto[]
        {
            Dto("Ace", reviews: new ReviewSummary(5, 40)),
            Dto("Ben") with { OtherWork = "linis" },
        };

        var result = Rank(pros, Request() with { OtherText = "linis" });

        Names(result.InTown).Should().Equal("Ace", "Ben");
    }

    [Fact]
    public void Price_OfAWorkersOwnRange_IsItsLowEnd()
    {
        var pro = Dto("Range Rita", rate: RateRange.Format(900, 2000));

        Scales.PriceOf(pro, "aircon-cleaning").Should().Be(900);
    }
}
