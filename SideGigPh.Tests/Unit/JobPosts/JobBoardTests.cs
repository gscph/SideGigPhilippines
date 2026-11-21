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
        Title = "Barado ang lababo",
        Description = "Barado ang lababo sa kusina",
    };

    private static PhotoPick Picked(params byte[] firstBytes) =>
        new(PhotoPickStatus.Picked, firstBytes.Select(b => new[] { b, (byte)0xD8 }).ToList());

    [Fact]
    public void ACompleteDraft_CanBePosted() => JobBoard.CanPost(Complete()).Should().BeTrue();

    [Fact]
    public void TheFirstMissingField_IsNamed_InTheOrderOnScreen()
    {
        JobBoard.FirstMissing(new JobPostDraft()).Should().Be(JobPostField.Trade);

        var draft = Complete();
        draft.ServiceId = "aircon-cleaning";
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Service, "a service of another trade");

        draft = Complete();
        draft.Title = "  Sira ";
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Title, "too short to be a title");

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

        draft.BudgetPesos = null;
        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Town, "Where sits above Your budget on the card");
    }

    [Fact]
    public void ANewPost_StartsFromTheirAnswers_TodayAndTypedOthersIncluded()
    {
        var today = new DateOnly(2026, 11, 19);
        var answers = new SeekerAnswers
        {
            CategoryId = "aircon", ServiceId = "aircon-cleaning", CityId = "angono", BarangayId = "angono-san-isidro",
            BudgetId = "800-1500", UrgencyId = ReferenceData.UrgencyToday,
        };

        var draft = JobBoard.DraftFrom(answers, today);

        draft.Should().BeEquivalentTo(new
        {
            CategoryId = "aircon", ServiceId = "aircon-cleaning", CityId = "angono", BarangayId = "angono-san-isidro",
            BudgetMinPesos = (int?)800, BudgetPesos = (int?)1500, NeededOn = (DateOnly?)today,
        }, options => options.ExcludingMissingMembers());
        draft.Description.Should().BeNull();

        answers.UrgencyId = ReferenceData.UrgencyThisWeek;
        answers.BudgetId = "under-1500";
        JobBoard.DraftFrom(answers, today).NeededOn.Should().BeNull("no one day: any day");
        JobBoard.DraftFrom(answers, today).BudgetMinPesos.Should().BeNull("only an \"up to\"");

        answers.ServiceId = ReferenceData.ServicesOf("aircon").Single(service => ReferenceData.IsOtherJob(service.Id)).Id;
        answers.OtherJobText = "Ayos ng ref";
        JobBoard.DraftFrom(answers, today).Description.Should().Be("Ayos ng ref", "a trade's own \"Others\" job keeps what they typed");

        answers.CategoryId = answers.ServiceId = ReferenceData.OtherTradeId;
        JobBoard.DraftFrom(answers, today).Description.Should().Be("Ayos ng ref");
    }

    [Fact]
    public void Missing_NamesEverythingStillNeeded_InTheOrderOnScreen()
    {
        JobBoard.Missing(Complete()).Should().BeEmpty();
        JobBoard.Missing(new JobPostDraft()).Should().Equal(
            JobPostField.Trade, JobPostField.Town, JobPostField.Budget, JobPostField.Title, JobPostField.Description);
        JobBoard.Missing(new JobPostDraft { CategoryId = "aircon", CityId = "angono", BudgetPesos = 800, Title = "Tumutulo ang aircon" }).Should().Equal(
            [JobPostField.Service, JobPostField.Description], "the trade is picked, its exact service is not");
    }

    [Fact]
    public void EachMissingField_SaysWhatToDo()
    {
        var text = new AppText();
        JobBoard.MissingText(JobPostField.Description, text).Should().Be("Describe the problem in at least 10 letters.");
        JobBoard.MissingText(JobPostField.Title, text).Should().Be("Give your post a title, at least 5 letters.");
        JobBoard.MissingText(JobPostField.Budget, text).Should().Contain("₱50 – ₱1,000,000").And.Contain("not less than");
        JobBoard.MissingText(JobPostField.Trade, text).Should().Be("Pick the kind of work.");
        JobBoard.MissingText(JobPostField.Town, text).Should().Be("Pick your town.");

        foreach (var draft in new[] { Budget(40), Budget(2_000_000), Budget(1000, from: 1500) })
        {
            JobBoard.FirstMissing(draft).Should().Be(JobPostField.Budget, "too small, too big, or \"from\" above \"up to\" is refused by the server too");
        }

        static JobPostDraft Budget(int upTo, int? from = null)
        {
            var draft = Complete();
            (draft.BudgetMinPesos, draft.BudgetPesos) = (from, upTo);
            return draft;
        }
    }

    [Fact]
    public async Task Photos_AreUploadedAsPicked_UpToThree()
    {
        var draft = Complete();

        var first = await _board.AddPhotosAsync(draft, Picked(1, 2));
        first.Should().Be(new PhotoAddResult(Added: 2));
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
        _photos.Refuse = new PhotoStoreException("offline", PhotoStoreFailure.NoInternet);

        var result = await _board.AddPhotosAsync(draft, Picked(2));

        result.Problem.Should().Be(PhotoProblem.NoInternet);
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

    private static JobPost Open(long id, int offers = 0) => new(
        id, "construction", "construction-plumbing", "Barado ang lababo sa kusina", 800, "taytay",
        [FakePhotoStore.Url("p1"), FakePhotoStore.Url("p2")], IsOpen: true, Mine: true, OffersService: false, DistanceKm: null,
        DateTimeOffset.UnixEpoch, OfferCount: offers, BudgetMinPesos: 500, BarangayId: "taytay-san-isidro", PhotoIds: ["p1", "p2"], Title: "Barado ang lababo");

    [Fact]
    public void AnEditDraft_HoldsThePostAsItIs()
    {
        var draft = JobBoard.EditDraft(Open(7));

        draft.EditsPostId.Should().Be(7);
        draft.ServiceId.Should().Be("construction-plumbing");
        draft.Title.Should().Be("Barado ang lababo");
        draft.BudgetMinPesos.Should().Be(500);
        draft.BarangayId.Should().Be("taytay-san-isidro");
        draft.Photos.Select(p => p.Id).Should().Equal("p1", "p2");
        draft.WhatLocked.Should().BeFalse();
        JobBoard.EditDraft(Open(7, offers: 2)).WhatLocked.Should().BeTrue("workers offered for this trade and service");
    }

    [Fact]
    public async Task RemovingAPhotoOfThePost_KeepsTheStoredCopy_UntilSave()
    {
        var draft = JobBoard.EditDraft(Open(7));
        await _board.AddPhotosAsync(draft, Picked(9));
        var added = draft.Photos[^1].Id;

        await _board.RemovePhotoAsync(draft, "p1");
        await _board.RemovePhotoAsync(draft, added);

        draft.Photos.Select(p => p.Id).Should().Equal("p2");
        _photos.Deleted.Should().Equal([added], "only the photo added in this edit was never on the post");
    }

    [Fact]
    public async Task SavingAnEdit_ChangesThePostInTheList_AndKeepsItsOfferCount()
    {
        _posts.SeedMine(Open(7, offers: 3));
        await _board.LoadMineAsync();
        var draft = JobBoard.EditDraft(_board.Mine[0]);
        draft.BudgetPesos = 1500;

        (await _board.SaveEditAsync(draft)).Should().Be(JobPostResult.Posted);

        _board.Mine.Should().ContainSingle().Which.Should().Match<JobPost>(p => p.BudgetPesos == 1500 && p.OfferCount == 3);
    }

    [Fact]
    public async Task AnEdit_ThatDoesntSend_OrFindsThePostClosed_SaysSo()
    {
        _posts.SeedMine(Open(7));
        await _board.LoadMineAsync();
        var draft = JobBoard.EditDraft(_board.Mine[0]);

        _posts.Fail = true;
        (await _board.SaveEditAsync(draft)).Should().Be(JobPostResult.Failed);
        _posts.Fail = false;

        await _board.CloseAsync(_board.Mine[0]);
        (await _board.SaveEditAsync(draft)).Should().Be(JobPostResult.Closed);

        draft.Description = "maikli";
        (await _board.SaveEditAsync(draft)).Should().Be(JobPostResult.Incomplete);
    }
}
