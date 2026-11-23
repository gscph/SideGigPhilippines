using System.Net;
using System.Net.Http.Json;
using SideGigPh.Api;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The landing page's Early Access form (<c>POST early-access</c>, public): a good sign-up is emailed to the team, a bad
/// address or phone is refused, a bot's is "done" with nothing sent, and a visitor is limited. Each test uses its own
/// visitor address (the per-address limit) and its own made-up email.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class EarlyAccessTests(ApiTestHost api)
{
    private static string Email() => $"t{Guid.NewGuid():N}"[..12] + "@gmail.com";

    [IntegrationFact]
    public async Task ASignUp_IsEmailedToTheTeam_Cleaned()
    {
        var email = Email();

        using var response = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest("  " + email.ToUpperInvariant() + " ", "iphone", "worker", "tl"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var sent = api.EarlyAccessMails.Sent.Should().ContainSingle(s => s.Email == email).Subject;
        sent.Phone.Should().Be("iphone");
        sent.Role.Should().Be("worker");
        sent.Language.Should().Be("tl");
    }

    [IntegrationFact]
    public async Task UnknownSideAndLanguage_AreLeftOut()
    {
        var email = Email();

        using var response = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest(email, "android", "boss", "fr"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var sent = api.EarlyAccessMails.Sent.Should().ContainSingle(s => s.Email == email).Subject;
        sent.Role.Should().BeNull();
        sent.Language.Should().BeNull();
    }

    [IntegrationFact]
    public async Task ABadEmailOrPhone_IsRefused_AndNothingIsSent()
    {
        var email = Email();

        using var badEmail = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest("juan@gmail", "android", null, null));
        using var badPhone = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest(email, "nokia", null, null));

        badEmail.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await badEmail.Content.ReadFromJsonAsync<ApiError>())!.Error.Should().Be("invalid-email");
        badPhone.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await badPhone.Content.ReadFromJsonAsync<ApiError>())!.Error.Should().Be("invalid-phone");
        api.EarlyAccessMails.Sent.Should().NotContain(s => s.Email == email);
    }

    [IntegrationFact]
    public async Task ABotFillingTheHiddenField_LooksDone_ButNothingIsSent()
    {
        var email = Email();

        using var response = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest(email, "android", null, null, "http://spam.example"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        api.EarlyAccessMails.Sent.Should().NotContain(s => s.Email == email);
    }

    /// <summary>Sign-ups an hour from one address (<c>CallerLimits</c>: phones behind one carrier address share it).</summary>
    private const int PerVisitor = 20;

    [IntegrationFact]
    public async Task OneVisitor_IsLimited()
    {
        var visitor = Visitor();
        var answers = new List<HttpStatusCode>();
        for (var i = 0; i <= PerVisitor; i++)
        {
            using var response = await visitor.PostAsJsonAsync("early-access", new EarlyAccessRequest(Email(), "android", null, null));
            answers.Add(response.StatusCode);
        }

        answers.Take(PerVisitor).Should().AllBeEquivalentTo(HttpStatusCode.NoContent);
        answers[PerVisitor].Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>A browser with its own address (the per-address limit).</summary>
    private HttpClient Visitor()
    {
        var client = api.Client();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}");
        return client;
    }
}
