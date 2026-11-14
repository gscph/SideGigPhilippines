using Microsoft.Data.SqlClient;
using SideGigPh.Api;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Error reports from the app reach <c>ErrorReports</c> through the real API: scrubbed again on the server (an older
/// app's slip is still caught), tied to the install and never the account, bad batches refused (the app then drops
/// them), each account limited, and nothing kept past 90 days.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ErrorReportTests(ApiTestHost api)
{
    // What a real Firebase uid looks like (the test people's ids don't).
    private const string FirebaseUid = "Xy7kP2mQ9rT4vW1zA3bC5dE8fG6h";

    private static readonly DeviceStamp Phone = new("1.0 (7)", "android 14", "samsung SM-A155F");

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
        (await Scalar($"SELECT COUNT(*) FROM dbo.ErrorReports WHERE InstallId = '{installId}'")).Should().Be(2);
        (await Scalar($"SELECT COUNT(*) FROM dbo.ErrorReports WHERE InstallId = '{installId}' AND Kind = 'slow' AND Count = 3 AND Route = '/start/pro/done' AND Platform = 'android 14' AND Device = N'samsung SM-A155F'"))
            .Should().Be(1);
        (await Scalar($"SELECT COUNT(*) FROM dbo.ErrorReports WHERE InstallId = '{installId}' AND (Message LIKE '%{FirebaseUid}%' OR Message LIKE '%{phone.PhoneNumber![1..]}%')"))
            .Should().Be(0, "the server scrubs what an app let through");
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
    }

    [IntegrationFact]
    public async Task UnknownKind_IsRefused()
    {
        var client = api.Client(api.Tokens.For($"kind-{Guid.NewGuid():N}"));
        var batch = new ErrorReportBatch(Guid.NewGuid().ToString(), "1.0", "android 14", null,
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
        var oldInstall = Guid.NewGuid().ToString();
        await Execute($"""
            INSERT dbo.ErrorReports (InstallId, AppVersion, Platform, Kind, Source, Message, Count, FirstAt, LastAt, ReceivedAt)
            VALUES ('{oldInstall}', '0.9', 'android 13', 'error', 's', N'old', 1, '2020-01-01', '2020-01-01',
                    DATEADD(day, -{(int)ErrorReportStore.KeepFor.TotalDays + 1}, SYSDATETIMEOFFSET()))
            """);

        await new TestPhone(api, "Cleaner").Get<IErrorReportRepository>().SendAsync(Guid.NewGuid().ToString(), Phone, [Report("new")]);

        (await Scalar($"SELECT COUNT(*) FROM dbo.ErrorReports WHERE InstallId = '{oldInstall}'")).Should().Be(0);
    }

    private async Task<int> Scalar(string sql)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private async Task Execute(string sql)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
