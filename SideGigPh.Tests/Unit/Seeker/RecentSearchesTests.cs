using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Seeker;

/// <summary>
/// Seeker home's recent searches: kept only on the phone (the onboarding file), newest first, at most three, the same
/// work in the same town only once. One tap makes one the current search, for the results or the post form.
/// </summary>
public class RecentSearchesTests
{
    [Fact]
    public async Task Search_PutsTheSearchOnTop_AndSavesItOnThePhone()
    {
        var app = await FakeApp.SeekerAsync();

        await app.Seeker.SearchAsync();

        app.Seeker.RecentSearches.Should().ContainSingle().Which.Should().Be(
            new RecentSearch("aircon", "aircon-cleaning", null, "taytay", "500-1000"));
        app.Store.State.Seeker.RecentSearches.Should().HaveCount(1, "it is in the saved file, not only in memory");
    }

    [Fact]
    public async Task Search_NoTradeYet_RemembersNothing()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.CategoryId = null;
        var app = await FakeApp.SeekerAsync(answers);

        await app.Seeker.SearchAsync();

        app.Seeker.RecentSearches.Should().BeEmpty();
    }

    [Fact]
    public void SameWorkAndTown_IsKeptOnce_WithTheNewBudget()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.RememberCurrent();
        answers.BudgetId = "1000-2000";

        answers.RememberCurrent();

        answers.RecentSearches.Should().ContainSingle().Which.BudgetId.Should().Be("1000-2000");
    }

    [Fact]
    public void OnlyTheNewestThree_AreKept_NewestFirst()
    {
        var answers = TestData.AirconCleaningInTaytay();
        foreach (var service in new[] { "aircon-cleaning", "aircon-repair", "aircon-leak", "aircon-freon" })
        {
            answers.ServiceId = service;
            answers.RememberCurrent();
        }

        answers.RecentSearches.Select(recent => recent.ServiceId).Should().Equal("aircon-freon", "aircon-leak", "aircon-repair");
    }

    [Fact]
    public void Use_MakesItTheCurrentSearch_ForgetsTypedWords_AndMovesItToTheTop()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.RememberCurrent();
        var plumbing = new RecentSearch("plumbing", "plumbing-leak", null, "angono", "800-1500");
        answers.RecentSearches.Add(plumbing);
        answers.SearchText = "aircon";

        answers.Use(plumbing);

        (answers.CategoryId, answers.ServiceId, answers.CityId, answers.AreaId, answers.BudgetId)
            .Should().Be(("plumbing", "plumbing-leak", "angono", ReferenceData.ServiceAreaId, "800-1500"));
        answers.SearchText.Should().BeNull();
        answers.RecentSearches[0].Should().Be(plumbing);
    }

    [Fact]
    public void RememberPost_KeepsItsWorkTownAndBudget()
    {
        var answers = new SeekerAnswers();
        var post = new JobPostDraft { CategoryId = "plumbing", ServiceId = "plumbing-leak", CityId = "angono", BudgetPesos = 1500 };

        answers.RememberPost(post);
        post.BudgetMinPesos = 800;
        post.CityId = "taytay";
        answers.RememberPost(post);

        answers.RecentSearches.Select(recent => (recent.CityId, recent.BudgetId))
            .Should().Equal(("taytay", "800-1500"), ("angono", "under-1500"));
    }

    [Fact]
    public void RememberPost_OthersTrade_KeepsWhatTheyTyped()
    {
        var answers = new SeekerAnswers();

        answers.RememberPost(new JobPostDraft
        {
            CategoryId = ReferenceData.OtherTradeId, ServiceId = ReferenceData.OtherTradeId, CityId = "taytay",
            Description = "Pool cleaning", BudgetPesos = 2000,
        });

        answers.RecentSearches.Should().ContainSingle().Which.OtherJobText.Should().Be("Pool cleaning");
    }

    [Fact]
    public void Shown_HidesARetiredTradeOrAnUnservedTown()
    {
        var answers = new SeekerAnswers
        {
            RecentSearches =
            [
                new RecentSearch("home_massage", null, null, "taytay", null),
                new RecentSearch("aircon", "aircon-cleaning", null, "makati", null),
                new RecentSearch("aircon", "aircon-cleaning", null, "taytay", null),
            ],
        };

        answers.Shown().Should().ContainSingle().Which.CityId.Should().Be("taytay");
    }
}
