using SideGigPh.Api;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Job post notifications end to end (brief item 10): a new post rings the workers of its trade who travel that far,
/// and an offer rings the household. Masons only, a trade no other test publishes, so other tests' workers never
/// crowd out the ones here.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class JobAlertTests(ApiTestHost api)
{
    private static JobPostDraft Draft(string tag) => new()
    {
        CategoryId = "masonry_tiling",
        ServiceId = "masonry-crack",
        CityId = "taytay",
        BudgetPesos = 1500,
        Description = $"May bitak ang pader sa sala {tag}",
    };

    private static string Tag() => $"za{Guid.NewGuid():N}"[..10];

    /// <summary>A published mason with notifications on. One-word names: the fake push token carries the name.</summary>
    private async Task<TestPhone> MasonAsync(string name)
    {
        var mason = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "masonry_tiling", jobs: ["masonry-crack"]);
        (await mason.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        await mason.Get<ChatAlerts>().RegisterAsync();
        return mason;
    }

    private static async Task<JobPost> PostAsync(TestPhone household, JobPostDraft draft)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(draft)).Should().Be(JobPostResult.Posted);
        return board.Mine[0];
    }

    private IEnumerable<PushMessage> AboutPost(TestPhone phone, long postId) =>
        api.Pushes.To(phone.Push.Token!).Where(p => p.Data.GetValueOrDefault(PushData.PostId) == postId.ToString());

    [IntegrationFact]
    public async Task ANewPost_RingsTheWorkersOfItsTradeNearby_OnceEvenIfSentAgain()
    {
        var mason = await MasonAsync("Lito");
        var carpenter = await TestPhone.WorkerReadyToPublishAsync(api, "Karpintero", trade: "carpentry", jobs: ["carpentry-door-window"]);
        await carpenter.Pro.PublishAsync();
        await carpenter.Get<ChatAlerts>().RegisterAsync();
        var household = await TestPhone.SeekerAsync(api, "Neri");
        var draft = Draft(Tag());

        var post = await PostAsync(household, draft);
        await PostAsync(household, draft); // a retry answers with the same post

        var push = AboutPost(mason, post.Id).Should().ContainSingle("a retry doesn't ring again").Subject;
        push.Alert.Should().BeTrue("a request nearby is work: the worker must notice");
        push.AndroidChannel.Should().Be(PushData.RequestsChannel);
        push.Title.Should().Be("New request near you");
        push.Body.Should().StartWith("₱1,500 · May bitak ang pader");
        push.Tag.Should().Be($"post-{post.Id}");
        ChatAlerts.PathFor(push.Data).Should().Be("/professional/requests", "a tap opens the Requests feed");
        AboutPost(carpenter, post.Id).Should().BeEmpty("another trade");
    }

    [IntegrationFact]
    public async Task ABlockEitherWay_KeepsTheWorkerFromBeingRung()
    {
        var mason = await MasonAsync("Ising");
        var household = await TestPhone.SeekerAsync(api, "Ayaw");
        await household.Seeker.BlockAsync(mason.Uid);

        var post = await PostAsync(household, Draft(Tag()));

        AboutPost(mason, post.Id).Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task AnOffer_RingsTheHousehold_AndATapOpensTheOffers()
    {
        var mason = await MasonAsync("Delfin");
        var household = await TestPhone.SeekerAsync(api, "Rosa");
        await household.Get<ChatAlerts>().RegisterAsync();
        var post = await PostAsync(household, Draft(Tag()));
        var board = mason.Get<JobBoard>();
        await board.LoadFeedAsync();
        while (board.HasMore && board.Feed.All(p => p.Id != post.Id))
        {
            await board.LoadMoreFeedAsync();
        }

        (await board.SendOfferAsync(board.Feed.Single(p => p.Id == post.Id), 1200, null)).Should().Be(OfferResult.Done);

        var push = AboutPost(household, post.Id).Should().ContainSingle().Subject;
        push.Alert.Should().BeTrue("offers are what the household waits for");
        push.AndroidChannel.Should().Be(PushData.OffersChannel);
        push.Title.Should().Be("New offer: Delfin");
        push.Body.Should().Be("₱1,200 for your post");
        push.Tag.Should().Be($"offers-{post.Id}");
        ChatAlerts.PathFor(push.Data).Should().Be($"/posts/{post.Id}/offers");
    }
}
