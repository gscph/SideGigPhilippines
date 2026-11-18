using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Seeker;

/// <summary>
/// Finding a Skilled Worker is the whole point for a Seeker. These follow the Seeker home and results pages: typing
/// in the search box, tapping a trade or a town, and the search that runs from those answers (saved, so it
/// survives Back and an app restart).
/// </summary>
public class SeekerSearchTests
{
    [Fact]
    public async Task Load_SignedInSeeker_UsesTheirUidAndShortName()
    {
        var app = await FakeApp.SeekerAsync();

        app.Seeker.MyId.Should().Be(TestData.SeekerUid);
        app.Seeker.Answers.ReviewerName.Should().Be("Maricel C.", "reviews show a short name, from the account");
    }

    [Fact]
    public async Task Load_NoSignInProvider_UsesTheLocalId()
    {
        var app = new FakeApp(TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()), user: null);

        (await app.Seeker.EnsureLoadedAsync()).Should().BeTrue();

        app.Seeker.MyId.Should().Be(SeekerWorkspace.LocalSeekerId);
    }

    [Fact]
    public async Task Load_SomeoneWhoIsNotASeeker_IsSentBack()
    {
        var app = new FakeApp(TestData.FinishedPro(TestData.ReadyDraft()), TestData.Worker());

        (await app.Seeker.EnsureLoadedAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Load_SeekerMidInterview_IsSentBack_AndLoadsOnceFinished()
    {
        var state = TestData.FinishedSeeker(TestData.AirconCleaningInTaytay());
        state.CompletedAt = null;
        var app = new FakeApp(state, TestData.SeekerUser());

        (await app.Seeker.EnsureLoadedAsync()).Should().BeFalse();

        state.CompletedAt = DateTimeOffset.Now;
        (await app.Seeker.EnsureLoadedAsync()).Should().BeTrue("a finished interview loads on the next try");
    }

    [Fact]
    public async Task SearchFor_KnownWords_SetsTradeAndJob_AndSavesThem()
    {
        var app = await FakeApp.SeekerAsync();

        var intent = await app.Seeker.SearchForAsync("  barado ang lababo  ");

        intent.CategoryId.Should().Be("construction");
        app.Seeker.Answers.CategoryId.Should().Be("construction");
        app.Seeker.Answers.ServiceId.Should().Be("construction-plumbing");
        app.Seeker.Answers.SearchText.Should().Be("barado ang lababo");
        app.Seeker.CurrentIntent.CategoryId.Should().Be("construction");
        app.Store.Saves.Should().Be(1);
    }

    [Fact]
    public async Task SearchFor_UnknownWords_KeepsThePreviousJob()
    {
        var app = await FakeApp.SeekerAsync();

        var intent = await app.Seeker.SearchForAsync("xyzzy");

        intent.Matched.Should().BeFalse();
        app.Seeker.Answers.ServiceId.Should().Be("aircon-cleaning");
    }

    [Fact]
    public async Task SearchFor_Blank_ClearsTheSavedWords()
    {
        var app = await FakeApp.SeekerAsync();
        await app.Seeker.SearchForAsync("tubero");

        await app.Seeker.SearchForAsync("   ");

        app.Seeker.Answers.SearchText.Should().BeNull();
        app.Seeker.CurrentIntent.Should().Be(SearchIntent.None);
    }

    [Fact]
    public async Task PickCategory_ShowsAllJobsOfTheTrade_AndForgetsTypedWords()
    {
        var app = await FakeApp.SeekerAsync();
        await app.Seeker.SearchForAsync("aircon leak");

        await app.Seeker.PickCategoryAsync("construction");

        app.Seeker.Request.Should().Be(new SearchRequest("construction", null, "taytay", "500-1000"));
        app.Seeker.Answers.SearchText.Should().BeNull();
    }

    [Fact]
    public async Task PickService_PicksTheJob_AndForgetsTypedWords()
    {
        var app = await FakeApp.SeekerAsync();
        await app.Seeker.SearchForAsync("aircon");

        await app.Seeker.PickServiceAsync("aircon-freon");

        app.Seeker.Answers.ServiceId.Should().Be("aircon-freon");
        app.Seeker.Answers.SearchText.Should().BeNull();
    }

    [Fact]
    public async Task PickTownAndBudget_AreSaved()
    {
        var app = await FakeApp.SeekerAsync();

        await app.Seeker.PickTownAsync("antipolo");
        await app.Seeker.PickBudgetAsync("unsure");

        app.Seeker.Answers.CityId.Should().Be("antipolo");
        app.Seeker.Answers.AreaId.Should().Be(ReferenceData.ServiceAreaId);
        app.Seeker.Answers.BudgetId.Should().Be("unsure");
        app.Store.Saves.Should().Be(2);
    }

    private static SearchHit Hit(string name) => new(TestData.Listing(name), null, true, true, true, false);

    [Fact]
    public async Task PickUrgency_IsSaved_AndWordsTheFirstMessage()
    {
        var app = await FakeApp.SeekerAsync();

        await app.Seeker.PickUrgencyAsync(ReferenceData.UrgencyThisWeek);

        app.Store.State.Seeker.UrgencyId.Should().Be(ReferenceData.UrgencyThisWeek);
        app.Store.Saves.Should().Be(1);
        app.Profiles.Searches.Should().BeEmpty("when they need it doesn't change the results");
    }

    [Fact]
    public async Task Search_SendsTheWholeSearchToTheServer_AndShowsItsFirstPage()
    {
        var app = await FakeApp.SeekerAsync();
        app.Profiles.SearchResults = new SearchResult([Hit("Local Lito")], [Hit("Nearby Nora")], 3, 5);

        var result = await app.Seeker.SearchAsync();

        app.Profiles.Searches.Should().ContainSingle().Which.Should().Be(
            (new ProfessionalSearchQuery("aircon", ReferenceData.ServiceAreaId, "aircon-cleaning", "taytay", "500-1000"), 0));
        result.InTown.Select(h => h.Pro.DisplayName).Should().Equal("Local Lito");
        result.OtherTowns.Select(h => h.Pro.DisplayName).Should().Equal("Nearby Nora");
        result.HasMore.Should().BeTrue("2 of 8 are shown");
    }

    [Fact]
    public async Task MoreResults_AsksForThePageAfterWhatIsShown_AndAddsItUnder()
    {
        var app = await FakeApp.SeekerAsync();
        var shown = new SearchResult([Hit("Ana"), Hit("Bea")], [], 3, 0);
        app.Profiles.SearchResults = new SearchResult([Hit("Carlo")], [], 3, 0);

        var more = await app.Seeker.MoreResultsAsync(shown);

        app.Profiles.Searches.Should().ContainSingle().Which.Skip.Should().Be(2);
        more.InTown.Select(h => h.Pro.DisplayName).Should().Equal("Ana", "Bea", "Carlo");
        more.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task MoreResults_EverythingShown_DoesNotAskTheServer()
    {
        var app = await FakeApp.SeekerAsync();
        var shown = new SearchResult([Hit("Ana")], [], 1, 0);

        (await app.Seeker.MoreResultsAsync(shown)).Should().BeSameAs(shown);
        app.Profiles.Searches.Should().BeEmpty();
    }

    [Fact]
    public async Task MoreResults_PageDidNotLoad_KeepsWhatIsShown()
    {
        // Offline: the adapter answers an empty page. The results stay, and the next scroll tries again.
        var app = await FakeApp.SeekerAsync();
        var shown = new SearchResult([Hit("Ana")], [], 5, 0);

        (await app.Seeker.MoreResultsAsync(shown)).Should().BeSameAs(shown);
    }

    [Fact]
    public async Task Search_NoTradeYet_IsEmptyWithoutAskingTheServer()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.CategoryId = null;
        var app = await FakeApp.SeekerAsync(answers);

        var result = await app.Seeker.SearchAsync();

        result.Should().Be(SearchResult.Empty);
        app.Profiles.Searches.Should().BeEmpty();
    }

    [Fact]
    public async Task ProfileAndReviews_ComeFromTheServer()
    {
        var app = await FakeApp.SeekerAsync();
        var pro = TestData.Listing("Ana");
        app.Profiles.ById[pro.Id] = pro;
        app.Reviews.AboutPro = [new Review("x", pro.Id, "s", "Ben", 5, null, null, DateTimeOffset.Now)];

        (await app.Seeker.GetProAsync(pro.Id)).Should().BeSameAs(pro);
        (await app.Seeker.GetReviewsAsync(pro.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task Forget_DropsEverything_SoTheNextLoadReadsFresh()
    {
        var app = await FakeApp.SeekerAsync(before: a => a.Chats.Inbox = [TestData.ChatWith("ana", "ana")]);
        app.Profiles.ById["ana"] = TestData.Listing("Ana");

        app.Seeker.Forget();

        app.Seeker.MyId.Should().Be(SeekerWorkspace.LocalSeekerId);
        app.Seeker.Threads.Should().BeEmpty();
        app.Seeker.MyReviews.Should().BeEmpty();
        app.Seeker.ProfilePhotoUrl.Should().BeNull();
        app.Seeker.Answers.CategoryId.Should().BeNull();
    }
}
