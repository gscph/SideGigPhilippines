using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Catalog;

/// <summary>
/// Every job has a price guide (lowest, average, above average; the lowest never under the minimum wage). A Skilled
/// Worker's rate is one of those points, so their price follows the job; a Seeker's budget is a peso range built from
/// the job's guide or typed. Listings and budgets from app versions before price guides must still read.
/// Built-in guide used here: aircon cleaning ₱755 / ₱1,000 / ₱1,500, aircon installation ₱2,500 / ₱3,500 / ₱5,000.
/// </summary>
public class PricingTests
{
    // --- Skilled Worker rate ---------------------------------------------------

    [Fact]
    public void Rate_IsThePointOfEachJobsGuide()
    {
        Pricing.PriceAt(PriceLevels.Minimum, "aircon-cleaning").Should().Be(755);
        Pricing.PriceAt(PriceLevels.Average, "aircon-cleaning").Should().Be(1000);
        Pricing.PriceAt(PriceLevels.AboveAverage, "aircon-install").Should().Be(5000);
        Pricing.PriceAt(ReferenceData.Quote, "aircon-cleaning").Should().BeNull("the price depends on the job");
    }

    [Fact]
    public void RateTiles_ShowEachLevelsPricesForTheWorkersJobs()
    {
        var tiles = Pricing.RateChoices(["aircon-cleaning", "aircon-install"]);

        tiles.Select(t => t.Id).Should().Equal(PriceLevels.Minimum, PriceLevels.Average, PriceLevels.AboveAverage, ReferenceData.Quote);
        tiles[0].Hint.Should().Be(new LocalizedText("₱755 – ₱2,500 per job", "₱755 – ₱2,500 per job"));
        Pricing.RateChoices(["aircon-cleaning"])[1].Hint!.English.Should().Be("₱1,000 per job");
    }

    [Fact]
    public void GoalPayoffPrice_IsTheMeanOfTheRatesPrices_OrAnOldTiersTypicalPrice()
    {
        Pricing.TypicalPrice(PriceLevels.Average, ["aircon-cleaning", "aircon-install"]).Should().Be(2250);
        Pricing.TypicalPrice("500-1000", ["aircon-cleaning"]).Should().Be(750, "a listing saved before price guides keeps its tier");
        Pricing.TypicalPrice(ReferenceData.Quote, ["aircon-cleaning"]).Should().BeNull();
    }

    [Fact]
    public void ListingPrice_IsForTheJobSearched_ElseOverAllTheirJobs()
    {
        var text = new AppText();
        var listing = TestData.Listing("Ana", services: ["aircon-cleaning", "aircon-install"], rate: PriceLevels.Average);

        text.Rate(listing, "aircon-install").Should().Be("₱3,500 per job");
        text.Rate(listing).Should().Be("₱1,000 – ₱3,500 per job");
        text.RateLabel(listing, "plumbing-clog").Should().Be("₱1,000 – ₱3,500", "a job they don't list falls back to all their jobs");
        text.Rate(TestData.Listing("Old", rate: "500-1000")).Should().Be("₱500 – ₱1,000 per job", "an old listing shows its tier");
        text.Rate(TestData.Listing("Quin", rate: ReferenceData.Quote)).Should().Be(text.Get(Copy.RateQuote));
    }

    // --- Seeker budget ---------------------------------------------------------

    [Fact]
    public void BudgetChoices_AreRangesFromTheJobsGuide_ThenNotSure()
    {
        var choices = Pricing.BudgetChoices("aircon-cleaning", "aircon", current: null);

        choices.Select(c => c.Id).Should().Equal("755-1000", "1000-1500", "1500-up", ReferenceData.Unsure);
        choices.Select(c => c.Label.English).Should().Equal("₱755 – ₱1,000", "₱1,000 – ₱1,500", "₱1,500 and up", "Not sure yet");
        choices[0].Hint.Should().Be(Copy.BudgetLow);
    }

    [Fact]
    public void BudgetChoices_KeepATypedOrOldBudget_SoItShowsAsPicked()
    {
        Pricing.BudgetChoices("aircon-cleaning", "aircon", "900-1300").Select(c => c.Id)
            .Should().Equal("755-1000", "1000-1500", "1500-up", "900-1300", ReferenceData.Unsure);
        Pricing.BudgetChoices("aircon-cleaning", "aircon", "1000-1500").Should().HaveCount(4, "a preset isn't added twice");
        Pricing.BudgetChoices("aircon-cleaning", "aircon", ReferenceData.Unsure).Should().HaveCount(4);
    }

    [Fact]
    public void BudgetChoices_ForAWholeTrade_UseAGuideOverItsJobs()
    {
        var guide = Pricing.GuideFor(null, "errand_runner")!;

        guide.Min.Should().Be(755);
        guide.Average.Should().Be(900, "the mean of the trade's average prices, to the nearest ₱50");
        Pricing.BudgetChoices(null, null, null).Should().ContainSingle("no job, no guide").Which.Id.Should().Be(ReferenceData.Unsure);
    }

    [Theory]
    [InlineData("800-1500", "₱800 – ₱1,500")]
    [InlineData("1500-up", "₱1,500 and up")]
    [InlineData("under-500", "Up to ₱500")]
    [InlineData("500-1000", "₱500 – ₱1,000")]
    [InlineData("unsure", "Not sure yet")]
    public void BudgetLabel_ReadsEveryBudgetId(string id, string english) =>
        Pricing.BudgetLabel(id)!.English.Should().Be(english);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cheap")]
    [InlineData("1500-800")]
    [InlineData("-up")]
    [InlineData("under-x")]
    public void BudgetLabel_NoneForWhatDoesntRead(string? id) => Pricing.BudgetLabel(id).Should().BeNull();

    [Theory]
    [InlineData(755, 755, true)]
    [InlineData(800, 1500, true)]
    [InlineData(700, 1500, false)]   // under the minimum wage
    [InlineData(1500, 800, false)]   // top under the bottom
    [InlineData(null, 1500, false)]
    [InlineData(800, null, false)]
    public void TypedBudget_StartsAtTheMinimumWage_AndGoesUp(int? min, int? max, bool valid) =>
        Pricing.IsValidBudget(min, max).Should().Be(valid);

    [Fact]
    public void TypedBudget_StartsFromTheSavedRange_ElseTheJobsLowestToAverage()
    {
        Pricing.CustomStart("900-1300", "aircon-cleaning", "aircon").Should().Be((900, 1300));
        Pricing.CustomStart("under-500", "aircon-cleaning", "aircon").Should().Be((755, 1000), "under the minimum wage isn't offered again");
        Pricing.CustomStart(null, "aircon-cleaning", "aircon").Should().Be((755, 1000));
        Pricing.CustomStart(null, null, null).Should().Be((755, 755));
    }

    // --- Server ranking reads the same prices ------------------------------------

    [Fact]
    public void ServerPrice_IsTheGuideAtTheRate_ForTheJobSearched_OrTheirCheapestJob()
    {
        var pro = TestData.Dto("Ana", services: ["aircon-cleaning", "aircon-install"], rate: PriceLevels.Average);

        TestData.Scales.PriceOf(pro, "aircon-install").Should().Be(3500);
        TestData.Scales.PriceOf(pro, null).Should().Be(1000);
        TestData.Scales.PriceOf(pro with { RateTierId = "2000-5000" }, "aircon-cleaning").Should().Be(3500, "an old tier's typical price");
        TestData.Scales.PriceOf(pro with { RateTierId = ReferenceData.Quote }, "aircon-cleaning").Should().BeNull();
    }

    [Fact]
    public void ServerScales_FromTheConfig_MatchTheApp()
    {
        var dto = new ConfigDto(
            [new CategoryDto("aircon", "Aircon", "Aircon", null, null, null, [new ServiceDto("aircon-cleaning", "A", "A", null, new PriceGuideDto(755, 1000, 1500))])],
            [new OptionDto("taytay", "Taytay", "Taytay", null, 0)],
            [new OptionDto("500-1000", "x", "x", null, 750)],
            [new OptionDto("10-plus", "x", "x", null, 0)],
            [],
            [],
            755);

        var scales = RankScales.From(dto);

        scales.Prices["aircon-cleaning"].Should().Be(new PriceGuideDto(755, 1000, 1500));
        scales.TierPrices["500-1000"].Should().Be(750);
        scales.TownIds.Should().Equal("taytay");
    }
}
