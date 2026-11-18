using System.Net;
using System.Net.Http.Headers;
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

    // --- Anti-fraud rule 2 across tables ----------------------------------------------

    [IntegrationFact]
    public async Task ProfilePhoto_ThatIsAnotherWorkersWorkPhoto_IsLeftOff()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Owner");
        await worker.Pro.PublishAsync();
        var listing = await api.Client(api.Tokens.For(worker.Uid)).GetFromJsonAsync($"professionals/{worker.Uid}", ApiJson.Default.ListingDto);
        var stolen = listing!.WorkPhotos[0].Id;
        var thief = NewUid("thief");

        using var response = await api.Client(api.Tokens.For(thief)).PutAsJsonAsync("users/me",
            new UserDto(thief, "Thief", null, null, Wire.English, Wire.Seeker, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, stolen),
            ApiJson.Default.UserDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!.ProfilePhotoId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Publish_WithAnotherAccountsProfilePhoto_IsPhotoInUse()
    {
        // A Seeker's own profile photo, stored in Users.
        var seekerUid = NewUid("photo-seeker");
        var seekerClient = api.Client(api.Tokens.For(seekerUid));
        using var upload = new ByteArrayContent(TestImages.Jpeg());
        upload.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        using var uploaded = await seekerClient.PostAsync("photos", upload);
        var photo = (await uploaded.Content.ReadFromJsonAsync(ApiJson.Default.PhotoDto))!.Id;
        using (await seekerClient.PutAsJsonAsync("users/me",
            new UserDto(seekerUid, "Ana", null, null, Wire.English, Wire.Seeker, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, photo),
            ApiJson.Default.UserDto)) { }

        // A worker republishing their listing with that photo as their own.
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Borrower");
        await worker.Pro.PublishAsync();
        var workerClient = api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber));
        var listing = (await workerClient.GetFromJsonAsync($"professionals/{worker.Uid}", ApiJson.Default.ListingDto))!;
        var request = new PublishListingRequest(listing.DisplayName, listing.CategoryId, listing.AreaId, listing.ServiceIds, listing.CityIds,
            listing.ExperienceId, listing.RateTierId, photo, [.. listing.WorkPhotos.Select(p => p.Id)]);

        using var response = await workerClient.PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.PhotoInUse);
    }
}
