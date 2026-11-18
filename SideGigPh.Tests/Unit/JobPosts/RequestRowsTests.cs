using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// A worker's short request rows and the request's own page: how each request stands (new, offered, picked, not
/// picked), the words each line prints, and finding a request for its page from the feed or the worker's own offers.
/// </summary>
public class RequestRowsTests
{
    private readonly AppText _text = new();
    private readonly FakeJobPosts _posts = new();
    private readonly JobBoard _board;

    public RequestRowsTests() => _board = new JobBoard(_posts, new FakePhotoStore());

    private static JobPost Post(long id, int? myOffer = null, int? km = 3, string? barangay = null) =>
        new(id, "construction", "construction-plumbing", "Barado ang lababo sa kusina", 1500, "angono", [], IsOpen: true, Mine: false,
            OffersService: true, DistanceKm: km, DateTimeOffset.Now.AddMinutes(-5), MyOfferPesos: myOffer, BarangayId: barangay);

    private static MyOffer Mine(long id, MyOfferState state, int price = 1000, string? note = null, string? chat = null) =>
        new(Post(id, myOffer: state == MyOfferState.Waiting ? price : null), price, note, state, DateTimeOffset.Now, chat);

    [Fact]
    public void AFeedPost_IsNew_UntilTheWorkerOffers()
    {
        var fresh = RequestItem.From(Post(1));
        fresh.State.Should().Be(RequestState.New);
        fresh.CanOffer.Should().BeTrue();
        fresh.HasMyOffer.Should().BeFalse();

        var offered = RequestItem.From(Post(1, myOffer: 1200));
        offered.State.Should().Be(RequestState.Offered);
        offered.MyPricePesos.Should().Be(1200);
        offered.HasMyOffer.Should().BeTrue();
    }

    [Fact]
    public void AnOwnOffer_StandsAsTheCustomerDecided()
    {
        RequestItem.From(Mine(1, MyOfferState.Waiting)).State.Should().Be(RequestState.Offered);

        var picked = RequestItem.From(Mine(1, MyOfferState.Picked, chat: "pro_seeker"));
        picked.State.Should().Be(RequestState.Picked);
        picked.ChatId.Should().Be("pro_seeker");
        picked.CanOffer.Should().BeFalse("the post closed when the customer picked");

        RequestItem.From(Mine(1, MyOfferState.NotPicked)).CanOffer.Should().BeFalse();
        RequestItem.From(Mine(1, MyOfferState.Waiting, note: "  ")).MyNote.Should().BeNull("a blank note isn't shown");
    }

    [Fact]
    public void Offering_ThenTakingBack_MovesTheRequestBackAndForth()
    {
        var offered = RequestItem.From(Post(1)).Offered(900, "  Bukas ng umaga ");
        offered.State.Should().Be(RequestState.Offered);
        offered.MyPricePesos.Should().Be(900);
        offered.MyNote.Should().Be("Bukas ng umaga");
        offered.Post.MyOfferPesos.Should().Be(900);

        var back = offered.Withdrawn();
        back.State.Should().Be(RequestState.New);
        back.MyPricePesos.Should().BeNull();
        back.MyNote.Should().BeNull();
        back.Post.MyOfferPesos.Should().BeNull();
    }

    [Fact]
    public void TheRow_NamesAStateOnlyWhenThereIsOne()
    {
        _text.RequestStatus(RequestItem.From(Post(1))).Should().BeNull("a new request has no state");
        _text.RequestTimeLine(RequestItem.From(Post(1))).Should().Be("5 min ago");

        _text.RequestTimeLine(RequestItem.From(Post(1, myOffer: 1000))).Should().Be("5 min ago · You offered ₱1,000");
        _text.RequestStatus(RequestItem.From(Mine(1, MyOfferState.Picked))).Should().Be("Picked you");
        _text.RequestStatus(RequestItem.From(Mine(1, MyOfferState.NotPicked))).Should().Be("Not picked");
    }

    [Fact]
    public void TheRowsButton_SaysOffer_ThenMyOffer()
    {
        _text.RequestAction(RequestItem.From(Post(1))).Should().Be("Offer");
        _text.RequestAction(RequestItem.From(Post(1, myOffer: 800))).Should().Be("My offer");
        _text.RequestAction(RequestItem.From(Mine(1, MyOfferState.NotPicked))).Should().Be("My offer");
    }

    [Fact]
    public void ThePlace_JoinsTownBarangayAndDistance()
    {
        _text.RequestPlace(Post(1, km: null)).Should().Be("Angono");
        _text.RequestPlace(Post(1, km: 3)).Should().Be("Angono · 3 km away");
        _text.RequestPlace(Post(1, km: 3, barangay: "angono-san-isidro")).Should().StartWith("Angono · ").And.EndWith(" · 3 km away");
        _text.ServiceLabel(Post(1)).Should().NotBe("construction-plumbing", "the service's own label shows");
    }

    [Fact]
    public async Task ARequestsPage_ReadsTheFeedFirst_ThenTheWorkersOwnOffers()
    {
        _posts.Seed(Post(1), Post(2, myOffer: 700));
        _posts.MyOffers.Add(Mine(2, MyOfferState.Waiting, price: 700, note: "Kaya ko bukas"));
        _posts.MyOffers.Add(Mine(9, MyOfferState.Picked, chat: "chat-9"));

        (await _board.FindRequestAsync(1))!.State.Should().Be(RequestState.New);
        (await _board.FindRequestAsync(9))!.State.Should().Be(RequestState.Picked, "a closed post comes from the worker's offers");
        var offered = await _board.FindRequestAsync(2);
        offered!.State.Should().Be(RequestState.Offered);
        offered.MyNote.Should().Be("Kaya ko bukas", "the feed has no note, the worker's offer does");
        (await _board.FindRequestAsync(404)).Should().BeNull("no longer open to this worker");
    }

    [Fact]
    public async Task ALoadedRequest_IsFoundWithoutAskingTheServerAgain()
    {
        _posts.Seed(Post(1));
        await _board.LoadFeedAsync();
        _posts.HasListing = false; // a read now would come back empty

        (await _board.FindRequestAsync(1)).Should().NotBeNull();
        _board.FindLoadedRequest(2).Should().BeNull();
    }

    [Fact]
    public async Task ChangingOrTakingBackAnOffer_UpdatesTheWorkersLoadedOffers()
    {
        _posts.MyOffers.Add(Mine(5, MyOfferState.Waiting, price: 700));
        await _board.MyOffersAsync();

        var item = _board.FindLoadedRequest(5)!;
        (await _board.SendOfferAsync(item.Post, 650, "Mas mura")).Should().Be(OfferResult.Done);
        _board.FindLoadedRequest(5)!.MyPricePesos.Should().Be(650);
        _board.FindLoadedRequest(5)!.MyNote.Should().Be("Mas mura");

        (await _board.WithdrawOfferAsync(item.Post)).Should().Be(OfferResult.Done);
        _board.FindLoadedRequest(5).Should().BeNull("the offer was taken back and the post isn't in the feed");
    }

    [Fact]
    public void ARequestsPage_SitsUnderTheRequestsTab() =>
        ProRoutes.Request(42).Should().Be("/professional/requests/42").And.StartWith(ProRoutes.Requests);
}
