using SideGigPh.Contracts;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.UpdateSafety;

/// <summary>
/// Ids of trades, jobs, towns, prices and so on are saved on phones, in listings and in chats. Renaming or
/// removing one after release silently breaks those users (a worker vanishes from search, a saved answer is lost).
/// These tests are snapshots: if one fails, you changed an id. Change only the label instead. If you really meant
/// to add a new option, add its id to the list here too.
/// </summary>
public class StableIdsTests
{
    [Fact]
    public void CategoryIds_NeverChange() =>
        ReferenceData.Categories.Select(c => c.Id).Should().Equal(
            "aircon", "plumbing", "electrical", "roofing", "carpentry", "welder", "masonry_tiling", "car_mechanic",
            "errand_runner", "solar_cleaning");

    // A retired trade keeps its id in the database (004-config.sql, IsActive = 0) so old listings and reviews still
    // read. Giving the id to a new trade would relabel those old listings.
    [Fact]
    public void RetiredIds_AreNeverReused()
    {
        ReferenceData.BuiltInConfig.Categories.Select(c => c.Id).Should().NotContain("home_massage");
        ReferenceData.BuiltInConfig.ServicesByCategory.Values.SelectMany(s => s).Select(s => s.Id)
            .Should().NotContain(id => id.StartsWith("massage-"));
    }

    public static TheoryData<string, string[]> ServiceIdsByCategory => new()
    {
        { "aircon", ["aircon-cleaning", "aircon-repair", "aircon-leak", "aircon-freon", "aircon-install", "aircon-relocate", "aircon-pms", "aircon-other"] },
        { "plumbing", ["plumbing-clog", "plumbing-leak", "plumbing-toilet", "plumbing-replace", "plumbing-install", "plumbing-water-heater", "plumbing-pump", "plumbing-septic", "plumbing-other"] },
        { "electrical", ["electrical-no-power", "electrical-outlet", "electrical-lights", "electrical-wiring", "electrical-breaker-panel", "electrical-meter", "electrical-cctv", "electrical-other"] },
        { "roofing", ["roofing-leak", "roofing-gutter", "roofing-sheet", "roofing-waterproofing", "roofing-paint", "roofing-other"] },
        { "carpentry", ["carpentry-furniture", "carpentry-door-window", "carpentry-cabinet", "carpentry-ceiling-roof", "carpentry-partition", "carpentry-other"] },
        { "welder", ["welder-repair", "welder-gate", "welder-grills", "welder-railings", "welder-frame", "welder-other"] },
        { "masonry_tiling", ["masonry-tiles", "masonry-cement", "masonry-crack", "masonry-wall", "masonry-cr-renovation", "masonry-other"] },
        { "errand_runner", ["errand-grocery", "errand-bills", "errand-pasabuy", "errand-documents", "errand-parcel", "errand-other"] },
        { "car_mechanic", ["mechanic-suspension", "mechanic-engine", "mechanic-electronics", "mechanic-other"] },
        { "solar_cleaning", ["solar-cleaning", "solar-checkup", "solar-inverter", "solar-other"] },
    };

    // Order may change (most-requested first), ids may not, so this compares as a set.
    [Theory]
    [MemberData(nameof(ServiceIdsByCategory))]
    public void ServiceIds_NeverChange(string categoryId, string[] expected) =>
        ReferenceData.ServicesByCategory[categoryId].Select(s => s.Id).Should().BeEquivalentTo(expected);

    [Fact]
    public void EveryCategory_HasServices_EndingWithOther()
    {
        foreach (var category in ReferenceData.Categories)
        {
            ReferenceData.ServicesByCategory.Should().ContainKey(category.Id);
            var last = ReferenceData.ServicesByCategory[category.Id].Last();
            last.Id.Should().EndWith(ReferenceData.OtherJobSuffix,
                "every trade ends with an \"Others / Wala sa nabanggit\" option so nobody is forced into a wrong pick");
            last.Label.Should().Be(new LocalizedText("Others", "Wala sa nabanggit"));
            ReferenceData.IsOtherJob(last.Id).Should().BeTrue();
        }
    }

    [Fact]
    public void ServiceIds_AreUniqueAcrossAllTrades() =>
        ReferenceData.ServicesByCategory.Values.SelectMany(s => s).Select(s => s.Id).Should().OnlyHaveUniqueItems();

    [Fact]
    public void TownIds_NeverChange()
    {
        ReferenceData.Cities.Select(c => c.Id).Should().Equal("angono", "antipolo", "taytay");
        ReferenceData.ServiceAreaId.Should().Be("rizal");
    }

    [Fact]
    public void EveryTown_BelongsToTheServiceArea() =>
        ReferenceData.Cities.Should().OnlyContain(c => ReferenceData.AreaOf(c.Id) == ReferenceData.ServiceAreaId);

    // Old app versions still send these as budgets and rates, and they read as peso ranges (BudgetRange).
    [Fact]
    public void PriceTierIds_NeverChange_CheapestFirst()
    {
        ReferenceData.PriceTiers.Select(p => p.Id).Should().Equal("under-500", "500-1000", "1000-2000", "2000-5000", "5000-up");
        ReferenceData.PriceTiers.Select(p => p.Amount).Should().BeInAscendingOrder();
    }

    [Fact]
    public void OtherAnswerIds_NeverChange()
    {
        ReferenceData.Unsure.Should().Be("unsure");
        ReferenceData.Quote.Should().Be("quote");
        ReferenceData.Experience.Select(e => e.Id).Should().Equal("under-1", "1-3", "3-10", "10-plus");
        ReferenceData.TopExperienceId.Should().Be("10-plus");
        ReferenceData.Urgencies.Select(u => u.Id).Should().Equal("today", "this-week", "canvassing");
        ReferenceData.IncomeGoals.Select(g => g.Id).Should().Equal("5000", "10000", "20000", "30000-up");
        ReferenceData.OtherTradeId.Should().Be("other");
    }

    // Saved in drafts and listings (RateTierId) and read by the server's ranking (PriceLevels in SideGigPh.Contracts).
    [Fact]
    public void RateLevelIds_NeverChange_CheapestFirst() =>
        ReferenceData.RateLevels.Select(r => r.Id).Should().Equal("minimum", "average", "above-average");

    // Budgets travel as text in chats and search ("800-1500"); old price tiers must keep reading as ranges.
    [Theory]
    [InlineData("800-1500", 800, 1500)]
    [InlineData("1500-up", 1500, null)]
    [InlineData("under-500", 0, 500)]
    [InlineData("500-1000", 500, 1000)]
    [InlineData("5000-up", 5000, null)]
    public void BudgetIds_KeepTheirFormat(string id, int min, int? max)
    {
        BudgetRange.TryParse(id, out var readMin, out var readMax).Should().BeTrue();
        (readMin, readMax).Should().Be((min, max));
        if (!id.StartsWith("under-"))
        {
            BudgetRange.Format(min, max).Should().Be(id);
        }
    }
}
