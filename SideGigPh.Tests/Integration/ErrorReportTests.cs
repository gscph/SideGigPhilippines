using System.Net.Http.Json;
using Azure.Data.Tables;
using SideGigPh.Api;
using SideGigPh.Api.Telemetry;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Error reports from the app reach the <c>ErrorReports</c> table (Table Storage, never the database) through the real API: scrubbed again on the server (an older
/// app's slip is still caught), tied to the install and never the account, bad batches refused (the app then drops
/// them), each account limited, and nothing kept past 90 days.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ErrorReportTests(ApiTestHost api)
{
    // What a real Firebase uid looks like (the test people's ids don't).
    private const string FirebaseUid = "Xy7kP2mQ9rT4vW1zA3bC5dE8fG6h";

    private static readonly DeviceStamp Phone = new("1.0 (7)", "48466c9-dirty", "beta", "android 14", "samsung SM-A155F");

    private static ErrorReport Report(string message, ErrorKind kind = ErrorKind.Error) =>
        new(kind, "SideGigPh.Maui.Services.Api.ApiClient", message, "System.Exception: boom\n   at X.Y()", "/start/pro/done",
            "08:00:00 → /start/pro/7", 3, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow);

    [IntegrationFact]
    public async Task Reports_AreKept_ScrubbedAndWithoutTheAccount()
    {
        var phone = new TestPhone(api, "Reporter");
        var installId = Guid.NewGuid().ToString();

        var kept = await phone.Get<IErrorReportRepository>().SendAsync(installId, Phone,
            [Report($"Couldn't save {phone.PhoneNumber} for {FirebaseUid}"), Report("Stuck", ErrorKind.Slow)]);

        kept.Should().BeTrue();
        var rows = Rows(installId);
        var digits = phone.PhoneNumber![1..];
        rows.Should().HaveCount(2);
        rows.Should().ContainSingle(r => r.GetString("Kind") == "slow" && r.GetInt32("Count") == 3 && r.GetString("Route") == "/start/pro/done"
            && r.GetString("Platform") == "android 14" && r.GetString("CodeVersion") == "48466c9-dirty" && r.PartitionKey == "beta"
            && r.GetString("Device") == "samsung SM-A155F");
        rows.Should().NotContain(r => r.GetString("Message")!.Contains(FirebaseUid) || r.GetString("Message")!.Contains(digits),
            "the server scrubs what an app let through");
        rows.Should().NotContain(r => r.Keys.Any(k => k.Contains("Uid", StringComparison.OrdinalIgnoreCase)), "reports are never tied to the account");
    }

    [IntegrationFact]
    public async Task BadBatches_AreRefused_SoTheAppDropsThem()
    {
        var reports = new TestPhone(api, "BadReporter").Get<IErrorReportRepository>();
        var installId = Guid.NewGuid().ToString();

        (await reports.SendAsync("not-a-guid", Phone, [Report("x")])).Should().BeFalse();
        (await reports.SendAsync(installId, Phone, [])).Should().BeFalse();
        (await reports.SendAsync(installId, Phone, [.. Enumerable.Range(0, ErrorReportRules.MaxReports + 1).Select(i => Report($"p{i}"))])).Should().BeFalse();
        (await reports.SendAsync(installId, Phone with { AppVersion = " " }, [Report("x")])).Should().BeFalse();
        (await reports.SendAsync(installId, Phone with { Channel = "staging" }, [Report("x")])).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task UnknownKind_IsRefused()
    {
        var client = api.Client(api.Tokens.For($"kind-{Guid.NewGuid():N}"));
        var batch = new ErrorReportBatch(Guid.NewGuid().ToString(), "1.0", "48466c9", AppChannels.Beta, "android 14", null,
            [new ErrorReportDto("panic", "s", "m", null, null, null, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)]);
        using var content = new ByteArrayContent(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(batch, ApiJson.Default.ErrorReportBatch));
        content.Headers.ContentType = new("application/json");

        using var response = await client.PostAsync("error-reports", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [IntegrationFact]
    public async Task OneAccount_PastTheLimit_IsTold429()
    {
        var reports = new TestPhone(api, "Chatty").Get<IErrorReportRepository>();
        var installId = Guid.NewGuid().ToString();
        for (var i = 0; i < CallerLimits.Rules[Limited.ErrorReport].Permits; i++)
        {
            (await reports.SendAsync(installId, Phone, [Report("again")])).Should().BeTrue();
        }

        var tooMany = () => reports.SendAsync(installId, Phone, [Report("again")]);

        (await tooMany.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(System.Net.HttpStatusCode.TooManyRequests);
    }

    [IntegrationFact]
    public async Task ReportsOlderThan90Days_AreDeleted()
    {
        var old = DateTimeOffset.UtcNow - ErrorReportTable.KeepFor - TimeSpan.FromDays(1);
        var oldKey = NewestFirst.Key(old);
        await api.ErrorReports.AddAsync(new TableEntity(AppChannels.Beta, oldKey) { ["Message"] = "old", ["LastAt"] = old }, default);
        var tables = new TelemetryTables(api.ErrorReports, api.OtherJobNotes);

        // A fresh store, as on a new server instance whose daily clean-up is due.
        await new ErrorReportTable(tables, new LaterClock()).SaveAsync(
            new ErrorReportBatch(Guid.NewGuid().ToString(), "1.0", "abc1234", AppChannels.Beta, "android 14", null,
                [new ErrorReportDto(ErrorKinds.Error, "s", "new", null, null, null, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)]),
            default);

        api.ErrorReports.All.Should().NotContain(r => r.RowKey == oldKey);
    }

    [IntegrationFact]
    public async Task Reading_ShowsTheNewestFirst_AndFiltersByChannel()
    {
        var installId = Guid.NewGuid().ToString();
        var reports = new TestPhone(api, "Reader").Get<IErrorReportRepository>();
        await reports.SendAsync(installId, Phone, [Report("read me")]);
        var client = api.Client(api.Tokens.For($"reader-{Guid.NewGuid():N}"));

        var beta = await client.GetFromJsonAsync<List<ErrorReportView>>("error-reports?take=200&channel=beta");
        var prod = await client.GetFromJsonAsync<List<ErrorReportView>>("error-reports?take=200&channel=prod");

        beta.Should().Contain(r => r.Message == "read me");
        beta!.Select(r => r.LastAt).Should().BeInDescendingOrder();
        prod.Should().NotContain(r => r.Message == "read me");
    }

    private List<TableEntity> Rows(string installId) => [.. api.ErrorReports.All.Where(r => r.GetString("InstallId") == installId)];

    // Far enough ahead that this store's once-a-day clean-up is due whatever the others did.
    private sealed class LaterClock : TimeProvider
    {
        private readonly DateTimeOffset _now = DateTimeOffset.UtcNow + TimeSpan.FromDays(2);

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
