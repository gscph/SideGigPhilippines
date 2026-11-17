using SideGigPh.AzureSQL;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// The job board's rules on the phone, without a server: when a post can be sent, how photos of the problem are
/// added, and that a failed call never breaks the screen or changes the lists.
/// </summary>
public class JobBoardTests
{
    private readonly FakeJobPosts _posts = new();
    private readonly FakePhotoStore _photos = new();
    private readonly JobBoard _board;

    public JobBoardTests() => _board = new JobBoard(_posts, _photos);

    private static JobPostDraft Complete() => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-plumbing",
        CityId = "taytay",
        BudgetPesos = 800,
        Description = "Barado ang lababo sa kusina",
    };

    private static PhotoPick Picked(params byte[] firstBytes) =>
        new(PhotoPickStatus.Picked, firstBytes.Select(b => new[] { b, (byte)0xD8 }).ToList());

    [Fact]
    public void ACompleteDraft_CanBePosted() => JobBoard.CanPost(Complete()).Should().BeTrue();

    [Fact]
    public void TheFirstMissingField_IsNamed_InTheOrderAsked()
    {
        JobBoard.FirstMissing(new JobPostDraft()).Should().Be(JobPostField.Trade);

        var draft = Complete();
        draft.ServiceId = "aircon-cleaning";
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Service, "a service of another trade");

        draft = Complete();
        draft.Description = "  Tulo   ";
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Description, "too short to say what is wrong");

        draft = Complete();
        draft.BudgetPesos = null;
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Budget);
        draft.BudgetPesos = 10;
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Budget, "under the smallest budget");

        draft = Complete();
        draft.CityId = null;
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Town);
    }

    [Fact]
    public void AGreyedOutPost_SaysWhatIsMissing_AndNothingOnceReady()
    {
        var text = new AppText();
        JobBoard.MissingHint(Complete(), text).Should().BeNull("the Post button works");

        var draft = Complete();
        draft.Description = "Sira";
        JobBoard.MissingHint(draft, text).Should().Be("Describe the problem in at least 10 letters.");

        draft = Complete();
        draft.BudgetPesos = 40;
        JobBoard.MissingHint(draft, text).Should().Contain("₱50 or more");

        draft = Complete();
        (draft.BudgetMinPesos, draft.BudgetPesos) = (1500, 1000);
        JobBoard.MissingHint(draft, text).Should().Contain("not less than", "\"from\" above \"up to\" is refused by the server too");

        JobBoard.MissingHint(new JobPostDraft(), text).Should().Be("Pick the kind of work.");
    }

    [Fact]
    public async Task Photos_AreUploadedAsPicked_UpToThree_WithoutRepeats()
    {
        var draft = Complete();

        var first = await _board.AddPhotosAsync(draft, Picked(1, 2, 1));
        first.Should().Be(new PhotoAddResult(Added: 2, Duplicates: 1));
        JobBoard.PhotoRoom(draft).Should().Be(1);

        var second = await _board.AddPhotosAsync(draft, Picked(3, 4));
        second.Added.Should().Be(1, "only one more fits");
        draft.Photos.Select(p => p.Id).Should().Equal(TestData.PhotoId(1), TestData.PhotoId(2), TestData.PhotoId(3));
        draft.Photos[0].Url.Should().Be(FakePhotoStore.Url(TestData.PhotoId(1)));
    }

    [Fact]
    public async Task APhotoThatDidNotUpload_IsReported_AndTheEarlierOnesStay()
    {
        var draft = Complete();
        await _board.AddPhotosAsync(draft, Picked(1));
        _photos.Refuse = new PhotoStoreException("offline");

        var result = await _board.AddPhotosAsync(draft, Picked(2));

        result.Problem.Should().Be(PhotoProblem.UploadFailed);
        draft.Photos.Should().ContainSingle();
    }

    [Fact]
    public async Task RemovingAPhoto_DeletesTheStoredCopy()
    {
        var draft = Complete();
        await _board.AddPhotosAsync(draft, Picked(1, 2));

        await _board.RemovePhotoAsync(draft, TestData.PhotoId(1));

        draft.Photos.Select(p => p.Id).Should().Equal(TestData.PhotoId(2));
        _photos.Deleted.Should().Equal(TestData.PhotoId(1));
    }

    [Fact]
    public async Task Posting_PutsThePostOnTopOfTheHouseholdsList()
    {
        var changes = 0;
        _board.Changed += () => changes++;
        await _board.PostAsync(Complete());
        var second = Complete();
        second.Description = "Tumutulo ang gripo sa banyo";

        (await _board.PostAsync(second)).Should().Be(JobPostResult.Posted);

        _board.Mine.Select(p => p.Description).Should().Equal("Tumutulo ang gripo sa banyo", "Barado ang lababo sa kusina");
        changes.Should().Be(2);
    }

    [Fact]
    public async Task AnIncompleteDraft_IsNeverSent()
    {
        (await _board.PostAsync(new JobPostDraft())).Should().Be(JobPostResult.Incomplete);

        _posts.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task Offline_NothingThrows_AndNothingChanges()
    {
        await _board.PostAsync(Complete());
        _posts.Seed(Post(50), Post(51));
        await _board.LoadFeedAsync();
        _posts.Fail = true;

        (await _board.PostAsync(Complete())).Should().Be(JobPostResult.Failed);
        (await _board.CloseAsync(_board.Mine[0])).Should().BeFalse();
        (await _board.ReportAsync(_board.Feed[0])).Should().BeFalse();

        _board.Mine.Should().ContainSingle().Which.IsOpen.Should().BeTrue();
        _board.Feed.Should().HaveCount(2);
    }

    [Fact]
    public async Task TakingDown_KeepsThePostInTheList_AsClosed()
    {
        await _board.PostAsync(Complete());

        (await _board.CloseAsync(_board.Mine[0])).Should().BeTrue();

        _board.Mine.Should().ContainSingle().Which.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task TheFeed_LoadsAPageAtATime_WithoutRepeats()
    {
        _posts.Seed([.. Enumerable.Range(1, 25).Select(i => Post(i))]);

        await _board.LoadFeedAsync();
        _board.Feed.Should().HaveCount(SideGigPh.Contracts.Paging.JobPosts);
        _board.HasMore.Should().BeTrue();

        await _board.LoadMoreFeedAsync();
        await _board.LoadMoreFeedAsync();
        await _board.LoadMoreFeedAsync();

        _board.Feed.Select(p => p.Id).Should().Equal(Enumerable.Range(1, 25).Select(i => (long)i));
        _board.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Reporting_TakesThePostOffTheFeed()
    {
        _posts.Seed(Post(1), Post(2));
        await _board.LoadFeedAsync();

        (await _board.ReportAsync(_board.Feed[0])).Should().BeTrue();

        _board.Feed.Select(p => p.Id).Should().Equal(2);
        _posts.Reported.Should().Equal(1);
    }

    [Fact]
    public async Task NoPublishedListing_IsPassedOn()
    {
        _posts.HasListing = false;

        await _board.LoadFeedAsync();

        _board.HasListing.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0.0, 1)]
    [InlineData(400.0, 1)]
    [InlineData(1499.0, 1)]
    [InlineData(1500.0, 2)]
    [InlineData(6400.0, 6)]
    public void ADistanceForOtherPeople_IsWholeKilometres_NeverUnderOne(double? meters, int? km) =>
        JobPostStore.DistanceKm(meters).Should().Be(km);

    private static JobPost Post(long id) =>
        new(id, "construction", "construction-plumbing", $"Barado ang lababo {id}", 800, "taytay", [], IsOpen: true, Mine: false, OffersService: true, DistanceKm: 2, DateTimeOffset.UnixEpoch);

    private static JobPost FeedPost(long id, int budget = 800) =>
        new(id, "construction", "construction-plumbing", "Barado ang lababo sa kusina", budget, "taytay", [], IsOpen: true, Mine: false, OffersService: true, DistanceKm: 2, DateTimeOffset.UnixEpoch);

    private static JobOffer Offer(long id, long postId, string proId = "pro-1", int price = 800) =>
        new(id, postId, TestData.Listing(proId), price, null, IsPicked: false, TakesBudget: price == 800, DistanceKm: 3, DateTimeOffset.UnixEpoch);

    [Fact]
    public void AnOffer_NeedsAPriceInRange_AndAShortNote()
    {
        JobBoard.CanOffer(800, null).Should().BeTrue();
        JobBoard.CanOffer(800, "Puwede ako bukas").Should().BeTrue();
        JobBoard.CanOffer(null, null).Should().BeFalse();
        JobBoard.CanOffer(10, null).Should().BeFalse("under the smallest budget");
        JobBoard.CanOffer(800, new string('a', JobBoard.MaxOfferNote + 1)).Should().BeFalse();
    }

    [Fact]
    public async Task SendingAnOffer_MarksThePostInTheFeed_AndChangingItReplacesThePrice()
    {
        _posts.Seed(FeedPost(1));
        await _board.LoadFeedAsync();

        (await _board.SendOfferAsync(_board.Feed[0], 800, null)).Should().Be(OfferResult.Done);
        _board.Feed[0].MyOfferPesos.Should().Be(800);

        (await _board.SendOfferAsync(_board.Feed[0], 650, "  Bukas ng umaga  ")).Should().Be(OfferResult.Done);
        _board.Feed[0].MyOfferPesos.Should().Be(650);
        _posts.Offers[1].Should().Be((650, "Bukas ng umaga"));
    }

    [Fact]
    public async Task TakingAnOfferBack_ClearsItFromThePost()
    {
        _posts.Seed(FeedPost(1));
        await _board.LoadFeedAsync();
        await _board.SendOfferAsync(_board.Feed[0], 800, null);

        (await _board.WithdrawOfferAsync(_board.Feed[0])).Should().Be(OfferResult.Done);

        _board.Feed[0].MyOfferPesos.Should().BeNull();
        _posts.Offers.Should().BeEmpty();
    }

    [Fact]
    public async Task AnOfferOnAPostThatClosed_TakesItOffTheFeed()
    {
        _posts.Seed(FeedPost(1), FeedPost(2));
        await _board.LoadFeedAsync();
        _posts.Gone = true;

        (await _board.SendOfferAsync(_board.Feed[0], 800, null)).Should().Be(OfferResult.Gone);

        _board.Feed.Select(p => p.Id).Should().Equal(2);
    }

    [Fact]
    public async Task AnOfferThatDidNotSend_ChangesNothing_AndNeverThrows()
    {
        _posts.Seed(FeedPost(1));
        await _board.LoadFeedAsync();
        _posts.Fail = true;

        (await _board.SendOfferAsync(_board.Feed[0], 800, null)).Should().Be(OfferResult.Failed);
        (await _board.WithdrawOfferAsync(_board.Feed[0])).Should().Be(OfferResult.Failed);

        _board.Feed[0].MyOfferPesos.Should().BeNull();
    }

    [Fact]
    public async Task ABadPrice_IsNotSent()
    {
        _posts.Seed(FeedPost(1));
        await _board.LoadFeedAsync();

        (await _board.SendOfferAsync(_board.Feed[0], 5, null)).Should().Be(OfferResult.Failed);

        _posts.Offers.Should().BeEmpty();
    }

    [Fact]
    public async Task PickingAnOffer_ClosesThePost_AndRemembersWhoWasPicked()
    {
        await _board.PostAsync(Complete());
        var post = _board.Mine[0];
        _posts.OffersOn[post.Id] = [Offer(10, post.Id, "pro-a"), Offer(11, post.Id, "pro-b", 700)];

        var offers = await _board.LoadOffersAsync(post.Id);
        (await _board.PickAsync(offers[1])).Should().Be(OfferResult.Done);

        _posts.Picked.Should().Equal((post.Id, 11L));
        _board.Mine[0].IsOpen.Should().BeFalse();
        _board.Mine[0].PickedProId.Should().Be("pro-b");
    }

    [Fact]
    public async Task APickThatDidNotGoThrough_LeavesThePostOpen()
    {
        await _board.PostAsync(Complete());
        var post = _board.Mine[0];
        _posts.OffersOn[post.Id] = [Offer(10, post.Id)];
        _posts.Gone = true;

        (await _board.PickAsync(_posts.OffersOn[post.Id][0])).Should().Be(OfferResult.Gone);

        _board.Mine[0].IsOpen.Should().BeTrue();
        _board.Mine[0].PickedProId.Should().BeNull();
    }

    [Fact]
    public void ABudgetFrom_AboveTheTop_CantBePosted()
    {
        var draft = Complete();
        draft.BudgetMinPesos = draft.BudgetPesos + 1;

        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Budget);

        draft.BudgetMinPesos = 0;
        JobBoard.CanPost(draft).Should().BeTrue("the low end is optional and may start at ₱0");
    }

    [Fact]
    public void AnOthersPost_CanBePosted_WithTheOthersService()
    {
        var draft = Complete();
        draft.CategoryId = ReferenceData.OtherTradeId;
        draft.ServiceId = ReferenceData.OtherTradeId;

        JobBoard.CanPost(draft).Should().BeTrue();
    }
}
