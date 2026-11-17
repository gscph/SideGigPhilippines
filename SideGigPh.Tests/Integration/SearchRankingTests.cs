using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Search ranking reads how workers behave on the app, through the real API and database: a worker who answers the
/// customers who write to them ranks above one who leaves them waiting.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SearchRankingTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task WorkerWhoReplies_RanksAboveWorkerWhoDoesnt()
    {
        // Roofers only in this test, so other tests' workers don't join the results. Same listing for both, and
        // "Rosa" sorts after "Ramil", so only her reply can put her first.
        var silent = await PublishRooferAsync("Ramil");
        var replies = await PublishRooferAsync("Rosa");
        var customer = await TestPhone.SeekerAsync(api, "Cora", Answers());
        foreach (var worker in new[] { silent, replies })
        {
            await customer.Seeker.SendAsync((await customer.Seeker.GetProAsync(worker.Uid))!, "Tulo po ang bubong");
            await api.StartChatEarlierAsync(customer.Seeker.ChatIdWith(worker.Uid), TimeSpan.FromHours(25));
        }

        await replies.Pro.RefreshActivityAsync();
        await replies.Pro.ReplyAsync(replies.Pro.Chats.Single(), "Opo, punta ako bukas");
        await replies.Pro.PublishAsync(); // an edit clears the server's 30 s search cache, as it would in time

        var results = await (await TestPhone.SeekerAsync(api, "Dina", Answers())).Seeker.SearchAsync();

        results.InTown.Select(h => h.Pro.Id).Should().Equal(replies.Uid, silent.Uid);
    }

    private static SeekerAnswers Answers()
    {
        var answers = SideGigPh.Tests.TestSupport.TestData.AirconCleaningInTaytay();
        answers.CategoryId = "construction";
        answers.ServiceId = "construction-roofing";
        return answers;
    }

    private async Task<TestPhone> PublishRooferAsync(string name)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "construction", jobs: ["construction-roofing"], towns: ["taytay"]);
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }
}
