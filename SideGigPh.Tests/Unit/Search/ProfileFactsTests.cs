using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// What a worker's public profile shows (Airbnb-style: one line of what and where, a stat row, "What Juan does", a
/// sideways review row). A wrong rule here hides a worker's track record or names a town they don't go to.
/// </summary>
public class ProfileFactsTests
{
    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    private static ProfessionalListing Worker(int completedJobs = 0, string? experience = "10-plus", string[]? cities = null) => new()
    {
        Id = "juan",
        DisplayName = "Juan Dela Cruz",
        CategoryIds = ["aircon"],
        ServiceIds = ["aircon-cleaning"],
        CityIds = cities ?? ["taytay", "angono"],
        ExperienceId = experience,
        RateTierId = "500-1000",
        MobileNumber = string.Empty,
        CompletedJobs = completedJobs,
    };

    private static Review ReviewOf(string id, int stars, string? comment) =>
        new(id, "juan", $"seeker-{id}", "Maria S.", stars, comment, "aircon-cleaning", DateTimeOffset.UnixEpoch);

    [Fact]
    public void Stats_StarsDoneAndYears_WhenThereIsSomethingToSay()
    {
        ProfileFacts.Stats(Worker(completedJobs: 8), new ReviewSummary(4.9, 12))
            .Should().Equal(ProfileStat.Stars, ProfileStat.ServicesDone, ProfileStat.Experience);
    }

    [Fact]
    public void Stats_NewWorker_SaysNewAndSkipsZeroDone()
    {
        ProfileFacts.Stats(Worker(completedJobs: 0, experience: null), null)
            .Should().Equal(ProfileStat.NoReviews);
        ProfileFacts.Stats(Worker(), new ReviewSummary(0, 0))
            .Should().Equal(ProfileStat.NoReviews, ProfileStat.Experience);
    }

    [Fact]
    public void Towns_SeekersOwnFirst_UnservedDropped()
    {
        var worker = Worker(cities: ["taytay", "nowhere", "angono", "antipolo"]);

        ProfileFacts.Towns(worker, "angono").Should().Equal("angono", "taytay", "antipolo");
        ProfileFacts.Towns(worker, null).Should().Equal("taytay", "angono", "antipolo");
        ProfileFacts.Towns(worker, "cainta").Should().Equal("taytay", "angono", "antipolo");
    }

    [Fact]
    public void PlaceLines_ReadLikeAirbnb()
    {
        var text = English();
        var worker = Worker(cities: ["taytay", "angono", "antipolo"]);
        var towns = ProfileFacts.Towns(worker, "angono");

        text.TradeIn(worker, towns).Should().Be("Aircon Technician in Angono");
        text.AlsoGoesTo(towns).Should().Be("Also goes to Taytay · Antipolo");
        text.AlsoGoesTo(["angono"]).Should().BeNull("one town needs no second line");
        text.TradeIn(worker, []).Should().Be("Aircon Technician");
    }

    [Fact]
    public void Jobs_CarryTheirIdForThePicture()
    {
        var text = English();
        var worker = TestData.Listing("Ana", services: ["aircon-cleaning", "nope"]);

        text.Jobs(worker).Should().Equal(("aircon-cleaning", "Aircon cleaning"));
        text.JobLabels(worker).Should().Equal("Aircon cleaning");
    }

    [Theory]
    [InlineData("Juan Dela Cruz", "Juan")]
    [InlineData("  Ana  ", "Ana")]
    [InlineData("Ma. Teresa Santos", "Ma. Teresa")]
    [InlineData("", "")]
    [InlineData("S9 Quote Aircon", "S9 Quote Aircon")]
    [InlineData("juan dela cruz", "juan dela cruz")]
    [InlineData("JR's  Aircon", "JR's Aircon")]
    public void FirstName_ForTheHeadings(string displayName, string expected) =>
        ProfileFacts.FirstName(displayName).Should().Be(expected);

    [Fact]
    public void ReviewRow_WordsFirst_AtMostSix()
    {
        var reviews = new[]
        {
            ReviewOf("a", 5, null),
            ReviewOf("b", 4, "Mabilis at malinis"),
            ReviewOf("c", 5, "  "),
            ReviewOf("d", 3, "Okay naman"),
            ReviewOf("e", 5, "1"), ReviewOf("f", 5, "2"), ReviewOf("g", 5, "3"), ReviewOf("h", 5, "4"),
        };

        ProfileFacts.InRow(reviews).Select(r => r.Id).Should().Equal("b", "d", "e", "f", "g", "h");
        ProfileFacts.InRow(reviews[..3]).Select(r => r.Id).Should().Equal("b", "a", "c");
    }

    [Fact]
    public void Summary_AverageAndCount_OrNullWhenNone()
    {
        ProfileFacts.Summary([]).Should().BeNull();
        ProfileFacts.Summary([ReviewOf("a", 5, null), ReviewOf("b", 4, null)]).Should().Be(new ReviewSummary(4.5, 2));
    }
}
