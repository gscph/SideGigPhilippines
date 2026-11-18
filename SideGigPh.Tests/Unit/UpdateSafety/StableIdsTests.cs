using SideGigPh.Contracts;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.UpdateSafety;

/// <summary>
/// The ids the app's code names by hand (and old app versions still send), plus the shape every trade must have.
/// Trades and jobs themselves live in 004-config.sql and are not listed here: adding one needs no test edit.
/// </summary>
public class StableIdsTests
{
    // A retired trade keeps its id in the database (004-config.sql, IsActive = 0) so old listings and reviews still
    // read. Giving the id to a new trade would relabel those old listings.
    [Fact]
    public void RetiredIds_AreNeverReused()
    {
        ReferenceData.BuiltInConfig.Categories.Select(c => c.Id).Should().NotContain("home_massage");
        ReferenceData.BuiltInConfig.ServicesByCategory.Values.SelectMany(s => s).Select(s => s.Id)
            .Should().NotContain(id => id.StartsWith("massage-"));
    }

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

    [Fact]
    public void OtherAnswerIds_NeverChange()
    {
        ReferenceData.Unsure.Should().Be("unsure");
        ReferenceData.Experience.Select(e => e.Id).Should().Equal("under-1", "1-3", "3-10", "10-plus");
        ReferenceData.TopExperienceId.Should().Be("10-plus");
        ReferenceData.Urgencies.Select(u => u.Id).Should().Equal("today", "this-week", "canvassing");
        ReferenceData.IncomeGoals.Select(g => g.Id).Should().Equal("5000", "10000", "20000", "30000-up");
        ReferenceData.OtherTradeId.Should().Be("other");
    }

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
