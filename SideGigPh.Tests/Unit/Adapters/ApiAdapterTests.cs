using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// The app's HTTP layer (<c>Services/Api/</c>) against a pretend API. It must send the sign-in token, turn every
/// server answer into what the pages expect (phone taken, bad reference, photo taken…), show "nothing" instead of
/// crashing when offline, and not ask the server again for data it read moments ago (battery and data cost).
/// </summary>
public class ApiAdapterTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 13, 8, 0, 0, TimeSpan.Zero);

    private static string Json<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type) => JsonSerializer.Serialize(value, type);

    private static ListingDto ListingDto(string id = "pro-1") => new(
        id, "Maria", "+639171234567", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], "1-3", "under-500", null, [], null, 1);

    private static string Error(string code) => Json(new ApiError(code), ApiJson.Default.ApiError);

    // --- Listings ---------------------------------------------------------------

    [Fact]
    public async Task Search_SendsTheTokenAndReadsListings()
    {
        var server = new StubServer();
        server.Answer("GET professionals?categoryId=aircon&areaId=rizal", HttpStatusCode.OK, Json(new List<ListingDto> { ListingDto() }, ApiJson.Default.ListListingDto));
        var repo = new ApiProfessionalProfileRepository(server.Client());

        var listings = await repo.SearchAsync(new ProfessionalSearchQuery("aircon", "rizal"));

        listings.Should().ContainSingle().Which.DisplayName.Should().Be("Maria");
        server.Requests.Single().Token.Should().Be($"token-for-{TestData.WorkerUid}");
    }

    [Fact]
    public async Task Search_Again_WithinTheFreshWindow_MakesNoRequest_AndFillsTheListingCache()
    {
        var server = new StubServer();
        server.Answer("GET professionals?categoryId=aircon", HttpStatusCode.OK, Json(new List<ListingDto> { ListingDto() }, ApiJson.Default.ListListingDto));
        var repo = new ApiProfessionalProfileRepository(server.Client());

        await repo.SearchAsync(new ProfessionalSearchQuery("aircon", null));
        await repo.SearchAsync(new ProfessionalSearchQuery("aircon", null));
        var profile = await repo.GetByIdAsync("pro-1");

        server.Requests.Should().ContainSingle("the second search and the profile come from memory");
        profile!.Id.Should().Be("pro-1");
    }

    [Fact]
    public async Task Search_Offline_ShowsNothingInsteadOfCrashing()
    {
        var server = new StubServer { Offline = true };
        var repo = new ApiProfessionalProfileRepository(server.Client());

        (await repo.SearchAsync(new ProfessionalSearchQuery("aircon", null))).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_FailedRead_IsNotCached_SoTheNextTryAsksAgain()
    {
        var server = new StubServer();
        server.Answer("GET professionals?categoryId=aircon", HttpStatusCode.InternalServerError, Error(ApiErrors.Exception));
        var repo = new ApiProfessionalProfileRepository(server.Client());

        (await repo.SearchAsync(new ProfessionalSearchQuery("aircon", null))).Should().BeEmpty();
        await repo.SearchAsync(new ProfessionalSearchQuery("aircon", null));

        server.CountOf("GET professionals?categoryId=aircon").Should().Be(2);
    }

    [Fact]
    public async Task GetById_Missing_IsNull_AndNotCached()
    {
        var server = new StubServer();
        var repo = new ApiProfessionalProfileRepository(server.Client());

        (await repo.GetByIdAsync("nobody")).Should().BeNull();
        (await repo.HasProfileAsync("nobody")).Should().BeFalse();

        server.CountOf("GET professionals/nobody").Should().Be(2, "a listing may be published any moment");
    }

    [Fact]
    public async Task GetById_Found_IsCached()
    {
        var server = new StubServer();
        server.Answer("GET professionals/pro-1", HttpStatusCode.OK, Json(ListingDto(), ApiJson.Default.ListingDto));
        var repo = new ApiProfessionalProfileRepository(server.Client());

        (await repo.HasProfileAsync("pro-1")).Should().BeTrue();
        await repo.GetByIdAsync("pro-1");

        server.CountOf("GET professionals/pro-1").Should().Be(1);
    }

    [Fact]
    public async Task Publish_Ok_IsPublished_AndClearsTheCaches()
    {
        var server = new StubServer();
        server.Answer("GET professionals?categoryId=aircon", HttpStatusCode.OK, "[]");
        server.Answer("PUT professionals/me", HttpStatusCode.OK, Json(ListingDto(), ApiJson.Default.ListingDto));
        var repo = new ApiProfessionalProfileRepository(server.Client());
        await repo.SearchAsync(new ProfessionalSearchQuery("aircon", null));

        var result = await repo.PublishAsync(TestData.ReadyDraft().ToListing(TestData.WorkerUid), TestData.ReadyDraft().References);
        await repo.SearchAsync(new ProfessionalSearchQuery("aircon", null));

        result.Should().Be(PublishResult.Published);
        server.CountOf("GET professionals?categoryId=aircon").Should().Be(2, "a new listing must show in the next search");
        var sent = JsonSerializer.Deserialize(server.Requests.Single(r => r.Route == "PUT professionals/me").Body!, ApiJson.Default.PublishListingRequest)!;
        sent.References.Should().ContainSingle();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, ApiErrors.PhoneTaken, PublishResult.PhoneTaken)]
    [InlineData(HttpStatusCode.BadRequest, "invalid-references", PublishResult.BadReference)]
    public async Task Publish_Refused_TellsThePageWhy(HttpStatusCode status, string code, PublishResult expected)
    {
        var server = new StubServer();
        server.Answer("PUT professionals/me", status, Error(code));
        var repo = new ApiProfessionalProfileRepository(server.Client());

        (await repo.PublishAsync(TestData.ReadyDraft().ToListing(), [])).Should().Be(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":\"invalid-cityIds\"}")]
    [InlineData(HttpStatusCode.Conflict, "{\"error\":\"photo-in-use\"}")]
    [InlineData(HttpStatusCode.Conflict, "not json")]
    [InlineData(HttpStatusCode.Forbidden, "{\"error\":\"phone-required\"}")]
    public async Task Publish_OtherRefusals_Throw_SoThePageSaysItDidNotPublish(HttpStatusCode status, string body)
    {
        var server = new StubServer();
        server.Answer("PUT professionals/me", status, body);
        var repo = new ApiProfessionalProfileRepository(server.Client());

        var publish = () => repo.PublishAsync(TestData.ReadyDraft().ToListing(), []);

        (await publish.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(status);
    }

    [Fact]
    public async Task Remove_DeletesTheCallersListing()
    {
        var server = new StubServer();
        server.Answer("DELETE professionals/me", HttpStatusCode.NoContent);
        var repo = new ApiProfessionalProfileRepository(server.Client());

        await repo.RemoveAsync(TestData.WorkerUid);

        server.Requests.Should().ContainSingle().Which.Route.Should().Be("DELETE professionals/me");
    }

    // --- Photos -------------------------------------------------------------------

    private static ApiPhotoStore Photos(StubServer server) => new(server.Client(), server.Options, NullLogger<ApiPhotoStore>.Instance);

    [Fact]
    public async Task PhotoUpload_ReturnsTheServersId_AndShowsItsUrl()
    {
        var server = new StubServer();
        var id = TestData.PhotoId(7);
        server.Answer("POST photos", HttpStatusCode.OK, Json(new PhotoDto(id, "https://blob/photos/x.jpg"), ApiJson.Default.PhotoDto));
        var photos = Photos(server);

        (await photos.SaveAsync([1, 2, 3])).Should().Be(id);
        (await photos.GetUrlAsync(id)).Should().Be("https://blob/photos/x.jpg");
        (await photos.GetUrlAsync("other")).Should().Be($"{FakePhotoStore.BaseUrl}other.jpg", "older photos show from the public container");
    }

    [Fact]
    public async Task PhotoUpload_PhotoOfAnotherAccount_IsTaken()
    {
        var server = new StubServer();
        server.Answer("POST photos", HttpStatusCode.Conflict, Error(ApiErrors.PhotoInUse));

        var upload = () => Photos(server).SaveAsync([1]);

        (await upload.Should().ThrowAsync<PhotoStoreException>()).Which.Taken.Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, "null")]
    [InlineData(HttpStatusCode.OK, "{broken")]
    public async Task PhotoUpload_ServerProblem_IsAnUploadFailure_NotTaken(HttpStatusCode status, string body)
    {
        var server = new StubServer();
        server.Answer("POST photos", status, body);

        var upload = () => Photos(server).SaveAsync([1]);

        (await upload.Should().ThrowAsync<PhotoStoreException>()).Which.Taken.Should().BeFalse();
    }

    [Fact]
    public async Task PhotoUpload_Offline_IsAnUploadFailure()
    {
        var upload = () => Photos(new StubServer { Offline = true }).SaveAsync([1]);

        (await upload.Should().ThrowAsync<PhotoStoreException>()).Which.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task PhotoDelete_Failing_NeverStopsTheUsersChange()
    {
        var refused = new StubServer();
        refused.Answer("DELETE photos/abc", HttpStatusCode.Forbidden);
        var offline = new StubServer { Offline = true };

        await Photos(refused).Invoking(p => p.DeleteAsync("abc")).Should().NotThrowAsync();
        await Photos(offline).Invoking(p => p.DeleteAsync("abc")).Should().NotThrowAsync();
        refused.Requests.Should().ContainSingle();
    }

    // --- Chats --------------------------------------------------------------------

    private static ChatDto ChatDto(string lastMessageId = "m1") => new(
        "pro-1_seeker-1", "pro-1", "seeker-1", "Maria", "Ana", "aircon-cleaning", "taytay", "today", "under-500",
        new MessageDto(lastMessageId, "pro-1_seeker-1", "seeker-1", Wire.Text, "Hi po", At), null, null, At);

    [Fact]
    public async Task Chats_InboxChatAndMessages_AreRead()
    {
        var server = new StubServer();
        server.Answer("GET chats?side=seeker", HttpStatusCode.OK, Json(new List<ChatDto> { ChatDto() }, ApiJson.Default.ListChatDto));
        server.Answer("GET chats/pro-1_seeker-1", HttpStatusCode.OK, Json(ChatDto(), ApiJson.Default.ChatDto));
        server.Answer("GET chats/pro-1_seeker-1/messages", HttpStatusCode.OK,
            Json(new List<MessageDto> { ChatDto().LastMessage, ChatDto().LastMessage with { Id = "m2", Kind = Wire.Call, Text = null } }, ApiJson.Default.ListMessageDto));
        var chats = new ApiChatRepository(server.Client(), NullLogger<ApiChatRepository>.Instance);

        (await chats.GetInboxAsync("seeker-1", ChatSide.Seeker)).Should().ContainSingle().Which.LastMessage.Text.Should().Be("Hi po");
        (await chats.GetAsync("pro-1_seeker-1"))!.ServiceId.Should().Be("aircon-cleaning");
        (await chats.GetMessagesAsync("pro-1_seeker-1")).Select(m => m.Kind).Should().Equal(ChatMessageKind.Text, ChatMessageKind.Call);
    }

    [Fact]
    public async Task Chats_Offline_ShowEmptyInbox()
    {
        var chats = new ApiChatRepository(new StubServer { Offline = true }.Client(), NullLogger<ApiChatRepository>.Instance);

        (await chats.GetInboxAsync("p", ChatSide.Professional)).Should().BeEmpty();
        (await chats.GetAsync("x")).Should().BeNull();
        (await chats.GetMessagesAsync("x")).Should().BeEmpty();
    }

    [Fact]
    public async Task Chats_Send_ReturnsTheChatAsSaved()
    {
        var server = new StubServer();
        server.Answer("POST chats/pro-1_seeker-1/messages", HttpStatusCode.OK, Json(ChatDto("m9"), ApiJson.Default.ChatDto));
        var chats = new ApiChatRepository(server.Client(), NullLogger<ApiChatRepository>.Instance);
        var chat = ChatDto().ToModel();

        var saved = await chats.SendAsync(chat, chat.LastMessage with { Id = "m9" });

        saved.LastMessage.Id.Should().Be("m9");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "null")]
    [InlineData(HttpStatusCode.Forbidden, "{\"error\":\"forbidden\"}")]
    public async Task Chats_SendRefusedOrEmpty_Throws_SoThePageSaysItDidNotSend(HttpStatusCode status, string body)
    {
        var server = new StubServer();
        server.Answer("POST chats/pro-1_seeker-1/messages", status, body);
        var chats = new ApiChatRepository(server.Client(), NullLogger<ApiChatRepository>.Instance);
        var chat = ChatDto().ToModel();

        await chats.Invoking(c => c.SendAsync(chat, chat.LastMessage)).Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Chats_MarkReadFailing_IsHarmless()
    {
        var refused = new StubServer();
        var offline = new StubServer { Offline = true };

        await new ApiChatRepository(refused.Client(), NullLogger<ApiChatRepository>.Instance)
            .Invoking(c => c.MarkReadAsync("x", ChatSide.Professional)).Should().NotThrowAsync();
        await new ApiChatRepository(offline.Client(), NullLogger<ApiChatRepository>.Instance)
            .Invoking(c => c.MarkReadAsync("x", ChatSide.Professional)).Should().NotThrowAsync();
        refused.Requests.Single().Route.Should().Be("POST chats/x/read?side=professional");
    }

    [Fact]
    public async Task Chats_DeleteMine_CallsTheServer()
    {
        var server = new StubServer();
        server.Answer("DELETE chats/mine", HttpStatusCode.NoContent);

        await new ApiChatRepository(server.Client(), NullLogger<ApiChatRepository>.Instance).DeleteForPersonAsync("me");

        server.CountOf("DELETE chats/mine").Should().Be(1);
    }

    // The open chat checks for a new message every few seconds; this waits for two real checks (about 10 s):
    // the first finds nothing new and stays quiet, the second finds the new message.
    [Fact]
    public async Task Chats_Watch_StaysQuietUntilANewMessage_ThenReportsIt()
    {
        var server = new StubServer();
        server.Answer("GET chats/pro-1_seeker-1", HttpStatusCode.OK, Json(ChatDto("m1"), ApiJson.Default.ChatDto));
        var chats = new ApiChatRepository(server.Client(), NullLogger<ApiChatRepository>.Instance);
        var reported = new List<string>();
        var arrived = new TaskCompletionSource<Chat>();

        using (chats.Watch("pro-1_seeker-1", "m1", chat =>
        {
            reported.Add(chat.LastMessage.Id);
            arrived.TrySetResult(chat);
        }))
        {
            while (server.CountOf("GET chats/pro-1_seeker-1") == 0)
            {
                await Task.Delay(100);
            }

            server.Answer("GET chats/pro-1_seeker-1", HttpStatusCode.OK, Json(ChatDto("m2"), ApiJson.Default.ChatDto));
            var winner = await Task.WhenAny(arrived.Task, Task.Delay(ApiChatRepository.WatchInterval * 3));
            winner.Should().Be(arrived.Task, "a new message shows without reopening the chat");
        }

        reported.Should().Equal("m2");
    }

    // --- Reviews and profile ----------------------------------------------------------

    [Fact]
    public async Task Reviews_ReadSaveAndDelete()
    {
        var server = new StubServer();
        var dto = new ReviewDto("pro-1", "seeker-1", "Ana", 5, null, "aircon-cleaning", At, At);
        server.Answer("GET reviews?professionalId=pro-1", HttpStatusCode.OK, Json(new List<ReviewDto> { dto }, ApiJson.Default.ListReviewDto));
        server.Answer("GET reviews?seekerId=seeker-1", HttpStatusCode.OK, "[]");
        server.Answer("PUT reviews", HttpStatusCode.NoContent);
        server.Answer("DELETE reviews/mine", HttpStatusCode.NoContent);
        var reviews = new ApiReviewRepository(server.Client());

        var about = await reviews.GetForProfessionalAsync("pro-1");
        (await reviews.GetBySeekerAsync("seeker-1")).Should().BeEmpty();
        await reviews.SaveAsync(about[0]);
        await reviews.DeleteForPersonAsync("seeker-1");

        about.Single().Id.Should().Be("pro-1_seeker-1");
        server.CountOf("PUT reviews").Should().Be(1);
        server.CountOf("DELETE reviews/mine").Should().Be(1);
    }

    [Fact]
    public async Task Reviews_SaveRefused_Throws()
    {
        var server = new StubServer();
        server.Answer("PUT reviews", HttpStatusCode.Forbidden, Error(ApiErrors.NoChat));
        var review = new Review("x", "pro-1", "s", "Ana", 5, null, null, At);

        await new ApiReviewRepository(server.Client()).Invoking(r => r.SaveAsync(review)).Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Profile_GetSaveDelete()
    {
        var server = new StubServer();
        var dto = new UserDto("uid", "Juan", null, null, Wire.English, Wire.Seeker, null, At, At);
        server.Answer("GET users/me", HttpStatusCode.OK, Json(dto, ApiJson.Default.UserDto));
        server.Answer("PUT users/me", HttpStatusCode.OK, Json(dto, ApiJson.Default.UserDto));
        server.Answer("DELETE users/me", HttpStatusCode.NoContent);
        var users = new ApiUserRepository(server.Client());

        var profile = await users.GetAsync("uid");
        await users.SaveAsync(profile!);
        await users.DeleteAsync("uid");

        profile!.Role.Should().Be(UserRole.Seeker);
        server.Requests.Select(r => r.Route).Should().Equal("GET users/me", "PUT users/me", "DELETE users/me");
    }

    [Fact]
    public async Task Device_IsSentAsTokenAndPlatform()
    {
        var server = new StubServer();
        server.Answer("PUT users/me/devices", HttpStatusCode.NoContent);

        await new ApiUserRepository(server.Client()).SaveDeviceAsync("fcm-1", Wire.Ios);

        var request = server.Requests.Single();
        request.Route.Should().Be("PUT users/me/devices");
        request.Body.Should().Be("""{"token":"fcm-1","platform":"ios"}""");
    }

    [Fact]
    public async Task Device_ServerRefuses_Throws()
    {
        var server = new StubServer();
        server.Answer("PUT users/me/devices", HttpStatusCode.BadRequest);

        var save = () => new ApiUserRepository(server.Client()).SaveDeviceAsync("fcm-1", Wire.Android);

        await save.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Profile_NotSavedYet_IsNull()
    {
        (await new ApiUserRepository(new StubServer().Client()).GetAsync("uid")).Should().BeNull();
    }

    [Fact]
    public async Task NotSignedIn_SendsNoToken()
    {
        var server = new StubServer();
        var client = new ApiClient(server.Options, new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        using var _ = await client.SendAsync(HttpMethod.Get, "users/me");

        server.Requests.Single().Token.Should().BeNull();
    }

    // MauiProgram calls AddSideGigPhApi: every port must resolve, with the API photo store replacing the on-device one.
    [Fact]
    public void AddSideGigPhApi_RegistersEveryPort()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IIdentityProvider>(new FakeIdentity(null))
            .AddSideGigPhApi(new StubServer().Options)
            .BuildServiceProvider();

        services.GetRequiredService<IPhotoStore>().Should().BeOfType<ApiPhotoStore>();
        services.GetRequiredService<IProfessionalProfileRepository>().Should().BeOfType<ApiProfessionalProfileRepository>();
        services.GetRequiredService<IUserRepository>().Should().BeOfType<ApiUserRepository>();
        services.GetRequiredService<IChatRepository>().Should().BeOfType<ApiChatRepository>();
        services.GetRequiredService<IReviewRepository>().Should().BeOfType<ApiReviewRepository>();
    }
}
