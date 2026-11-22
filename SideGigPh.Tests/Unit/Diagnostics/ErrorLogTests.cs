using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// The error log on the phone: problems are kept scrubbed with the screen they happened on and what came before, the
/// same problem only counts up, the queue stays small, and the Me page's switch stops it and deletes what waits.
/// </summary>
public class ErrorLogTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeErrorLogStore _store = new();
    private readonly FakeClock _clock = new(Now);

    private ErrorLog NewLog() => new(_store, _clock);

    private static DeviceStamp Phone => new("1.0 (1)", "48466c9", "beta", "android 14", "samsung SM-A155F");

    [Fact]
    public async Task Record_KeepsTheScreenAndTrail_Scrubbed()
    {
        var log = NewLog();
        await log.LoadAsync();
        log.Visit("/start/pro/7");
        log.Visit("/pro/Xy7kP2mQ9rT4vW1zA3bC5dE8fG6h");

        log.Record(ErrorKind.Error, "SideGigPh.Maui.Services.Api.ApiClient", "Couldn't save +639171234567", new InvalidOperationException("boom"));

        var report = log.Queued.Should().ContainSingle().Subject;
        report.Kind.Should().Be(ErrorKind.Error);
        report.Message.Should().Be("Couldn't save [phone]");
        report.Detail.Should().Contain("InvalidOperationException: boom");
        report.Route.Should().Be("/pro/[id]");
        report.Trail.Should().Contain("/start/pro/7").And.Contain("/pro/[id]").And.NotContain("Xy7kP2mQ9rT4vW1zA3bC5dE8fG6h");
        report.Count.Should().Be(1);
    }

    [Fact]
    public async Task SameProblem_CountsUp()
    {
        var log = NewLog();
        await log.LoadAsync();

        log.Record(ErrorKind.Error, "Api", "Couldn't read config");
        _clock.Now = Now.AddMinutes(5);
        log.Record(ErrorKind.Error, "Api", "Couldn't read config");
        log.Record(ErrorKind.Error, "Api", "Couldn't read chats");

        log.Queued.Should().HaveCount(2);
        var repeated = log.Queued[0];
        repeated.Count.Should().Be(2);
        repeated.FirstAt.Should().Be(Now);
        repeated.LastAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public async Task Queue_KeepsOnlyTheNewest()
    {
        var log = NewLog();
        await log.LoadAsync();

        for (var i = 0; i < ErrorLog.MaxQueued + 5; i++)
        {
            log.Record(ErrorKind.Error, "Api", $"Problem {i}");
        }

        log.Queued.Should().HaveCount(ErrorLog.MaxQueued);
        log.Queued[0].Message.Should().Be("Problem 5");
    }

    [Fact]
    public async Task SwitchOff_DeletesWhatWaits_AndRecordsNothing()
    {
        var log = NewLog();
        await log.LoadAsync();
        log.Record(ErrorKind.Error, "Api", "Problem");

        await log.SetSendReportsAsync(false);
        log.Record(ErrorKind.Crash, "Crash", "Another");

        log.Queued.Should().BeEmpty();
        _store.Saved!.SendReports.Should().BeFalse();
        _store.Saved.Reports.Should().BeEmpty();
    }

    [Fact]
    public async Task Load_KeepsTheInstallId_DropsOldReports_AndKeepsOnesRecordedMeanwhile()
    {
        _store.Saved = new DiagnosticsState
        {
            InstallId = "11111111-1111-1111-1111-111111111111",
            Reports =
            [
                new(ErrorKind.Error, "Api", "Old", null, null, null, 1, Now.AddDays(-30), Now.AddDays(-20)),
                new(ErrorKind.Error, "Api", "Recent", null, null, null, 3, Now.AddDays(-2), Now.AddDays(-1)),
            ],
        };
        var log = NewLog();
        log.Record(ErrorKind.Error, "Api", "Recent");
        log.Record(ErrorKind.Crash, "Crash", "Before load");

        await log.LoadAsync();

        log.InstallId.Should().Be("11111111-1111-1111-1111-111111111111");
        log.Queued.Select(r => (r.Message, r.Count)).Should().Equal(("Recent", 4), ("Before load", 1));
    }

    [Fact]
    public async Task NewInstall_GetsARandomId_Saved()
    {
        var log = NewLog();

        await log.LoadAsync();

        Guid.TryParse(log.InstallId, out _).Should().BeTrue();
        _store.Saved!.InstallId.Should().Be(log.InstallId);
    }

    [Fact]
    public async Task RemoveSent_KeepsWhatCameInWhileSending()
    {
        var log = NewLog();
        await log.LoadAsync();
        log.Record(ErrorKind.Error, "Api", "Sent");
        log.Record(ErrorKind.Error, "Api", "Counted up while sending");
        var sent = log.Queued;
        log.Record(ErrorKind.Error, "Api", "Counted up while sending");

        await log.RemoveSentAsync(sent);

        var left = log.Queued.Should().ContainSingle().Subject;
        left.Message.Should().Be("Counted up while sending");
        left.Count.Should().Be(1);
    }

    [Fact]
    public async Task Recorded_IsRaisedForAProblem_NotForTrailLines_NorWithTheSwitchOff()
    {
        var log = NewLog();
        await log.LoadAsync();
        var raised = 0;
        log.Recorded += () => raised++;

        log.Visit("/seeker/posts/new");
        log.Step("Tapped Post");
        raised.Should().Be(0, "screens and steps are only the trail");

        log.Record(ErrorKind.Error, "SideGigPh.Maui.Services.Api.ApiClient", "API: no answer to POST job-posts");
        raised.Should().Be(1, "a new problem is sent soon, not at the next periodic try");

        await log.SetSendReportsAsync(false);
        log.Record(ErrorKind.Error, "SideGigPh.Maui.Services.Api.ApiClient", "API: no answer to POST job-posts");
        raised.Should().Be(1, "with the switch off nothing is kept, so there is nothing to send");
    }

    [Fact]
    public async Task Logger_AnErrorAlreadyReported_IsATrailLine_NotASecondReport()
    {
        var log = NewLog();
        await log.LoadAsync();
        using var provider = new ErrorLogLoggerProvider(log);
        var api = provider.CreateLogger("SideGigPh.Maui.Services.Api.ApiClient");
        var page = provider.CreateLogger("SideGigPh.Maui.Components.Inbox.ThreadPage");
        var failure = new HttpRequestException("The network connection was lost.");

        api.LogError(failure, "API: no answer to POST chats/x/messages");
        ErrorLog.MarkReported(failure);
        page.LogError(failure, "Sending a chat message failed.");
        page.LogError(new InvalidOperationException("bug"), "Sending a chat message failed.");

        log.Queued.Select(report => report.Message).Should().BeEquivalentTo(
            ["API: no answer to POST chats/x/messages", "Sending a chat message failed."], "the bug is still its own report");
        log.Queued.Last().Trail.Should().Contain("Error: Sending a chat message failed.");
    }

    [Fact]
    public async Task Logger_RecordsErrors_AndKeepsTheAppsOwnLinesAsTrail()
    {
        var log = NewLog();
        await log.LoadAsync();
        using var provider = new ErrorLogLoggerProvider(log);
        var app = provider.CreateLogger("SideGigPh.Maui.Services.ProWorkspace");
        var framework = provider.CreateLogger("Microsoft.AspNetCore.Components.RenderTree");

        framework.LogInformation("Rendering");
        app.LogWarning("Saving the profile failed");
        framework.LogError(new InvalidOperationException("render failed"), "Unhandled exception rendering component");

        var report = log.Queued.Should().ContainSingle().Subject;
        report.Source.Should().Be("Microsoft.AspNetCore.Components.RenderTree");
        report.Detail.Should().Contain("render failed");
        report.Trail.Should().Contain("Saving the profile failed").And.NotContain("Rendering");
    }

    [Fact]
    public async Task Sender_WaitsForSignIn_ThenSendsInBatches()
    {
        var log = NewLog();
        await log.LoadAsync();
        for (var i = 0; i < 25; i++)
        {
            log.Record(ErrorKind.Error, "Api", $"Problem {i}");
        }

        var identity = new FakeIdentity(null);
        var api = new FakeErrorReportRepository();
        var sender = new ErrorReportSender(log, api, identity, Phone, NullLogger<ErrorReportSender>.Instance);

        (await sender.SendAsync()).Should().BeFalse("nobody is signed in yet");
        api.Batches.Should().BeEmpty();

        identity.User = new AuthUser { Uid = "u1" };
        (await sender.SendAsync()).Should().BeTrue();

        api.Batches.Select(b => b.Count).Should().Equal(20, 5);
        log.Queued.Should().BeEmpty();
    }

    [Fact]
    public async Task Sender_Offline_KeepsTheReports_RefusedOnesAreDropped()
    {
        var log = NewLog();
        await log.LoadAsync();
        log.Record(ErrorKind.Error, "Api", "Problem");
        var api = new FakeErrorReportRepository { Fail = true };
        var sender = new ErrorReportSender(log, api, new FakeIdentity(new AuthUser { Uid = "u1" }), Phone, NullLogger<ErrorReportSender>.Instance);

        (await sender.SendAsync()).Should().BeFalse();
        log.Queued.Should().HaveCount(1);

        api.Fail = false;
        api.Answer = false;
        (await sender.SendAsync()).Should().BeTrue();
        log.Queued.Should().BeEmpty("a batch the server calls bad would block every report after it");
    }

    [Fact]
    public async Task Sender_SwitchOff_SendsNothing()
    {
        var log = NewLog();
        await log.LoadAsync();
        log.Record(ErrorKind.Error, "Api", "Problem");
        await log.SetSendReportsAsync(false);
        var api = new FakeErrorReportRepository();
        var sender = new ErrorReportSender(log, api, new FakeIdentity(new AuthUser { Uid = "u1" }), Phone, NullLogger<ErrorReportSender>.Instance);

        await sender.SendAsync();

        api.Batches.Should().BeEmpty();
    }
}
