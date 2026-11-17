using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Offers on job posts end to end (the app's <see cref="JobBoard"/> → API → SQL Server), inDrive style: workers take
/// the household's budget or name another price; the household sees each offer with the worker's listing and picks
/// one, which closes the post.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class JobOfferTests(ApiTestHost api)
{
    private static string Tag() => $"zo{Guid.NewGuid():N}"[..10];

    private static JobPostDraft Draft(string tag, int budget = 800) => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-plumbing",
        CityId = "taytay",
        BudgetPesos = budget,
        Description = $"Barado ang lababo sa kusina {tag}",
    };

    private static async Task<JobPost> PostAsync(TestPhone household, string tag)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(Draft(tag))).Should().Be(JobPostResult.Posted);
        return board.Mine[0];
    }

    private static async Task<TestPhone> PlumberAsync(ApiTestHost api, string name)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "construction", jobs: ["construction-plumbing"]);
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }

    /// <summary>The post as it sits in the worker's feed, or null when it isn't there.</summary>
    private static async Task<JobPost?> InFeedAsync(TestPhone worker, long postId)
    {
        var board = worker.Get<JobBoard>();
        await board.LoadFeedAsync();
        while (board.HasMore && board.Feed.All(p => p.Id != postId))
        {
            await board.LoadMoreFeedAsync();
        }

        return board.Feed.FirstOrDefault(p => p.Id == postId);
    }

    private static async Task<OfferResult> OfferAsync(TestPhone worker, long postId, int price, string? note = null) =>
        await InFeedAsync(worker, postId) is { } post
            ? await worker.Get<JobBoard>().SendOfferAsync(post, price, note)
            : OfferResult.Gone;

    private static async Task<JobPost> MineAsync(TestPhone household, long postId)
    {
        var board = household.Get<JobBoard>();
        await board.LoadMineAsync();
        return board.Mine.Single(p => p.Id == postId);
    }

    [IntegrationFact]
    public async Task AWorkerTakesTheBudget_AndTheHouseholdSeesTheOffer_WithTheirListing()
    {
        var household = await TestPhone.SeekerAsync(api, "Maricel");
        var plumber = await PlumberAsync(api, "Mang Ben");
        var post = await PostAsync(household, Tag());

        (await OfferAsync(plumber, post.Id, 800)).Should().Be(OfferResult.Done);

        plumber.Get<JobBoard>().Feed.Single(p => p.Id == post.Id).MyOfferPesos.Should().Be(800);
        (await InFeedAsync(plumber, post.Id))!.MyOfferPesos.Should().Be(800, "the feed says what the worker offered when read again");
        (await MineAsync(household, post.Id)).OfferCount.Should().Be(1);
        var offer = (await household.Get<JobBoard>().LoadOffersAsync(post.Id)).Should().ContainSingle().Subject;
        offer.Pro.Id.Should().Be(plumber.Uid);
        offer.Pro.DisplayName.Should().Be("Mang Ben");
        offer.PricePesos.Should().Be(800);
        offer.TakesBudget.Should().BeTrue();
        offer.IsPicked.Should().BeFalse();
        offer.DistanceKm.Should().Be(1, "both sit at Taytay's centre, and a distance is never shown under 1 km");
    }

    [IntegrationFact]
    public async Task AnotherPrice_ReplacesTheWorkersEarlierOffer_AndTheCheapestComesFirst()
    {
        var household = await TestPhone.SeekerAsync(api, "Lorna");
        var first = await PlumberAsync(api, "Una");
        var second = await PlumberAsync(api, "Pangalawa");
        var post = await PostAsync(household, Tag());

        await OfferAsync(first, post.Id, 800);
        await OfferAsync(second, post.Id, 700);
        (await OfferAsync(first, post.Id, 650, "  Puwede ako bukas ng umaga  ")).Should().Be(OfferResult.Done);

        var offers = await household.Get<JobBoard>().LoadOffersAsync(post.Id);
        offers.Select(o => (o.Pro.Id, o.PricePesos)).Should().Equal((first.Uid, 650), (second.Uid, 700));
        offers[0].Note.Should().Be("Puwede ako bukas ng umaga");
        offers[0].TakesBudget.Should().BeFalse();
        (await MineAsync(household, post.Id)).OfferCount.Should().Be(2, "one offer per worker, however often they change it");
    }

    [IntegrationFact]
    public async Task TakingAnOfferBack_HidesIt_AndSendingAgainBringsItBack()
    {
        var household = await TestPhone.SeekerAsync(api, "Nena");
        var plumber = await PlumberAsync(api, "Bawi");
        var post = await PostAsync(household, Tag());
        await OfferAsync(plumber, post.Id, 900);

        var seen = plumber.Get<JobBoard>().Feed.Single(p => p.Id == post.Id);
        (await plumber.Get<JobBoard>().WithdrawOfferAsync(seen)).Should().Be(OfferResult.Done);

        (await household.Get<JobBoard>().LoadOffersAsync(post.Id)).Should().BeEmpty();
        (await MineAsync(household, post.Id)).OfferCount.Should().Be(0);
        (await InFeedAsync(plumber, post.Id))!.MyOfferPesos.Should().BeNull();

        (await OfferAsync(plumber, post.Id, 850)).Should().Be(OfferResult.Done);
        (await household.Get<JobBoard>().LoadOffersAsync(post.Id)).Single().PricePesos.Should().Be(850);
    }

    [IntegrationFact]
    public async Task PickingAnOffer_ClosesThePost_AndNothingMoreCanBeOfferedOrPicked()
    {
        var household = await TestPhone.SeekerAsync(api, "Pili");
        var picked = await PlumberAsync(api, "Napili");
        var other = await PlumberAsync(api, "Hindi");
        var post = await PostAsync(household, Tag());
        await OfferAsync(picked, post.Id, 750);
        await OfferAsync(other, post.Id, 700);
        var offers = await household.Get<JobBoard>().LoadOffersAsync(post.Id);
        var otherSeen = (await InFeedAsync(other, post.Id))!;

        (await household.Get<JobBoard>().PickAsync(offers.Single(o => o.Pro.Id == picked.Uid))).Should().Be(OfferResult.Done);

        var mine = await MineAsync(household, post.Id);
        mine.IsOpen.Should().BeFalse();
        mine.PickedProId.Should().Be(picked.Uid);
        (await InFeedAsync(other, post.Id)).Should().BeNull("a post with a picked offer leaves every feed");
        (await other.Get<JobBoard>().SendOfferAsync(otherSeen, 600, null)).Should().Be(OfferResult.Gone);
        var after = await household.Get<JobBoard>().LoadOffersAsync(post.Id);
        after.Select(o => (o.Pro.Id, o.IsPicked)).Should().Equal((picked.Uid, true), (other.Uid, false));
        (await household.Get<JobBoard>().PickAsync(after[1])).Should().Be(OfferResult.Gone, "one pick per post");
    }

    [IntegrationFact]
    public async Task OnlyTheHousehold_SeesOrPicksTheOffersOnItsPost_AndOnlyWorkersOfThatTradeOffer()
    {
        var household = await TestPhone.SeekerAsync(api, "May-ari");
        var stranger = await TestPhone.SeekerAsync(api, "Iba");
        var plumber = await PlumberAsync(api, "Tubero");
        var mechanic = await TestPhone.WorkerReadyToPublishAsync(api, "Mekaniko", trade: "car_mechanic", jobs: ["mechanic-engine"]);
        await mechanic.Pro.PublishAsync();
        var post = await PostAsync(household, Tag());
        await OfferAsync(plumber, post.Id, 800);
        var offerId = (await household.Get<JobBoard>().LoadOffersAsync(post.Id)).Single().Id;

        var asStranger = api.Client(api.Tokens.For(stranger.Uid));
        (await asStranger.GetAsync($"job-posts/{post.Id}/offers")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await asStranger.PostAsync($"job-posts/{post.Id}/offers/{offerId}/pick", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var asMechanic = api.Client(api.Tokens.For(mechanic.Uid, mechanic.PhoneNumber));
        (await asMechanic.PostAsJsonAsync($"job-posts/{post.Id}/offers", new SendOfferRequest(800), ApiJson.Default.SendOfferRequest))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "another trade");
        (await MineAsync(household, post.Id)).IsOpen.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task ABadPriceOrALongNote_IsRefused_NamingTheField()
    {
        var household = await TestPhone.SeekerAsync(api, "Mali");
        var plumber = await PlumberAsync(api, "Tubero");
        var post = await PostAsync(household, Tag());
        var client = api.Client(api.Tokens.For(plumber.Uid, plumber.PhoneNumber));
        (SendOfferRequest Request, string Error)[] cases =
        [
            (new(5), "invalid-pricePesos"),
            (new(2_000_000), "invalid-pricePesos"),
            (new(800, new string('a', JobOfferRules.MaxNote + 1)), "invalid-note"),
        ];

        foreach (var (request, error) in cases)
        {
            using var response = await client.PostAsJsonAsync($"job-posts/{post.Id}/offers", request, ApiJson.Default.SendOfferRequest);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, error);
            (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(error);
        }
    }

    [IntegrationFact]
    public async Task Numbers_StayPrivateUntilAPick_ThenBothSidesGetTheOthers()
    {
        var household = await TestPhone.SeekerAsync(api, "Lihim");
        var picked = await PlumberAsync(api, "Napili");
        var other = await PlumberAsync(api, "Iba");
        var post = await PostAsync(household, Tag());
        await OfferAsync(picked, post.Id, 750);
        await OfferAsync(other, post.Id, 700);

        var board = household.Get<JobBoard>();
        (await household.Seeker.GetProAsync(picked.Uid))!.MobileNumber.Should().BeEmpty("profiles never carry the number");
        (await board.ContactNumberAsync(picked.Uid)).Should().BeNull("no pick yet: they chat in the app");
        (await picked.Get<JobBoard>().ContactNumberAsync(household.Uid)).Should().BeNull();

        var offers = await board.LoadOffersAsync(post.Id);
        (await board.PickAsync(offers.Single(o => o.Pro.Id == picked.Uid))).Should().Be(OfferResult.Done);

        (await board.ContactNumberAsync(picked.Uid)).Should().Be(picked.PhoneNumber);
        (await picked.Get<JobBoard>().ContactNumberAsync(household.Uid)).Should().BeNull("this household signed in without a number");
        // A household signed in with its phone: the account's number is the token's.
        using (var saved = await api.Client(api.Tokens.For(household.Uid, "+639170000417")).PutAsJsonAsync("users/me",
                   new UserDto(household.Uid, "Lihim", null, null, Wire.English, Wire.Seeker, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                   ApiJson.Default.UserDto))
        {
            saved.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await picked.Get<JobBoard>().ContactNumberAsync(household.Uid)).Should().Be("+639170000417");
        (await board.ContactNumberAsync(other.Uid)).Should().BeNull("only the picked worker");
        (await other.Get<JobBoard>().ContactNumberAsync(household.Uid)).Should().BeNull();
        (await other.Get<IProfessionalProfileRepository>().GetByIdAsync(other.Uid))!.MobileNumber
            .Should().Be(other.PhoneNumber, "a worker still sees their own number");

        await household.Seeker.BlockAsync(picked.Uid);
        (await picked.Get<JobBoard>().ContactNumberAsync(household.Uid)).Should().BeNull("a block hides the number again");
    }

    [IntegrationFact]
    public async Task ABlock_HidesTheOffer_AndStopsNewOnes()
    {
        var household = await TestPhone.SeekerAsync(api, "Ayaw");
        var plumber = await PlumberAsync(api, "Tubero");
        var post = await PostAsync(household, Tag());
        await OfferAsync(plumber, post.Id, 800);

        await household.Seeker.BlockAsync(plumber.Uid);

        (await household.Get<JobBoard>().LoadOffersAsync(post.Id)).Should().BeEmpty();
        var client = api.Client(api.Tokens.For(plumber.Uid, plumber.PhoneNumber));
        (await client.PostAsJsonAsync($"job-posts/{post.Id}/offers", new SendOfferRequest(700), ApiJson.Default.SendOfferRequest))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [IntegrationFact]
    public async Task DeleteAccount_RemovesTheWorkersOffers()
    {
        var household = await TestPhone.SeekerAsync(api, "Naghihintay");
        var plumber = await PlumberAsync(api, "Aalis");
        var post = await PostAsync(household, Tag());
        await OfferAsync(plumber, post.Id, 800);

        await plumber.Account.DeleteAccountAsync();

        (await household.Get<JobBoard>().LoadOffersAsync(post.Id)).Should().BeEmpty();
        using var scope = api.Server.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().JobOffers.AnyAsync(o => o.ProUid == plumber.Uid)).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task MyOffers_SaysWhoWasPicked_AndOpensThePickedWorkersChat()
    {
        // Before this list, a worker who wasn't picked just saw the post leave the feed.
        var household = await TestPhone.SeekerAsync(api, "Tess");
        var picked = await PlumberAsync(api, "Napili");
        var other = await PlumberAsync(api, "Iba");
        var post = await PostAsync(household, Tag());
        var second = await PostAsync(household, Tag());
        await OfferAsync(picked, post.Id, 700, "Kaya bukas");
        await OfferAsync(other, post.Id, 900);
        await OfferAsync(other, second.Id, 600);

        var waiting = await other.Get<JobBoard>().MyOffersAsync();
        waiting.Select(o => (o.Post.Id, o.State)).Should().BeEquivalentTo([(post.Id, MyOfferState.Waiting), (second.Id, MyOfferState.Waiting)]);

        var offers = await household.Get<JobBoard>().LoadOffersAsync(post.Id);
        (await household.Get<JobBoard>().PickAsync(offers.Single(o => o.Pro.Id == picked.Uid))).Should().Be(OfferResult.Done);

        var mine = (await picked.Get<JobBoard>().MyOffersAsync()).Should().ContainSingle().Subject;
        mine.State.Should().Be(MyOfferState.Picked);
        mine.PricePesos.Should().Be(700);
        mine.Note.Should().Be("Kaya bukas");
        mine.ChatId.Should().Be(Chat.IdFor(picked.Uid, household.Uid));

        var theirs = await other.Get<JobBoard>().MyOffersAsync();
        theirs.Single(o => o.Post.Id == post.Id).State.Should().Be(MyOfferState.NotPicked);
        theirs.Single(o => o.Post.Id == post.Id).ChatId.Should().BeNull();
        theirs.Single(o => o.Post.Id == second.Id).State.Should().Be(MyOfferState.Waiting);
        theirs[0].Post.Id.Should().Be(second.Id, "newest change first");

        (await other.Get<JobBoard>().WithdrawOfferAsync(theirs.Single(o => o.Post.Id == second.Id).Post)).Should().Be(OfferResult.Done);
        (await other.Get<JobBoard>().MyOffersAsync()).Should().ContainSingle(o => o.Post.Id == post.Id, "an offer taken back leaves the list");
    }
}
