using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SideGigPh.AzureSQL;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Mcp.Tools;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The local MCP server's tools (<c>SideGigPh.Mcp</c>) answer from the real tables through <see cref="SideGigPhDb"/>:
/// the app's error reports as sent through the API, usage events as the API stores them.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class McpToolTests(ApiTestHost api)
{
    private static readonly DeviceStamp Phone = new("0.2.3 (41)", "abc1234", "prod", "android 15", "samsung SM-A256E");

    private SideGigPhDb Db() => new(new DbContextOptionsBuilder<SideGigPhDb>()
        .UseSqlServer(api.ConnectionString)
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        .Options);

    private static string Tag() => $"mcp{Guid.NewGuid():N}"[..12];

    [IntegrationFact]
    public async Task ErrorTools_FindTheAppsReports_AndOpenOne()
    {
        var tag = Tag();
        var sent = await new TestPhone(api, "McpReporter").Get<IErrorReportRepository>().SendAsync(Guid.NewGuid().ToString(), Phone,
            [new ErrorReport(ErrorKind.Crash, $"Crash.{tag}", "Boom", "System.Exception: boom\n   at X.Y()", "/seeker/find", "→ /seeker", 2,
                DateTimeOffset.UtcNow.AddMinutes(-3), DateTimeOffset.UtcNow)]);
        sent.Should().BeTrue();
        await using var db = Db();
        var tools = new ErrorReportTools(db);

        var recent = await tools.Recent(channel: "prod", kind: "crash", search: tag);
        var summary = await tools.Summary(channel: "PROD", top: 200);
        var byVersion = await tools.ByVersion(channel: "prod");

        recent.Should().ContainSingle().Which.Times.Should().Be(2);
        summary.Should().Contain(r => r.Source == $"Crash.{tag}" && r.Phones == 1 && r.Times == 2);
        byVersion.Should().Contain(r => r.CodeVersion == "abc1234" && r.Crashes > 0);
        (await tools.Recent(channel: "beta", search: tag)).Should().BeEmpty();

        var report = await tools.Report(recent[0].Id);
        report.Should().NotBeNull();
        report!.Detail.Should().Be("System.Exception: boom\n   at X.Y()");
        report.Route.Should().Be("/seeker/find");
        (await tools.Report(-1)).Should().BeNull();
    }

    [IntegrationFact]
    public async Task AnalyticsTools_CountTheEvents()
    {
        var term = Tag();
        await InsertEventAsync("search_empty", term);
        await InsertEventAsync("search_empty", term);
        await InsertEventAsync("search", term, results: 0);
        await using var db = Db();
        var tools = new AnalyticsTools(db);

        (await tools.UnmetDemand(top: 200)).Should().ContainSingle(r => r.SearchTerm == term).Which.Searches.Should().Be(2);
        (await tools.EventsPerDay("search_empty", days: 1)).Should().OnlyContain(r => r.EventName == "search_empty").And.Contain(r => r.Events >= 2 && r.People >= 1);
        (await tools.EventsPerWeek("search_empty", weeks: 1)).Should().ContainSingle().Which.Period.DayOfWeek.Should().Be(DayOfWeek.Monday);
        (await tools.DemandByTradeAndTown()).Should().Contain(r => r.Trade == "aircon" && r.Town == "angono" && r.Empty >= 1);
        (await tools.Fulfilment()).PostsCreated.Should().BeGreaterThanOrEqualTo(0);
        (await tools.WorkerFunnel()).SignedUp.Should().BeGreaterThanOrEqualTo(0);
        (await tools.TrustReports()).Should().NotBeNull();
    }

    private async Task InsertEventAsync(string name, string term, int? results = null)
    {
        await using var db = new SqlConnection(api.ConnectionString);
        await db.OpenAsync();
        await using var insert = new SqlCommand("""
            INSERT dbo.app_events (event_id, event_name, source, user_id, user_type, service_category, area, search_term, result_count)
            VALUES (NEWID(), @name, 'app', 'mcp-user', 'customer', 'aircon', 'angono', @term, @results)
            """, db);
        insert.Parameters.AddWithValue("@name", name);
        insert.Parameters.AddWithValue("@term", term);
        insert.Parameters.AddWithValue("@results", (object?)results ?? DBNull.Value);
        await insert.ExecuteNonQueryAsync();
    }
}
