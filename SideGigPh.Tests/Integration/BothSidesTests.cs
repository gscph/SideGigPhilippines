using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// One account on both sides (switched on the Me page): a live Skilled Worker who is also a household searches,
/// writes to another worker and keeps their own listing and customers, each side with its own Inbox.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class BothSidesTests(ApiTestHost api)
{
    /// <summary>A live worker who switched to the household side and finished its interview.</summary>
    private static async Task<TestPhone> LiveWorkerAlsoCustomerAsync(ApiTestHost api, string name)
    {
        var phone = await TestPhone.WorkerReadyToPublishAsync(api, name);
        await phone.Pro.PublishAsync();
        await phone.Account.SwitchSideAsync(UserRole.Seeker);
        var state = await phone.Store.LoadAsync();
        state.Seeker = TestData.AirconCleaningInTaytay();
        state.SeekerFinishedAt = DateTimeOffset.Now;
        (await phone.Seeker.EnsureLoadedAsync()).Should().BeTrue();
        return phone;
    }

    [IntegrationFact]
    public async Task WorkerSearchingAsCustomer_DoesNotFindThemself_ButOthersStillFindThem()
    {
        var both = await LiveWorkerAlsoCustomerAsync(api, "Lito");
        var stranger = await TestPhone.SeekerAsync(api, "Nena");

        (await AllResultIdsAsync(both.Uid)).Should().NotContain(both.Uid, "nobody contacts themselves");
        (await AllResultIdsAsync(stranger.Uid)).Should().Contain(both.Uid, "the listing is still live for everyone else");
    }

    // Every aircon worker the caller's search returns, in one page: the shared test database holds more than one page.
    private async Task<List<string>> AllResultIdsAsync(string callerUid)
    {
        var page = await api.Client(api.Tokens.For(callerUid))
            .GetFromJsonAsync("search/professionals?categoryId=aircon&cityId=taytay&take=1000", ApiJson.Default.SearchPageDto);
        return [.. page!.Hits.Select(hit => hit.Listing.Id)];
    }

    [IntegrationFact]
    public async Task WorkerWritingToAnotherWorkerAsCustomer_KeepsEachSidesInboxApart()
    {
        var both = await LiveWorkerAlsoCustomerAsync(api, "Mario");
        var other = await TestPhone.WorkerReadyToPublishAsync(api, "Oca");
        await other.Pro.PublishAsync();
        var customer = await TestPhone.SeekerAsync(api, "Pia");

        await both.Seeker.SendAsync((await both.Seeker.GetProAsync(other.Uid))!, "Hi po, linis ng aircon");
        await customer.Seeker.SendAsync((await customer.Seeker.GetProAsync(both.Uid))!, "Hi po");
        await both.Seeker.RefreshActivityAsync();
        (await both.Pro.EnsureLoadedAsync()).Should().BeTrue("the worker side stays set up");
        await both.Pro.RefreshActivityAsync();

        both.Seeker.Threads.Should().ContainSingle().Which.Pro.Id.Should().Be(other.Uid);
        both.Pro.Chats.Should().ContainSingle().Which.SeekerId.Should().Be(customer.Uid);
        both.Pro.IsPublished.Should().BeTrue();
    }
}
