using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// "Others" end to end (app → API → SQL Server): a worker and a household whose work we don't list both get into the
/// app, and find each other by what they typed. Also the prices: a worker's own average price and a job post's
/// budget "from" are stored and come back.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class OthersTests(ApiTestHost api)
{
    private const string Other = ReferenceData.OtherTradeId;

    private static string Tag() => $"zq{Guid.NewGuid():N}"[..10];

    private static async Task<TestPhone> OthersWorkerAsync(ApiTestHost api, string name, string typed, int rate = 1000)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: Other, jobs: [Other], rate: rate);
        worker.Pro.Draft.OtherJobText = typed;
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }

    [IntegrationFact]
    public async Task AnOthersHousehold_FindsTheOthersWorkerWhoTypedTheSameWords_First()
    {
        var tag = Tag();
        await OthersWorkerAsync(api, $"Pintor {tag}", "Paint ng pader at gate");
        await OthersWorkerAsync(api, $"Kisame {tag}", $"Kisame at plaster {tag}");
        var answers = TestData.AirconCleaningInTaytay();
        answers.PickCategory(Other);
        answers.OtherJobText = $"Plaster ng kisame {tag}";

        var results = await (await TestPhone.SeekerAsync(api, "Lorna", answers)).Seeker.SearchAsync();

        var mine = results.InTown.Select(hit => hit.Pro).Where(pro => pro.DisplayName.EndsWith(tag)).ToList();
        mine.Select(pro => pro.DisplayName).Should().Equal($"Kisame {tag}", $"Pintor {tag}");
        mine[0].OtherWork.Should().Be($"Kisame at plaster {tag}", "the household sees what the worker typed");
    }

    [IntegrationFact]
    public async Task AWorkersOwnPrice_IsStoredAndComesBack()
    {
        var worker = await OthersWorkerAsync(api, "Price", "Ayos ng gate", rate: 1450);

        var listing = await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber))
            .GetFromJsonAsync($"professionals/{worker.Uid}", ApiJson.Default.ListingDto);

        listing!.RatePesos.Should().Be(1450);
        listing.CategoryId.Should().Be(Other);
    }

    [IntegrationFact]
    public async Task AnOthersPost_WithABudgetRange_ReachesOthersWorkers()
    {
        var tag = Tag();
        var worker = await OthersWorkerAsync(api, "Gawa", "Ayos ng gate");
        var household = await TestPhone.SeekerAsync(api, "Rosa");
        var board = household.Get<JobBoard>();

        (await board.PostAsync(new JobPostDraft
        {
            CategoryId = Other,
            ServiceId = Other,
            CityId = "taytay",
            BudgetMinPesos = 800,
            BudgetPesos = 1500,
            Description = $"Ayusin ang sirang gate {tag}",
        })).Should().Be(JobPostResult.Posted);

        board.Mine[0].BudgetMinPesos.Should().Be(800);
        var feed = worker.Get<JobBoard>();
        await feed.LoadFeedAsync();
        feed.Feed.Should().Contain(post => post.Description.EndsWith(tag) && post.BudgetMinPesos == 800 && post.BudgetPesos == 1500);
    }

    [IntegrationFact]
    public async Task ABudgetFromAboveTheTop_IsRefused()
    {
        var household = await TestPhone.SeekerAsync(api, "Mali");
        var request = new NewJobPostRequest("construction", "construction-plumbing", "Barado ang lababo sa kusina", 800, "taytay", BudgetMinPesos: 900);

        using var response = await api.Client(api.Tokens.For(household.Uid)).PostAsJsonAsync("job-posts", request, ApiJson.Default.NewJobPostRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be("invalid-budgetMinPesos");
    }
}
