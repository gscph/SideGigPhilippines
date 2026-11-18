using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

using GeoPoint = SideGigPh.AzureSQL.GeoPoint;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Job posts end to end (the app's <see cref="JobBoard"/> → API → SQL Server): a household posts what it needs done,
/// and only workers of that trade who travel that far see it, a service they offer first, then the nearest. The
/// household's exact point never leaves the server.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class JobPostTests(ApiTestHost api)
{
    private static string Tag() => $"zq{Guid.NewGuid():N}"[..10];

    private static JobPostDraft Draft(string tag, string service = "construction-plumbing", string town = "taytay", int budget = 800) => new()
    {
        CategoryId = "construction",
        ServiceId = service,
        CityId = town,
        BudgetPesos = budget,
        Description = $"Barado ang lababo sa kusina {tag}",
    };

    private static async Task<JobPost> PostAsync(TestPhone household, JobPostDraft draft)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(draft)).Should().Be(JobPostResult.Posted);
        return board.Mine[0];
    }

    /// <summary>The worker's whole feed, every page, narrowed to this test's posts (the database is shared).</summary>
    private static async Task<List<JobPost>> FeedAsync(TestPhone worker, string tag)
    {
        var board = worker.Get<JobBoard>();
        await board.LoadFeedAsync();
        while (board.HasMore)
        {
            await board.LoadMoreFeedAsync();
        }

        return [.. board.Feed.Where(p => p.Description.EndsWith(tag))];
    }

    private static async Task<TestPhone> PlumberAsync(ApiTestHost api, string name, string[]? jobs = null, string[]? towns = null)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "construction", jobs: jobs ?? ["construction-plumbing"], towns: towns);
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }

    /// <summary>Publishes the worker's listing again with their own base point and travel distance.</summary>
    private async Task MoveAsync(TestPhone worker, GeoPoint point, int radiusKm)
    {
        var client = api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber));
        var listing = (await client.GetFromJsonAsync($"professionals/{worker.Uid}", ApiJson.Default.ListingDto))!;
        var request = new PublishListingRequest(listing.DisplayName, listing.CategoryId, listing.AreaId, listing.ServiceIds, listing.CityIds,
            listing.ExperienceId, listing.RateTierId, listing.ProfilePhoto!.Id, [.. listing.WorkPhotos.Select(p => p.Id)],
            point.Latitude, point.Longitude, radiusKm);
        (await client.PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [IntegrationFact]
    public async Task AHouseholdPosts_OnlyWorkersOfThatTrade_SeeIt()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Maricel");
        var plumber = await PlumberAsync(api, "Tubero");
        var mechanic = await TestPhone.WorkerReadyToPublishAsync(api, "Mekaniko", trade: "car_mechanic", jobs: ["mechanic-engine"]);
        await mechanic.Pro.PublishAsync();

        var posted = await PostAsync(household, Draft(tag));

        posted.IsOpen.Should().BeTrue();
        posted.Mine.Should().BeTrue();
        var seen = (await FeedAsync(plumber, tag)).Should().ContainSingle().Subject;
        seen.Id.Should().Be(posted.Id);
        seen.Mine.Should().BeFalse();
        seen.OffersService.Should().BeTrue("the plumber's listing has this service");
        seen.BudgetPesos.Should().Be(800);
        seen.CityId.Should().Be("taytay");
        seen.DistanceKm.Should().Be(1, "both sit at Taytay's centre, and a distance is never shown under 1 km");
        (await FeedAsync(mechanic, tag)).Should().BeEmpty("another trade");
    }

    [IntegrationFact]
    public async Task TheFeed_PutsAServiceTheWorkerOffersFirst_ThenTheNearest()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Lorna");
        var plumber = await PlumberAsync(api, "Tubero", jobs: ["construction-plumbing"], towns: ["taytay"]);
        await MoveAsync(plumber, GeoRules.TownCentres["taytay"], radiusKm: 20);

        var otherServiceNear = await PostAsync(household, Draft(tag, service: "construction-welding", town: "taytay"));
        var theirServiceFar = await PostAsync(household, Draft(tag, service: "construction-plumbing", town: "antipolo"));
        var theirServiceNear = await PostAsync(household, Draft(tag, service: "construction-plumbing", town: "taytay"));

        var feed = await FeedAsync(plumber, tag);

        feed.Select(p => p.Id).Should().Equal([theirServiceNear.Id, theirServiceFar.Id, otherServiceNear.Id],
            "their own service first (nearest of those first), then the rest of their trade");
        feed.Select(p => p.OffersService).Should().Equal(true, true, false);
        feed[1].DistanceKm.Should().BeInRange(5, 7, "Antipolo's centre is about 6 km from Taytay's");
    }

    [IntegrationFact]
    public async Task AWorkerWhoGoes3Km_DoesNotSeeAPost6KmAway()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Nena");
        var plumber = await PlumberAsync(api, "Malapit");
        await MoveAsync(plumber, GeoRules.TownCentres["taytay"], radiusKm: 3);

        var near = await PostAsync(household, Draft(tag, town: "taytay"));
        await PostAsync(household, Draft(tag, service: "construction-welding", town: "antipolo"));

        (await FeedAsync(plumber, tag)).Select(p => p.Id).Should().Equal(near.Id);
    }

    [IntegrationFact]
    public async Task AWorkerWithoutAPublishedListing_IsToldToPublishFirst()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Di pa live", trade: "construction", jobs: ["construction-plumbing"]);

        var board = worker.Get<JobBoard>();
        await board.LoadFeedAsync();

        board.HasListing.Should().BeFalse();
        board.Feed.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task TakingAPostDown_HidesItFromWorkers_AndKeepsItInTheHouseholdsList()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Susan");
        var plumber = await PlumberAsync(api, "Tubero");
        var posted = await PostAsync(household, Draft(tag));
        var board = household.Get<JobBoard>();

        (await board.CloseAsync(posted)).Should().BeTrue();

        (await FeedAsync(plumber, tag)).Should().BeEmpty();
        await board.LoadMineAsync();
        board.Mine.Single(p => p.Id == posted.Id).IsOpen.Should().BeFalse();
        using var notTheirs = await api.Client(api.Tokens.For(plumber.Uid, plumber.PhoneNumber)).DeleteAsync($"job-posts/{posted.Id}");
        notTheirs.StatusCode.Should().Be(HttpStatusCode.NotFound, "only its household takes a post down");
    }

    [IntegrationFact]
    public async Task PostingTheSameThingTwice_KeepsOnePost()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Dalawang tap");

        var first = await PostAsync(household, Draft(tag));
        var again = await PostAsync(household, Draft(tag));

        again.Id.Should().Be(first.Id);
        household.Get<JobBoard>().Mine.Count(p => p.Description.EndsWith(tag)).Should().Be(1);
    }

    [IntegrationFact]
    public async Task PhotosOfTheProblem_ComeWithThePost()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "May photo");
        var plumber = await PlumberAsync(api, "Tubero");
        var board = household.Get<JobBoard>();
        var draft = Draft(tag);

        var added = await board.AddPhotosAsync(draft, new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg(), TestImages.Jpeg()]));
        added.Added.Should().Be(2);
        await PostAsync(household, draft);

        var seen = (await FeedAsync(plumber, tag)).Should().ContainSingle().Subject;
        seen.PhotoUrls.Should().Equal(draft.Photos.Select(p => p.Url));
    }

    [IntegrationFact]
    public async Task AFakePost_IsReported_AndABlock_HidesTheHouseholdsPosts()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Prank");
        var plumber = await PlumberAsync(api, "Tubero");
        var posted = await PostAsync(household, Draft(tag));
        var seen = (await FeedAsync(plumber, tag)).Should().ContainSingle().Subject;

        (await plumber.Get<JobBoard>().ReportAsync(seen)).Should().BeTrue();

        plumber.Get<JobBoard>().Feed.Should().NotContain(p => p.Id == posted.Id, "a reported post leaves the worker's screen at once");
        using (var scope = api.Server.Services.CreateScope())
        {
            var report = await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().Reports.AsNoTracking()
                .SingleAsync(r => r.SubjectId == $"{JobPostStore.ReportSubjectPrefix}{posted.Id}");
            (report.ReporterUid, report.TargetUid, report.Reason).Should().Be((plumber.Uid, household.Uid, ReportReasons.Fake));
        }

        await plumber.Pro.BlockAsync(household.Uid);
        (await FeedAsync(plumber, tag)).Should().BeEmpty("a block either way hides the two from each other");
    }

    [IntegrationFact]
    public async Task DeleteAccount_RemovesTheHouseholdsPosts()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Aalis");
        var plumber = await PlumberAsync(api, "Tubero");
        var posted = await PostAsync(household, Draft(tag));

        await household.Account.DeleteAccountAsync();

        (await FeedAsync(plumber, tag)).Should().BeEmpty();
        using var scope = api.Server.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().JobPosts.AnyAsync(p => p.Id == posted.Id)).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task APostThatIsNotComplete_IsRefused_NamingTheField()
    {
        const string Problem = "Barado ang lababo sa kusina";
        var household = await TestPhone.SeekerAsync(api, "Kulang");
        (NewJobPostRequest Request, string Error)[] cases =
        [
            (new("construction", "construction-plumbing", "Tulo", 800, "taytay"), "invalid-description"),
            (new("construction", "aircon-cleaning", Problem, 800, "taytay"), "invalid-serviceId"),
            (new("karaoke", "construction-plumbing", Problem, 800, "taytay"), "invalid-categoryId"),
            (new("construction", "construction-plumbing", Problem, 800, "makati"), "invalid-cityId"),
            (new("construction", "construction-plumbing", Problem, 5, "taytay"), "invalid-budgetPesos"),
            (new("construction", "construction-plumbing", Problem, 800, "taytay", ["not-a-photo"]), "invalid-photoIds"),
            (new("construction", "construction-plumbing", Problem, 800, "taytay", null, 0, 0), "invalid-latitude"),
        ];

        foreach (var (request, error) in cases)
        {
            using var response = await api.Client(api.Tokens.For(household.Uid)).PostAsJsonAsync("job-posts", request, ApiJson.Default.NewJobPostRequest);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, error);
            (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(error);
        }
    }

    [IntegrationFact]
    public async Task TheHouseholdsPoint_IsStoredButNeverSent()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Pribado");
        var plumber = await PlumberAsync(api, "Tubero");
        var request = new NewJobPostRequest("construction", "construction-plumbing", $"Barado ang lababo sa kusina {tag}", 800, "taytay", null, 14.5601, 121.1352);
        using var response = await api.Client(api.Tokens.For(household.Uid)).PostAsJsonAsync("job-posts", request, ApiJson.Default.NewJobPostRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await response.Content.ReadFromJsonAsync(ApiJson.Default.JobPostDto))!.Id;

        using (var scope = api.Server.Services.CreateScope())
        {
            var row = await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().JobPosts.AsNoTracking().SingleAsync(p => p.Id == id);
            (row.Latitude, row.Longitude).Should().Be((14.5601, 121.1352));
        }

        var feed = await api.Client(api.Tokens.For(plumber.Uid, plumber.PhoneNumber)).GetStringAsync("job-posts/feed?take=30");
        feed.Should().Contain(tag).And.NotContain("14.56").And.NotContain("121.13").And.NotContain("atitude");
        var mine = await api.Client(api.Tokens.For(household.Uid)).GetStringAsync("job-posts/mine");
        mine.Should().Contain(tag).And.NotContain("atitude");
    }

    [IntegrationFact]
    public async Task ABarangay_IsKeptOnThePost_AndWorkersSeeIt()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Barangay");
        var plumber = await PlumberAsync(api, "Tubero", towns: ["angono"]);
        var draft = Draft(tag, town: "angono");
        draft.BarangayId = "angono-san-isidro";

        var posted = await PostAsync(household, draft);

        posted.BarangayId.Should().Be("angono-san-isidro");
        (await FeedAsync(plumber, tag)).Should().ContainSingle().Which.BarangayId.Should().Be("angono-san-isidro");
    }

    [IntegrationFact]
    public async Task ABarangayOfAnotherTown_OrNotServed_IsLeftOff_AndThePostStillGoesThrough()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Iba");
        var client = api.Client(api.Tokens.For(household.Uid));

        async Task<string?> PostWith(string? barangayId, string words)
        {
            var request = new NewJobPostRequest("construction", "construction-plumbing", $"{words} {tag}", 800, "taytay", BarangayId: barangayId);
            using var response = await client.PostAsJsonAsync("job-posts", request, ApiJson.Default.NewJobPostRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK, "a barangay is optional and never stops a post");
            return (await response.Content.ReadFromJsonAsync(ApiJson.Default.JobPostDto))!.BarangayId;
        }

        (await PostWith("angono-san-isidro", "Barado ang lababo sa kusina")).Should().BeNull("San Isidro of Angono is not in Taytay");
        (await PostWith("taytay-nowhere", "Tumutulo ang gripo sa banyo")).Should().BeNull("not a barangay we serve");
        (await PostWith(null, "Sira ang inidoro sa itaas")).Should().BeNull();
        (await PostWith("taytay-san-isidro", "Barado ang drain sa labas")).Should().Be("taytay-san-isidro");
    }

    [IntegrationFact]
    public async Task AHousehold_EditsItsOpenPost_AndWorkersSeeTheChange()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Nagbago");
        var plumber = await PlumberAsync(api, "Tubero");
        var board = household.Get<JobBoard>();
        var first = Draft(tag);
        await board.AddPhotosAsync(first, new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg(), TestImages.Jpeg()]));
        var posted = await PostAsync(household, first);

        var edit = JobBoard.EditDraft((await board.FindMineAsync(posted.Id))!);
        edit.WhatLocked.Should().BeFalse("no offers yet");
        edit.Photos.Select(p => p.Id).Should().Equal(first.Photos.Select(p => p.Id), "the post's photos come back with their ids");
        await board.RemovePhotoAsync(edit, edit.Photos[0].Id);
        edit.ServiceId = "construction-tiles";
        edit.Description = $"Pati tiles sa CR, natanggal na {tag}";
        edit.BudgetMinPesos = 1000;
        edit.BudgetPesos = 2500;

        (await board.SaveEditAsync(edit)).Should().Be(JobPostResult.Posted);

        var mine = board.Mine.Single(p => p.Id == posted.Id);
        mine.ServiceId.Should().Be("construction-tiles");
        mine.BudgetPesos.Should().Be(2500);
        mine.PhotoIds.Should().Equal(first.Photos[1].Id);
        var seen = (await FeedAsync(plumber, tag)).Should().ContainSingle().Subject;
        seen.Id.Should().Be(posted.Id, "an edit changes the post, it doesn't make another");
        seen.Description.Should().StartWith("Pati tiles");
        seen.BudgetMinPesos.Should().Be(1000);
        seen.PhotoUrls.Should().ContainSingle();
    }

    [IntegrationFact]
    public async Task OnceOffersCame_TheTradeAndServiceStay_ButTheRestCanChange()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "May offer");
        var plumber = await PlumberAsync(api, "Tubero");
        var posted = await PostAsync(household, Draft(tag));
        var seen = (await FeedAsync(plumber, tag)).Should().ContainSingle().Subject;
        (await plumber.Get<JobBoard>().SendOfferAsync(seen, 900, null)).Should().Be(OfferResult.Done);

        var board = household.Get<JobBoard>();
        await board.LoadMineAsync();
        var edit = JobBoard.EditDraft(board.Mine.Single(p => p.Id == posted.Id));
        edit.WhatLocked.Should().BeTrue("a worker already offered for this work");

        var client = api.Client(api.Tokens.For(household.Uid));
        var moved = Draft(tag, service: "construction-tiles");
        using (var refused = await client.PutAsJsonAsync($"job-posts/{posted.Id}", new NewJobPostRequest(
            moved.CategoryId!, moved.ServiceId!, moved.Description!, 800, "taytay"), ApiJson.Default.NewJobPostRequest))
        {
            refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await refused.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.Invalid("serviceId"));
        }

        edit.BudgetPesos = 1200;
        (await board.SaveEditAsync(edit)).Should().Be(JobPostResult.Posted);
        board.Mine.Single(p => p.Id == posted.Id).OfferCount.Should().Be(1, "the offers stay with the post");
    }

    [IntegrationFact]
    public async Task OnlyTheHousehold_EditsItsPost_AndOnlyWhileItIsOpen()
    {
        var tag = Tag();
        var household = await TestPhone.SeekerAsync(api, "Akin");
        var stranger = await TestPhone.SeekerAsync(api, "Iba");
        var posted = await PostAsync(household, Draft(tag));
        var body = new NewJobPostRequest("construction", "construction-plumbing", $"Hindi ko ito post {tag}", 800, "taytay");

        (await api.Client(api.Tokens.For(stranger.Uid)).PutAsJsonAsync($"job-posts/{posted.Id}", body, ApiJson.Default.NewJobPostRequest))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "someone else's post");

        var board = household.Get<JobBoard>();
        var edit = JobBoard.EditDraft(posted);
        (await board.CloseAsync(posted)).Should().BeTrue();
        (await board.SaveEditAsync(edit)).Should().Be(JobPostResult.Closed, "a post taken down can't change");
    }
}
