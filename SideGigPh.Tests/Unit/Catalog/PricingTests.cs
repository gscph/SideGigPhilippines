using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Catalog;

/// <summary>
/// Every job has a price guide (low, average, high), only ever a guide. A Skilled Worker types one price, their average
/// per service, and sees the guide over their jobs beside the box; a Seeker's budget is a peso range built from the
/// job's guide or typed. Built-in guide used here: aircon cleaning ₱755 / ₱1,000 / ₱1,500, aircon installation
/// ₱2,500 / ₱3,500 / ₱5,000.
/// </summary>
public class PricingTests
{
    // --- Skilled Worker price ----------------------------------------------------

    [Fact]
    public void WorkerPrice_IsOneAmountAboveZero()
    {
        WorkerPrice.IsValid(1200).Should().BeTrue();
        WorkerPrice.IsValid(1).Should().BeTrue("any price above ₱0: the guide is only a guide");
        WorkerPrice.IsValid(0).Should().BeFalse();
        WorkerPrice.IsValid(null).Should().BeFalse();
        WorkerPrice.IsValid(WorkerPrice.MaxPesos + 1).Should().BeFalse();
    }

    [Fact]
    public void GuideBesideThePriceBox_IsOverTheWorkersJobs_NoneForAnOthersJob()
    {
        Pricing.GuideOver(["aircon-cleaning", "aircon-install"]).Should().Be(new PriceGuide(755, 2250, 3250),
            "the lowest low, then the averages and highs on average (to the nearest ₱50)");
        Pricing.GuideOver(["aircon-cleaning"]).Should().Be(new PriceGuide(755, 1000, 1500));
        Pricing.GuideOver([ReferenceData.OtherTradeId]).Should().BeNull();
    }

    [Fact]
    public void ListingPrice_IsTheirAveragePrice()
    {
        var text = new AppText();

        text.Rate(TestData.Listing("Ana", rate: 1200)).Should().Be("₱1,200 average per service");
        TestData.Listing("Ana", rate: 1200).RateLabel().Should().Be("₱1,200");
        text.Rate(TestData.Listing("Draft", rate: null)).Should().Be(text.Get(Copy.RateNotSet));
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
    public void BudgetChoices_KeepATypedBudget_SoItShowsAsPicked()
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
    [InlineData("under-1500", "Up to ₱1,500")]
    [InlineData("unsure", "Not sure yet")]
    public void BudgetLabel_ReadsEveryBudgetId(string id, string english) =>
        Pricing.BudgetLabel(id)!.English.Should().Be(english);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cheap")]
    [InlineData("1500-800")]
    [InlineData("-up")]
    public void BudgetLabel_NoneForWhatDoesntRead(string? id) => Pricing.BudgetLabel(id).Should().BeNull();

    [Theory]
    [InlineData(755, 755, true)]
    [InlineData(800, 1500, true)]
    [InlineData(300, 1500, true)]    // under the job's guide: it is only a guide
    [InlineData(0, 1500, false)]     // a budget starts above ₱0
    [InlineData(1500, 800, false)]   // top under the bottom
    [InlineData(null, 1500, false)]
    [InlineData(800, null, false)]
    public void TypedBudget_AnyAmountAboveZero_AndGoesUp(int? min, int? max, bool valid) =>
        Pricing.IsValidBudget(min, max).Should().Be(valid);

    [Fact]
    public void TypedBudget_StartsFromTheSavedRange_ElseTheJobsLowestToAverage()
    {
        Pricing.CustomStart("900-1300", "aircon-cleaning", "aircon").Should().Be((900, 1300));
        Pricing.CustomStart(null, "aircon-cleaning", "aircon").Should().Be((755, 1000));
        Pricing.CustomStart(null, null, null).Should().Be(((int?)null, (int?)null), "no guide: the boxes start empty");
    }

    [Fact]
    public void BudgetGuideNote_IsTheJobsUsualPrices()
    {
        Pricing.GuideNote("aircon-cleaning", "aircon")!.English.Should().Be("Usual price: ₱755 – ₱1,500. Only a guide.");
        Pricing.GuideNote(null, null).Should().BeNull();
    }

    // --- Server ranking reads the same guides ---------------------------------------

    [Fact]
    public void ServerScales_FromTheConfig_MatchTheApp()
    {
        var dto = new ConfigDto(
            [new CategoryDto("aircon", "Aircon", "Aircon", null, null, null, [new ServiceDto("aircon-cleaning", "A", "A", null, new PriceGuideDto(755, 1000, 1500))])],
            [new OptionDto("taytay", "Taytay", "Taytay", null, 0)],
            [new OptionDto("10-plus", "x", "x", null, 0)],
            [],
            []);

        var scales = RankScales.From(dto);

        scales.Prices["aircon-cleaning"].Should().Be(new PriceGuideDto(755, 1000, 1500));
        scales.ExperienceIds.Should().Equal("10-plus");
        scales.TownIds.Should().Equal("taytay");
    }

    [Fact]
    public void PostBudget_ShowsTheRange_OrTheTopAlone()
    {
        var post = new JobPost(1, "plumbing", "plumbing-clog", "Barado ang lababo", 1500, "taytay", [], true, true, false, null, DateTimeOffset.Now, BudgetMinPesos: 800);

        Pricing.PostBudget(post).Should().Be("₱800 – ₱1,500");
        Pricing.PostBudget(post with { BudgetMinPesos = null }).Should().Be("₱1,500");
    }
}
