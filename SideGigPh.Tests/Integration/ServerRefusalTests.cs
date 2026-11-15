using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using SideGigPh.Api;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// What the server does with requests the app shouldn't send (an old or tampered app, a bad network, two phones at
/// once): it must refuse them with a clear code and keep the data correct, never crash or store something wrong.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ServerRefusalTests(ApiTestHost api)
{
    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static SendMessageRequest Message(string proId, string seekerId, string id = "m1", string kind = Wire.Text, string? text = "Hi") =>
        new(id, proId, seekerId, kind, text, "Pro", "Seeker", null, null, null, null);

    private async Task<(TestPhone Worker, TestPhone Seeker, string ChatId)> ChatAsync(string name)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, name);
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, name + "Seeker");
        await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Hi po");
        return (worker, seeker, seeker.Seeker.ChatIdWith(worker.Uid));
    }

    // --- Chats ------------------------------------------------------------------

    [IntegrationFact]
    public async Task SameMessageSentTwice_IsStoredOnce()
    {
        // A phone on a bad network may resend a message it already sent: it must not appear twice.
        var (worker, seeker, chatId) = await ChatAsync("Ramon");
        var client = api.Client(api.Tokens.For(seeker.Uid));
        var resend = Message(worker.Uid, seeker.Uid, id: "resent-1");

        using (await client.PostAsJsonAsync($"chats/{chatId}/messages", resend, ApiJson.Default.SendMessageRequest)) { }
        using var again = await client.PostAsJsonAsync($"chats/{chatId}/messages", resend, ApiJson.Default.SendMessageRequest);

        again.StatusCode.Should().Be(HttpStatusCode.OK);
        (await seeker.Seeker.GetMessagesAsync(worker.Uid)).Messages.Count(m => m.Id == "resent-1").Should().Be(1);
    }

    [IntegrationFact]
    public async Task OnlyTheSeekerCanStartAChat()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Sol");
        await worker.Pro.PublishAsync();
        var chatId = ChatStore.IdFor(worker.Uid, "some-seeker");

        using var response = await api.Client(api.Tokens.For(worker.Uid))
            .PostAsJsonAsync($"chats/{chatId}/messages", Message(worker.Uid, "some-seeker"), ApiJson.Default.SendMessageRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "workers can't cold-message customers");
    }

    [IntegrationFact]
    public async Task StrangerWritingIntoAChat_IsForbidden()
    {
        var (worker, seeker, chatId) = await ChatAsync("Tito");

        using var response = await api.Client(api.Tokens.For("stranger"))
            .PostAsJsonAsync($"chats/{chatId}/messages", Message(worker.Uid, seeker.Uid, id: "x"), ApiJson.Default.SendMessageRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [IntegrationFact]
    public async Task MalformedMessages_AreBadRequests()
    {
        var (worker, seeker, chatId) = await ChatAsync("Uri");
        var client = api.Client(api.Tokens.For(seeker.Uid));

        using var wrongChat = await client.PostAsJsonAsync("chats/someone_else/messages", Message(worker.Uid, seeker.Uid), ApiJson.Default.SendMessageRequest);
        using var emptyText = await client.PostAsJsonAsync($"chats/{chatId}/messages", Message(worker.Uid, seeker.Uid, id: "e", text: "  "), ApiJson.Default.SendMessageRequest);
        using var unknownKind = await client.PostAsJsonAsync($"chats/{chatId}/messages", Message(worker.Uid, seeker.Uid, id: "k", kind: "video"), ApiJson.Default.SendMessageRequest);
        using var nullBody = await client.PostAsync($"chats/{chatId}/messages", Json("null"));

        wrongChat.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        emptyText.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        unknownKind.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        nullBody.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [IntegrationFact]
    public async Task SeekerOpensTheReply_ClearsTheirBadge_OnTheServerToo()
    {
        var (worker, seeker, chatId) = await ChatAsync("Vina");
        await worker.Pro.RefreshActivityAsync();
        await worker.Pro.ReplyAsync(worker.Pro.Chats.Single(), "Opo");
        await seeker.Seeker.RefreshActivityAsync();

        await seeker.Seeker.MarkReadAsync(worker.Uid);

        var fresh = await seeker.Get<SideGigPh.Maui.Services.IChatRepository>().GetAsync(chatId);
        fresh!.IsUnreadFor(SideGigPh.Maui.Models.ChatSide.Seeker).Should().BeFalse();
    }

    // --- Reviews and profile -------------------------------------------------------

    [IntegrationFact]
    public async Task ReviewWithZeroStars_OrNoBody_IsBadRequest()
    {
        var (worker, seeker, _) = await ChatAsync("Wen");
        var client = api.Client(api.Tokens.For(seeker.Uid));

        using var zeroStars = await client.PutAsJsonAsync("reviews", new SaveReviewRequest(worker.Uid, "W", 0, null, null), ApiJson.Default.SaveReviewRequest);
        using var nullBody = await client.PutAsync("reviews", Json("null"));

        zeroStars.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        nullBody.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [IntegrationFact]
    public async Task ProfileWithNoBody_IsBadRequest()
    {
        using var response = await api.Client(api.Tokens.For("uid-x")).PutAsync("users/me", Json("null"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [IntegrationFact]
    public async Task TwoAccountsClaimingTheSameProfilePhoto_SecondKeepsItsProfileWithoutThePhoto()
    {
        // Anti-fraud rule 2 for profile photos (the database's unique index), without losing the rest of the profile.
        var photo = new string('a', 63) + "1";
        var profile = new UserDto("ignored", "Name", null, null, Wire.English, Wire.Seeker, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, photo);

        using (await api.Client(api.Tokens.For("photo-owner")).PutAsJsonAsync("users/me", profile, ApiJson.Default.UserDto)) { }
        using var second = await api.Client(api.Tokens.For("photo-copier")).PutAsJsonAsync("users/me", profile, ApiJson.Default.UserDto);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await second.Content.ReadFromJsonAsync(ApiJson.Default.UserDto);
        saved!.ProfilePhotoId.Should().BeNull();
        saved.DisplayName.Should().Be("Name");
    }

    [IntegrationFact]
    public async Task ProfessionalBodyNull_IsBadBody()
    {
        using var response = await api.Client(api.Tokens.For("uid-y", TestPhone.UniqueNumber())).PutAsync("professionals/me", Json("null"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.BadBody);
    }

    // --- Publishing at the same moment ------------------------------------------

    [IntegrationFact]
    public async Task ManyAccountsPublishingTheSameNumberAtOnce_EndWithOneListing()
    {
        // Anti-fraud rule 1 under a race. The account holding the verified number takes it over from a stale one, so a
        // racer may take down a listing another racer just published (both then answered Published), or lose to the
        // unique index (PhoneTaken). Whatever the order, nothing crashes and one listing has the number at the end.
        // (Real accounts can't race like this: the sign-in provider gives a number to one account at a time.)
        var number = TestPhone.UniqueNumber();
        var attempts = Enumerable.Range(0, 6).Select(async i =>
        {
            using var scope = api.Server.Services.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<ProfessionalStore>();
            return await store.PublishAsync($"racer-{Guid.NewGuid():N}", number, Listing(i), CancellationToken.None);
        });

        var outcomes = await Task.WhenAll(attempts);

        outcomes.Should().OnlyContain(o => o == PublishOutcome.Published || o == PublishOutcome.PhoneTaken);
        outcomes.Should().Contain(PublishOutcome.Published);
        using var check = api.Server.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<SideGigPhDb>();
        (await db.Professionals.CountAsync(p => p.MobileNumber == number)).Should().Be(1, "one number, one listing");
    }

    [IntegrationFact]
    public async Task StoreCalledWithAnInvalidListing_RefusesIt()
    {
        using var scope = api.Server.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ProfessionalStore>();

        var outcome = await store.PublishAsync("someone", TestPhone.UniqueNumber(), Listing(0) with { CityIds = [] }, CancellationToken.None);

        outcome.Should().Be(PublishOutcome.Invalid);
    }

    private static PublishListingRequest Listing(int seed) => new(
        "Racer", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], null, null,
        Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
        [Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")],
        [new ReferenceDto($"Ref {seed}", TestPhone.UniqueNumber())]);

    // --- Photos -------------------------------------------------------------------

    [IntegrationFact]
    public async Task PhotoOver10MB_IsTooLarge()
    {
        using var response = await api.Client(api.Tokens.For("big")).PostAsync("photos", new ByteArrayContent(new byte[(10 * 1024 * 1024) + 10]));

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [IntegrationFact]
    public async Task BrokenJpeg_IsBrokenImage()
    {
        using var response = await api.Client(api.Tokens.For("broken")).PostAsync("photos", new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("broken-image");
    }

    [IntegrationFact]
    public async Task DeletePhoto_BadIdIsRefused_MissingPhotoIsFine()
    {
        var client = api.Client(api.Tokens.For("deleter"));

        using var badId = await client.DeleteAsync("photos/not-a-hash");
        using var missing = await client.DeleteAsync($"photos/{new string('b', 64)}");

        badId.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        missing.StatusCode.Should().Be(HttpStatusCode.NoContent, "deleting what's already gone is not an error");
    }

    [IntegrationFact]
    public async Task PrivatePhotoContainer_IsMadeReadableByUrl_OnTheFirstUpload()
    {
        // Photos show straight from their URL, so a private container (made by hand in the portal) is switched to public read.
        ForgetContainerCheck();
        api.Blobs.MakePrivate();

        await new TestPhone(api, "Wilma").Get<IPhotoStore>().SaveAsync(TestImages.Jpeg());

        api.Blobs.Access.Should().Be(PublicAccessType.Blob);
    }

    [IntegrationFact]
    public async Task StorageAccountThatBlocksPublicPhotos_FailsWithAClearMessage()
    {
        ForgetContainerCheck();
        api.Blobs.MakePrivate();
        api.Blobs.BlockPublicAccess = true;
        try
        {
            using var response = await api.Client(api.Tokens.For("blocked")).PostAsync("photos", new ByteArrayContent(TestImages.Jpeg()));

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Allow Blob anonymous access");
        }
        finally
        {
            api.Blobs.BlockPublicAccess = false;
            ForgetContainerCheck();
        }
    }

    // PhotoFunctions checks the container once per process; these tests need it to check again.
    private static void ForgetContainerCheck() =>
        typeof(PhotoFunctions).GetField("containerReady", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, false);
}
