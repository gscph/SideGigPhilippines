using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Rude or sexual words in a job post (new or edited) or an offer's note, end to end (the app's <see cref="JobBoard"/>
/// → API → SQL Server): the server answers <c>422 text-not-allowed</c> before saving, nothing is stored, nobody is rung,
/// and the app keeps what the person typed so they can fix it. The word list ("gago") and the outside service
/// (<see cref="FakeTextModerator.Marker"/>) both refuse. Masons, like <see cref="JobAlertTests"/>: one more never
/// crowds out its workers.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class JobPostModerationTests(ApiTestHost api)
{
    private static JobPostDraft Draft(string description) => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-masonry",
        CityId = "taytay",
        BudgetPesos = 1500,
        Description = description,
    };

    private static string Tag() => $"zm{Guid.NewGuid():N}"[..10];

    private async Task<TestPhone> MasonAsync(string name)
    {
        var mason = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "construction", jobs: ["construction-masonry"]);
        (await mason.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        await mason.Get<ChatAlerts>().RegisterAsync();
        return mason;
    }

    private static async Task<JobPost> InFeedAsync(TestPhone worker, long postId)
    {
        var board = worker.Get<JobBoard>();
        await board.LoadFeedAsync();
        while (board.HasMore && board.Feed.All(p => p.Id != postId))
        {
            await board.LoadMoreFeedAsync();
        }

        return board.Feed.Single(p => p.Id == postId);
    }

    [IntegrationTheory]
    [InlineData("May bitak ang pader, gago ka")]
    [InlineData("May bitak ang pader " + FakeTextModerator.Marker)]
    public async Task ARudePost_IsRefused_NothingIsSaved_AndNoWorkerIsRung(string description)
    {
        var mason = await MasonAsync("Berto");
        var household = await TestPhone.SeekerAsync(api, "Lorna");
        var board = household.Get<JobBoard>();
        var draft = Draft($"{description} {Tag()}");

        (await board.PostAsync(draft)).Should().Be(JobPostResult.TextNotAllowed);

        draft.Description.Should().StartWith(description, "the person fixes what they typed");
        await board.LoadMineAsync();
        board.Mine.Should().BeEmpty("a refused post is never saved");
        api.Pushes.To(mason.Push.Token!).Should().BeEmpty("a refused post rings nobody");
    }

    [IntegrationFact]
    public async Task ARudeEdit_IsRefused_AndThePostStaysAsItWas()
    {
        var household = await TestPhone.SeekerAsync(api, "Delia");
        var board = household.Get<JobBoard>();
        var tag = Tag();
        (await board.PostAsync(Draft($"May bitak ang pader sa sala {tag}"))).Should().Be(JobPostResult.Posted);
        var edit = JobBoard.EditDraft(board.Mine[0]);
        edit.Description = $"May bitak ang pader, gago ka {tag}";

        (await board.SaveEditAsync(edit)).Should().Be(JobPostResult.TextNotAllowed);

        await board.LoadMineAsync();
        board.Mine.Should().ContainSingle().Which.Description.Should().Be($"May bitak ang pader sa sala {tag}");
    }

    [IntegrationTheory]
    [InlineData("Kaya ko 'yan, gago")]
    [InlineData("Kaya ko 'yan " + FakeTextModerator.Marker)]
    public async Task ARudeOfferNote_IsRefused_NothingIsSaved_AndTheHouseholdIsNotRung(string note)
    {
        var mason = await MasonAsync("Carding");
        var household = await TestPhone.SeekerAsync(api, "Mila");
        await household.Get<ChatAlerts>().RegisterAsync();
        var households = household.Get<JobBoard>();
        (await households.PostAsync(Draft($"May bitak ang pader sa sala {Tag()}"))).Should().Be(JobPostResult.Posted);
        var post = households.Mine[0];

        (await mason.Get<JobBoard>().SendOfferAsync(await InFeedAsync(mason, post.Id), 1200, note)).Should().Be(OfferResult.TextNotAllowed);

        (await households.LoadOffersAsync(post.Id)).Should().BeEmpty("a refused offer is never saved");
        api.Pushes.To(household.Push.Token!).Should().BeEmpty("a refused offer rings nobody");
        (await InFeedAsync(mason, post.Id)).MyOfferPesos.Should().BeNull();
    }
}
