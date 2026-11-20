using System.Net;
using System.Net.Http.Json;
using SideGigPh.Api;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// What a tampered app or a script with a real account could try: flooding other people or our storage, borrowing
/// another account's photo, or faking names. The server refuses or ignores it.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class AbuseTests(ApiTestHost api)
{
    private static string NewUid(string name) => $"{name}-{Guid.NewGuid():N}";

    // --- Rate limits ------------------------------------------------------------------

    [IntegrationFact]
    public async Task OtherJobNotes_PastTheLimit_AreRefusedWith429()
    {
        var client = api.Client(api.Tokens.For(NewUid("noter")));
        var permits = CallerLimits.Rules[Limited.OtherJobNote].Permits;
        var note = new OtherJobRequest("aircon", Wire.Seeker, "Pa-check ng ref");

        for (var i = 0; i < permits; i++)
        {
            using var ok = await client.PostAsJsonAsync("other-jobs", note, ApiJson.Default.OtherJobRequest);
            ok.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var refused = await client.PostAsJsonAsync("other-jobs", note, ApiJson.Default.OtherJobRequest);

        refused.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await refused.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.TooManyRequests);
        using var someoneElse = await api.Client(api.Tokens.For(NewUid("other-noter"))).PostAsJsonAsync("other-jobs", note, ApiJson.Default.OtherJobRequest);
        someoneElse.StatusCode.Should().Be(HttpStatusCode.NoContent, "the limit is per account");
    }

    // Each listing change clears the shared search cache, so a loop would push every search onto the database.
    [IntegrationFact]
    public async Task ListingChanges_PastTheLimit_AreRefusedWith429()
    {
        var client = api.Client(api.Tokens.For(NewUid("churner")));

        for (var i = 0; i < CallerLimits.Rules[Limited.Listing].Permits; i++)
        {
            using var ok = await client.DeleteAsync("professionals/me");
            ok.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var refused = await client.DeleteAsync("professionals/me");

        refused.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [IntegrationFact]
    public async Task ChatMessages_PastTheLimit_AreRefusedWith429()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Flood");
        await worker.Pro.PublishAsync();
        var seekerUid = NewUid("flooder");
        var client = api.Client(api.Tokens.For(seekerUid));
        var chatId = ChatStore.IdFor(worker.Uid, seekerUid);
        var permits = CallerLimits.Rules[Limited.Message].Permits;

        HttpStatusCode last = HttpStatusCode.OK;
        for (var i = 0; i <= permits; i++)
        {
            using var response = await client.PostAsJsonAsync($"chats/{chatId}/messages",
                new SendMessageRequest($"flood-{seekerUid}-{i}", worker.Uid, seekerUid, Wire.Text, $"Hi {i}", "P", "S", null, null, null, null),
                ApiJson.Default.SendMessageRequest);
            if (i < permits)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            last = response.StatusCode;
        }

        last.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // --- Chats ------------------------------------------------------------------------

    [IntegrationFact]
    public async Task NewChat_TakesTheWorkersNameFromTheListing_NotTheRequest()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Realname");
        await worker.Pro.PublishAsync();
        var seekerUid = NewUid("namer");
        var client = api.Client(api.Tokens.For(seekerUid));
        var listing = await client.GetFromJsonAsync($"professionals/{worker.Uid}", ApiJson.Default.ListingDto);

        using var response = await client.PostAsJsonAsync($"chats/{ChatStore.IdFor(worker.Uid, seekerUid)}/messages",
            new SendMessageRequest("named-1", worker.Uid, seekerUid, Wire.Text, "Hi", "Fake Name", "S", null, null, null, null),
            ApiJson.Default.SendMessageRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chat = await response.Content.ReadFromJsonAsync(ApiJson.Default.ChatDto);
        chat!.ProfessionalName.Should().Be(listing!.DisplayName);
    }

    [IntegrationFact]
    public async Task OverlongIdsInAMessage_AreBadRequests()
    {
        var seekerUid = NewUid("long");
        var longId = new string('p', 200);
        using var response = await api.Client(api.Tokens.For(seekerUid)).PostAsJsonAsync($"chats/{ChatStore.IdFor(longId, seekerUid)}/messages",
            new SendMessageRequest("long-1", longId, seekerUid, Wire.Text, "Hi", "P", "S", null, null, null, null),
            ApiJson.Default.SendMessageRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Search -----------------------------------------------------------------------

    [IntegrationFact]
    public async Task SearchForAnIdThatCantBeATrade_FindsNobody()
    {
        var page = await api.Client(api.Tokens.For(NewUid("searcher")))
            .GetFromJsonAsync($"search/professionals?categoryId={Uri.EscapeDataString("DROP TABLE; " + new string('x', 500))}", ApiJson.Default.SearchPageDto);

        page!.Hits.Should().BeEmpty();
    }
}
