using System.Net;
using System.Net.Http.Json;
using System.Text;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The API's doors: every route the app calls exists, nobody gets in without a valid Firebase token, people can only
/// reach their own chats, and broken requests get a clear error code instead of a crash.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ApiWiringTests(ApiTestHost api)
{
    // If a function's route or method changes, phones still on the old app call a door that no longer exists.
    [Fact]
    public void EveryRouteTheAppCalls_IsServed() =>
        ApiTestHost.Routes.Should().BeEquivalentTo(
            "GET users/me", "PUT users/me", "DELETE users/me", "PUT users/me/devices",
            "GET professionals", "GET search/professionals", "GET professionals/{id}", "PUT professionals/me", "DELETE professionals/me",
            "GET reviews", "PUT reviews", "DELETE reviews/mine",
            "GET chats", "GET chats/{chatId}", "GET chats/{chatId}/messages", "POST chats/{chatId}/messages",
            "POST chats/{chatId}/read", "DELETE chats/mine",
            "POST photos", "DELETE photos/{id}",
            "GET config", "POST other-jobs", "POST error-reports");

    /// <summary>Public reference data: the trades and jobs, read before the account is ready. Nothing else is public.</summary>
    private static readonly string[] PublicRoutes = ["GET config"];

    [IntegrationFact]
    public async Task EveryRoute_WithoutAToken_Is401()
    {
        var anonymous = api.Client();
        foreach (var route in ApiTestHost.Routes.Except(PublicRoutes))
        {
            var parts = route.Split(' ');
            var path = parts[1].Replace("{id}", "x").Replace("{chatId}", "x");
            using var request = new HttpRequestMessage(new HttpMethod(parts[0]), path) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };

            using var response = await anonymous.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"{route} must check the sign-in token");
        }
    }

    [IntegrationFact]
    public async Task PublicRoutes_AnswerWithoutAToken()
    {
        using var response = await api.Client().GetAsync("config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [IntegrationFact]
    public async Task TokenFromAnotherFirebaseProject_OrExpired_Is401()
    {
        using var otherProject = await api.Client(api.Tokens.For("uid", issuerProject: "someone-elses-app")).GetAsync("users/me");
        using var expired = await api.Client(api.Tokens.For("uid", expires: DateTime.UtcNow.AddHours(-1))).GetAsync("users/me");
        using var garbage = await api.Client("not-a-token").GetAsync("users/me");

        otherProject.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        garbage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [IntegrationFact]
    public async Task SomeoneElsesChat_IsNotVisible()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Dan");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Eve");
        await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Private po");
        var chatId = seeker.Seeker.ChatIdWith(worker.Uid);

        var snoop = api.Client(api.Tokens.For("snoop"));
        using var chat = await snoop.GetAsync($"chats/{chatId}");
        using var messages = await snoop.GetAsync($"chats/{chatId}/messages");

        chat.StatusCode.Should().Be(HttpStatusCode.NotFound);
        messages.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [IntegrationFact]
    public async Task ChatWithAWorkerWhoHasNoListing_CannotBeStarted()
    {
        var seeker = await TestPhone.SeekerAsync(api, "Fay");
        var ghost = SideGigPh.Tests.TestSupport.TestData.Listing("Ghost") ;

        var send = () => seeker.Seeker.SendAsync(new SideGigPh.Maui.Models.ProfessionalListing
        {
            Id = $"ghost-{Guid.NewGuid():N}", DisplayName = ghost.DisplayName, CategoryIds = ghost.CategoryIds,
            ServiceIds = ghost.ServiceIds, CityIds = ghost.CityIds, MobileNumber = ghost.MobileNumber,
        }, "Hello?");

        await send.Should().ThrowAsync<HttpRequestException>();
    }

    [IntegrationFact]
    public async Task BrokenJsonBody_IsBadBody()
    {
        var client = api.Client(api.Tokens.For("uid"));

        using var response = await client.PutAsync("users/me", new StringContent("{not json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.BadBody);
    }

    [IntegrationFact]
    public async Task MissingQueryValues_AreClearErrors()
    {
        var client = api.Client(api.Tokens.For("uid"));

        using var search = await client.GetAsync("professionals");
        using var reviews = await client.GetAsync("reviews");
        using var inbox = await client.GetAsync("chats");
        using var read = await client.PostAsync("chats/x_y/read", null);

        search.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        reviews.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        inbox.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        read.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
