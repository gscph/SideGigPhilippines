using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Long lists come a page at a time through the real API and database: search results (ranked on the server,
/// <see cref="Paging.Search"/> per page) and chat messages (<see cref="Paging.Messages"/> per page, newest first, older
/// ones as the person scrolls up). Paging must never lose, repeat or reorder anyone.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class PagingTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task Search_ComesAPageAtATime_InRankedOrder_UntilEveryoneIsShown()
    {
        // Errand runners only in this test, so other tests' workers don't change the count.
        var cheap = await PublishErrandRunnerAsync("Abe", "taytay", 450);
        var local = new List<TestPhone> { cheap };
        foreach (var name in new[] { "Bert", "Cesar", "Dado" })
        {
            local.Add(await PublishErrandRunnerAsync(name, "taytay", 3000));
        }

        var nearby = await PublishErrandRunnerAsync("Eddie", "antipolo", 450);
        var answers = SideGigPh.Tests.TestSupport.TestData.AirconCleaningInTaytay();
        answers.CategoryId = "errand_runner";
        answers.ServiceId = "errand-grocery";
        var seeker = await TestPhone.SeekerAsync(api, "Fely", answers);

        var first = await seeker.Seeker.SearchAsync();
        var all = await seeker.Seeker.MoreResultsAsync(first);

        first.Count.Should().Be(Paging.Search);
        first.InTown[0].Pro.Id.Should().Be(cheap.Uid, "in town and in budget ranks first");
        first.InTownTotal.Should().Be(4);
        first.OtherTownsTotal.Should().Be(1);
        first.HasMore.Should().BeTrue();

        all.InTown.Select(h => h.Pro.Id).Should().BeEquivalentTo(local.Select(w => w.Uid));
        all.OtherTowns.Should().ContainSingle().Which.Pro.Id.Should().Be(nearby.Uid);
        all.HasMore.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task Inbox_ComesAPageAtATime_NewestFirst_UntilEveryChatIsShown()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Inday");
        await worker.Pro.PublishAsync();
        var customers = Enumerable.Range(0, Paging.Inbox + 2).Select(i => $"inbox-customer-{i}-{Guid.NewGuid():N}").ToList();
        foreach (var customer in customers)
        {
            using var sent = await api.Client(api.Tokens.For(customer)).PostAsJsonAsync(
                $"chats/{worker.Uid}_{customer}/messages",
                new SendMessageRequest($"m-{customer}", worker.Uid, customer, Wire.Text, "Hi po", "W", "C", null, null, null, null),
                ApiJson.Default.SendMessageRequest);
            sent.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        await worker.Pro.RefreshActivityAsync();

        worker.Pro.Chats.Should().HaveCount(Paging.Inbox);
        worker.Pro.Chats[0].SeekerId.Should().Be(customers[^1], "the newest chat is on top");
        worker.Pro.HasMoreChats.Should().BeTrue();

        await worker.Pro.MoreChatsAsync();

        worker.Pro.Chats.Select(c => c.SeekerId).Should().Equal(Enumerable.Reverse(customers), "nobody lost, repeated or out of order");
        worker.Pro.HasMoreChats.Should().BeFalse();

        // An app from before Inbox paging still gets every chat.
        var everything = await api.Client(api.Tokens.For(worker.Uid)).GetFromJsonAsync("chats?side=professional", ApiJson.Default.ListChatDto);
        everything!.Should().HaveCount(customers.Count);
    }

    [IntegrationFact]
    public async Task Chat_OpensOnTheNewestMessages_AndScrollingUpLoadsTheOlderOnes()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Gardo");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Hilda");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        for (var i = 1; i <= 25; i++)
        {
            await seeker.Seeker.SendAsync(listing, $"Message {i}");
        }

        var opened = await seeker.Seeker.GetMessagesAsync(worker.Uid);
        var all = await seeker.Seeker.OlderMessagesAsync(worker.Uid, opened);

        opened.Count.Should().Be(Paging.Messages);
        opened.Messages[^1].Text.Should().Be("Message 25");
        opened.HasOlder.Should().BeTrue();
        all.Messages.Select(m => m.Text).Should().Equal(Enumerable.Range(1, 25).Select(i => $"Message {i}"));
        all.HasOlder.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task Worker_ScrollsUpInTheChat_AndSeesANewMessageArrive()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Iking");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Joy");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        for (var i = 1; i <= Paging.Messages + 1; i++)
        {
            await seeker.Seeker.SendAsync(listing, $"Message {i}");
        }

        var chatId = seeker.Seeker.ChatIdWith(worker.Uid);
        var opened = await worker.Pro.GetMessagesAsync(chatId);
        var all = await worker.Pro.OlderMessagesAsync(chatId, opened);
        await seeker.Seeker.SendAsync(listing, "One more");
        var updated = await worker.Pro.NewMessagesAsync(chatId, all);

        all.Messages[0].Text.Should().Be("Message 1");
        updated.Count.Should().Be(Paging.Messages + 2);
        updated.Messages[^1].Text.Should().Be("One more");
        (await seeker.Seeker.NewMessagesAsync(worker.Uid, ChatHistory.Empty)).Count.Should().Be(Paging.Messages);
    }

    [IntegrationFact]
    public async Task Messages_WithoutTake_AreEveryMessage_ForAppsBeforePaging()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Kulas");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Lorna");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        for (var i = 1; i <= Paging.Messages + 1; i++)
        {
            await seeker.Seeker.SendAsync(listing, $"Message {i}");
        }

        var client = api.Client(api.Tokens.For(seeker.Uid));
        var chatId = seeker.Seeker.ChatIdWith(worker.Uid);

        var every = await client.GetFromJsonAsync($"chats/{chatId}/messages", ApiJson.Default.ListMessageDto);
        var beforeUnknown = await client.GetFromJsonAsync($"chats/{chatId}/messages?take=5&before=no-such-message", ApiJson.Default.ListMessageDto);
        using var badTake = await client.GetAsync($"chats/{chatId}/messages?take=0");

        every!.Should().HaveCount(Paging.Messages + 1);
        beforeUnknown.Should().BeEmpty();
        badTake.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await badTake.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.Invalid("take"));
    }

    [IntegrationFact]
    public async Task Search_UnrankedListRoute_StillAnswers_ForAppsBeforePaging_AndBadPagesAreRefused()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Mando", trade: "errand_runner", jobs: ["errand-parcel"], towns: ["angono"]);
        await worker.Pro.PublishAsync();
        var client = api.Client(api.Tokens.For("old-app"));

        var listings = await client.GetFromJsonAsync("professionals?categoryId=errand_runner&areaId=rizal", ApiJson.Default.ListListingDto);
        using var badSkip = await client.GetAsync("search/professionals?categoryId=errand_runner&skip=-1");
        using var badTake = await client.GetAsync("search/professionals?categoryId=errand_runner&take=x");
        using var noTrade = await client.GetAsync("search/professionals");

        listings.Should().Contain(l => l.Id == worker.Uid);
        badSkip.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badTake.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        noTrade.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<TestPhone> PublishErrandRunnerAsync(string name, string town, int rate)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name, trade: "errand_runner", jobs: ["errand-grocery"], towns: [town], rate: rate);
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        return worker;
    }
}
