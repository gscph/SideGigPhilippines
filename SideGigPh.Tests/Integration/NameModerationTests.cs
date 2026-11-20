using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// A person's display name (<c>PUT users/me</c>) is what others see in chats and reviews, so a rude one is refused
/// (<c>422 text-not-allowed</c>) and never saved. The app's name page asks first (<see cref="LocalAccount"/> as its
/// <see cref="IDisplayNameCheck"/>), so the refused name is never set on the account either.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class NameModerationTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task ARudeName_IsRefusedByTheNamePagesCheck_AndNotSaved_AFineOneIs()
    {
        var ana = new TestPhone(api, "Ana");
        IDisplayNameCheck check = ana.Account;

        (await check.AllowsAsync("Juan gago ka")).Should().BeFalse("the word list knows it");
        (await check.AllowsAsync($"Juan {FakeTextModerator.Marker}")).Should().BeFalse("Content Safety flags it");
        (await ana.Get<IUserRepository>().GetAsync(ana.Uid)).Should().BeNull("nothing was saved");

        (await check.AllowsAsync("Ana Santos")).Should().BeTrue();
        (await ana.Get<IUserRepository>().GetAsync(ana.Uid))!.DisplayName.Should().Be("Ana Santos");
    }

    [IntegrationFact]
    public async Task ARudeName_FromAnyApp_Is422_TextNotAllowed_AndTheSavedNameStays()
    {
        var client = api.Client(api.Tokens.For($"name-{Guid.NewGuid():N}"));
        using (var first = await client.PutAsJsonAsync("users/me", Profile("Ben Cruz"), ApiJson.Default.UserDto))
        {
            first.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var rude = await client.PutAsJsonAsync("users/me", Profile("Ben gago ka"), ApiJson.Default.UserDto);

        rude.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await rude.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.TextNotAllowed);
        (await client.GetFromJsonAsync("users/me", ApiJson.Default.UserDto))!.DisplayName.Should().Be("Ben Cruz");
    }

    private static UserDto Profile(string name) =>
        new("ignored", name, null, null, Wire.English, Wire.Seeker, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
