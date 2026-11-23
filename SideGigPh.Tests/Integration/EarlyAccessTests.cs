using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The landing page's Early Access form (<c>POST early-access</c>, public): a good sign-up is kept in SQL, one row per address, a bad
/// address or phone is refused, a bot's is "done" with nothing kept, and a visitor is limited. Each test uses its own
/// visitor address (the per-address limit) and its own made-up email.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class EarlyAccessTests(ApiTestHost api)
{
    private static string Email() => $"t{Guid.NewGuid():N}"[..12] + "@gmail.com";

    [IntegrationFact]
    public async Task ASignUp_IsKept_Cleaned()
    {
        var email = Email();

        using var response = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest("  " + email.ToUpperInvariant() + " ", "iphone", "worker", "tl"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Saved(email)).Should().Be(("iphone", "worker", "tl"));
    }

    [IntegrationFact]
    public async Task UnknownSideAndLanguage_AreLeftOut()
    {
        var email = Email();

        using var response = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest(email, "android", "boss", "fr"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Saved(email)).Should().Be(("android", (string?)null, (string?)null));
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
        (await Saved(email)).Should().BeNull();
    }

    [IntegrationFact]
    public async Task TheSameAddressAgain_UpdatesItsOneRow()
    {
        var email = Email();

        using var first = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest(email, "android", "household", "en"));
        using var again = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest(email.ToUpperInvariant(), "iphone", null, null));

        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Saved(email)).Should().Be(("iphone", "household", "en"), "a later sign-up changes the phone and keeps what it left out");
    }

    [IntegrationFact]
    public async Task ABotFillingTheHiddenField_LooksDone_ButNothingIsKept()
    {
        var email = Email();

        using var response = await Visitor().PostAsJsonAsync("early-access", new EarlyAccessRequest(email, "android", null, null, "http://spam.example"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Saved(email)).Should().BeNull();
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

    /// <summary>The kept row's phone, side and language; null when there is none (each address is one row).</summary>
    private async Task<(string Phone, string? Role, string? Language)?> Saved(string email)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT Phone, Role, Language FROM dbo.EarlyAccessSignups WHERE Email = @email", connection);
        command.Parameters.AddWithValue("@email", email);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var row = (reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
        (await reader.ReadAsync()).Should().BeFalse("one row per address");
        return row;
    }

    /// <summary>A browser with its own address (the per-address limit).</summary>
    private HttpClient Visitor()
    {
        var client = api.Client();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}");
        return client;
    }
}
