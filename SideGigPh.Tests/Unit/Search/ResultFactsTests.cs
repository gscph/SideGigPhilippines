using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// The words on a search result row and the results' headings (Airbnb's list: what + where with ★, the name, facts
/// joined by ·). A wrong town or a missing fact here misleads a household choosing a worker.
/// </summary>
public class ResultFactsTests
{
    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    private static ProfessionalListing Worker(string[]? cities = null, string? experience = "10-plus", int done = 0, bool badge = false) => new()
    {
        Id = "juan",
        DisplayName = "Juan Dela Cruz",
        CategoryIds = ["aircon"],
        ServiceIds = ["aircon-cleaning"],
        CityIds = cities ?? ["taytay", "angono"],
        ExperienceId = experience,
        RatePesos = 800,
        MobileNumber = string.Empty,
        CompletedJobs = done,
        Badges = badge ? [new Badge(BadgeKind.Id)] : [],
    };

    [Fact]
    public void Town_SeekersOwnWhenTheyGoThere_ElseTheirFirstServed()
    {
        ResultFacts.TownId(Worker(), "angono").Should().Be("angono");
        ResultFacts.TownId(Worker(cities: ["nowhere", "taytay"]), "angono").Should().Be("taytay");
        ResultFacts.TownId(Worker(cities: ["nowhere"]), null).Should().BeNull();
    }

    [Fact]
    public void LineOne_WhatAndWhere()
    {
        var text = English();

        text.WhatAndWhere(Worker(), "angono").Should().Be("Aircon Technician in Angono");
        text.WhatAndWhere(Worker(), null).Should().Be("Aircon Technician in Taytay");
        text.WhatAndWhere(Worker(cities: []), "angono").Should().Be("Aircon Technician");
    }

    [Fact]
    public void FactsLine_OnlyWhatThereIs()
    {
        var text = English();

        text.FactsLine(Worker(done: 8, badge: true)).Should().Be("10+ years · 8 done · ID Verified");
        text.FactsLine(Worker()).Should().Be("10+ years", "no services done and no badge say nothing");
        text.FactsLine(Worker(experience: null)).Should().BeNull();
        ResultFacts.Facts(Worker(experience: null, done: 2)).Should().Equal(ResultFact.ServicesDone);
    }

    [Fact]
    public void Stars_ShortOrNew()
    {
        var text = English();

        text.StarsShort(new ReviewSummary(4.86, 12)).Should().Be("4.9 (12)");
        text.StarsShort(null).Should().Be("New");
        text.StarsShort(new ReviewSummary(0, 0)).Should().Be("New");
    }

    [Fact]
    public void Headings_SayWhere()
    {
        var text = English();

        text.InTownHeading(9, "Angono").Should().Be("9 skilled workers in Angono");
        text.InTownHeading(1, "Angono").Should().Be("1 skilled worker in Angono");
        text.NearbyHeading(3, 9, "Angono").Should().Be("3 more in towns nearby");
        text.NearbyHeading(3, 0, "Angono").Should().Be("None in Angono yet · 3 in towns nearby");
    }
}
