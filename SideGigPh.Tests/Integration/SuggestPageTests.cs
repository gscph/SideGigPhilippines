using System.Net;
using Microsoft.Data.SqlClient;
using SideGigPh.Api;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The feedback board's public web page (<c>GET / POST suggest</c>, the Play testers' feedback link): anyone sees the
/// app's board, a look-alike is offered to upvote before a repeat is posted, a visitor (a cookie id) has one vote per
/// suggestion, and what the page posts is marked as from the web. Every test uses its own made-up tag (one board for
/// all tests) and its own visitor address (the per-address limits).
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SuggestPageTests(ApiTestHost api)
{
    private static string Tag() => $"zq{Guid.NewGuid():N}"[..10];

    [IntegrationFact]
    public async Task Page_OpensForAnyone_ShowsTheAppsBoard_AndGivesTheVisitorAnId()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        await ana.Get<FeedbackBoard>().PostAsync($"Dark mode please {tag}", "<b>at night</b>");

        using var response = await Visitor().GetAsync("suggest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain($"Dark mode please {tag}")
            .And.Contain("&lt;b&gt;at night&lt;/b&gt;", "what people wrote is shown as text, never as markup")
            .And.NotContain("<b>at night</b>");
        response.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith(SuggestFunctions.VoterCookie + "="));
    }

    [IntegrationFact]
    public async Task SomethingAlike_IsOfferedToUpvote_BeforeARepeatIsPosted()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        await ana.Get<FeedbackBoard>().PostAsync($"Pay with GCash wallet {tag}", null);

        using var response = await Visitor().PostAsync("suggest", Form(("title", $"GCash payment po {tag}")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Is it one of these?").And.Contain($"Pay with GCash wallet {tag}").And.Contain("None of these, post mine");
        (await ScalarAsync("SELECT COUNT(*) FROM FeedbackPosts WHERE Title LIKE @a", $"%{tag}")).Should().Be(1, "nothing new is posted yet");
    }

    [IntegrationFact]
    public async Task NoneOfThese_PostsIt_MarkedFromTheWeb_WithTheVisitorsVote()
    {
        var tag = Tag();

        using var response = await Visitor().PostAsync("suggest", Form(("title", $"Tagalog voice notes {tag}"), ("details", "For lolo"), ("step", "post")));

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().Contain("/api/suggest?posted=1#post-");
        (await ScalarAsync("SELECT COUNT(*) FROM FeedbackPosts WHERE Title = @a AND Source = 'web' AND Votes = 1 AND AuthorUid LIKE 'web-%'",
            $"Tagalog voice notes {tag}")).Should().Be(1);
    }

    [IntegrationFact]
    public async Task AVisitor_UpvotesOnce_AndCanTakeItBack()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var post = await ana.Get<FeedbackBoard>().PostAsync($"Book for tomorrow {tag}", null);
        var visitor = Visitor(cookie: Guid.NewGuid().ToString("N"));

        (await visitor.PostAsync($"suggest/{post.Id}/vote", Form(("on", "1")))).StatusCode.Should().Be(HttpStatusCode.Found);
        (await visitor.PostAsync($"suggest/{post.Id}/vote", Form(("on", "1")))).StatusCode.Should().Be(HttpStatusCode.Found);
        (await VotesAsync(post.Id)).Should().Be(2, "Ana's own vote and the visitor's, counted once");

        await visitor.PostAsync($"suggest/{post.Id}/vote", Form(("on", "0")));
        (await VotesAsync(post.Id)).Should().Be(1);
    }

    [IntegrationFact]
    public async Task ABotFillingTheHiddenField_LooksDone_ButNothingIsStored()
    {
        var tag = Tag();

        using var response = await Visitor().PostAsync("suggest",
            Form(("title", $"Cheap viagra {tag}"), ("step", "post"), (SuggestFunctions.TrapField, "http://spam.example")));

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        (await ScalarAsync("SELECT COUNT(*) FROM FeedbackPosts WHERE Title LIKE @a", $"%{tag}")).Should().Be(0);
    }

    [IntegrationFact]
    public async Task ATooShortIdea_IsRefusedOnThePage()
    {
        using var response = await Visitor().PostAsync("suggest", Form(("title", "hi"), ("step", "post")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Write at least");
    }

    /// <summary>A browser with its own address (and, when given, its saved visitor cookie).</summary>
    private HttpClient Visitor(string? cookie = null)
    {
        var client = api.Client();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}");
        if (cookie is not null)
        {
            client.DefaultRequestHeaders.Add("Cookie", $"{SuggestFunctions.VoterCookie}={cookie}");
        }

        return client;
    }

    private static FormUrlEncodedContent Form(params (string Name, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));

    private Task<int> VotesAsync(long postId) => ScalarAsync("SELECT Votes FROM FeedbackPosts WHERE Id = @a", postId);

    private async Task<int> ScalarAsync(string sql, params object[] values)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        for (var i = 0; i < values.Length; i++)
        {
            command.Parameters.AddWithValue($"@{(char)('a' + i)}", values[i]);
        }

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
