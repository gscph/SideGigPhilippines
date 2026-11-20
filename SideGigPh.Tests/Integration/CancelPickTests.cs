using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Calling off a picked job end to end (the app's <see cref="JobBoard"/> → API → SQL Server): either side may, with a
/// reason, until both marked it done. The post opens again for its other offers, the picked worker's number stops
/// showing, and the worker's offer no longer reads as picked or waiting.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class CancelPickTests(ApiTestHost api)
{
    private static string Tag() => $"zc{Guid.NewGuid():N}"[..10];

    private static JobPostDraft Draft(string tag) => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-plumbing",
        CityId = "taytay",
        BudgetPesos = 900,
        Description = $"Tumutulo ang gripo sa labas {tag}",
    };

    private async Task<TestPhone> PlumberAsync(string name)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "construction", jobs: ["construction-plumbing"]);
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }

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

    /// <summary>The household posts, both workers offer, the household picks <paramref name="picked"/>.</summary>
    private static async Task<JobPost> PickedAsync(TestPhone household, TestPhone picked, TestPhone other)
    {
        var board = household.Get<JobBoard>();
        (await board.PostAsync(Draft(Tag()))).Should().Be(JobPostResult.Posted);
        var post = board.Mine[0];
        (await picked.Get<JobBoard>().SendOfferAsync((await InFeedAsync(picked, post.Id))!, 900, null)).Should().Be(OfferResult.Done);
        (await other.Get<JobBoard>().SendOfferAsync((await InFeedAsync(other, post.Id))!, 850, null)).Should().Be(OfferResult.Done);
        var offer = (await board.LoadOffersAsync(post.Id)).Single(o => o.Pro.Id == picked.Uid);
        (await board.PickAsync(offer)).Should().Be(OfferResult.Done);
        return post;
    }

    [IntegrationFact]
    public async Task TheHouseholdCancels_ThePostOpensAgain_AndAnotherOfferCanBePicked()
    {
        var household = await TestPhone.SeekerAsync(api, "Rosa");
        var picked = await PlumberAsync("Hindi Dumating");
        var other = await PlumberAsync("Maaasahan");
        var post = await PickedAsync(household, picked, other);
        var board = household.Get<JobBoard>();
        (await board.ContactNumberAsync(picked.Uid)).Should().Be(picked.PhoneNumber);
        var job = (await board.PickedJobsAsync(picked.Uid)).Single();

        (await board.CancelPickAsync(job, PickCancelReasons.NoShow, "  Hindi po dumating kahapon  ")).Should().Be(CancelPickResult.Cancelled);

        board.Mine.Single(p => p.Id == post.Id).Should().Match<JobPost>(p => p.IsOpen && p.PickedProId == null, "the app shows it open at once");
        await board.LoadMineAsync();
        var mine = board.Mine.Single(p => p.Id == post.Id);
        mine.IsOpen.Should().BeTrue();
        mine.PickedProId.Should().BeNull();
        mine.OfferCount.Should().Be(1, "the cancelled offer no longer counts; the other worker's still waits");
        (await board.PickedJobsAsync(picked.Uid)).Should().BeEmpty();
        (await picked.Get<JobBoard>().PickedJobsAsync(household.Uid)).Should().BeEmpty();
        (await board.ContactNumberAsync(picked.Uid)).Should().BeNull("no pick links them any more");
        (await InFeedAsync(other, post.Id)).Should().NotBeNull("open again in workers' feeds");

        var offers = await board.LoadOffersAsync(post.Id);
        offers.Should().ContainSingle().Which.Pro.Id.Should().Be(other.Uid);
        (await board.PickAsync(offers[0])).Should().Be(OfferResult.Done);
        (await board.ContactNumberAsync(other.Uid)).Should().Be(other.PhoneNumber);
    }

    [IntegrationFact]
    public async Task TheWorkerCancels_TheirOfferReadsNotPicked_AndTheHouseholdCanPickAgain()
    {
        var household = await TestPhone.SeekerAsync(api, "Linda");
        var picked = await PlumberAsync("May Sakit");
        var other = await PlumberAsync("Handa");
        var post = await PickedAsync(household, picked, other);
        var workBoard = picked.Get<JobBoard>();
        var job = (await workBoard.PickedJobsAsync(household.Uid)).Single();

        (await workBoard.CancelPickAsync(job, PickCancelReasons.CantMakeIt, null)).Should().Be(CancelPickResult.Cancelled);

        (await workBoard.MyOffersAsync()).Single(o => o.Post.Id == post.Id).State.Should().Be(MyOfferState.NotPicked);
        var offers = await household.Get<JobBoard>().LoadOffersAsync(post.Id);
        offers.Select(o => o.Pro.Id).Should().Equal(other.Uid);
    }

    [IntegrationFact]
    public async Task ACompletedJob_CantBeCancelled_AndNobodyElseCanCancelIt()
    {
        var household = await TestPhone.SeekerAsync(api, "Ester");
        var picked = await PlumberAsync("Natapos");
        var other = await PlumberAsync("Iba Pa");
        var post = await PickedAsync(household, picked, other);

        using (var stranger = await api.Client(api.Tokens.For(other.Uid)).PostAsJsonAsync($"picked-jobs/{post.Id}/cancel",
                   new CancelPickRequest(PickCancelReasons.Other), ApiJson.Default.CancelPickRequest))
        {
            stranger.StatusCode.Should().Be(HttpStatusCode.NotFound, "only the household and the picked worker");
        }

        using (var unknown = await api.Client(api.Tokens.For(household.Uid)).PostAsJsonAsync($"picked-jobs/{post.Id}/cancel",
                   new CancelPickRequest("bored"), ApiJson.Default.CancelPickRequest))
        {
            unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest, "an unknown reason");
        }

        var homeBoard = household.Get<JobBoard>();
        var workBoard = picked.Get<JobBoard>();
        await homeBoard.MarkDoneAsync((await homeBoard.PickedJobsAsync(picked.Uid)).Single());
        await workBoard.MarkDoneAsync((await workBoard.PickedJobsAsync(household.Uid)).Single());
        var completed = (await homeBoard.PickedJobsAsync(picked.Uid)).Single();
        completed.IsCompleted.Should().BeTrue();

        using (var late = await api.Client(api.Tokens.For(household.Uid)).PostAsJsonAsync($"picked-jobs/{post.Id}/cancel",
                   new CancelPickRequest(PickCancelReasons.NotNeeded), ApiJson.Default.CancelPickRequest))
        {
            late.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await late.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.JobCompleted);
        }

        (await homeBoard.PickedJobsAsync(picked.Uid)).Single().IsCompleted.Should().BeTrue("a completed job stays");
    }
}
