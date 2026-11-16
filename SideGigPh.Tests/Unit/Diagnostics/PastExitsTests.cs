using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// How previous runs ended becomes crash reports: crashes, native crashes, frozen screens and memory kills on screen
/// are reported once each, dated when they happened, with no screen from this run; normal exits are not.
/// </summary>
public class PastExitsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 3, 0, 0, TimeSpan.Zero);

    private const string AnrDump = """
        ----- pid 1234 at 2026-11-16 -----
        "Signal Catcher" daemon prio=10 tid=7 Runnable
          at nothing

        "main" prio=5 tid=1 Blocked
          at com.example.Waiting.lock(Waiting.java:10)
          at crc64.MainActivity.n_onResume(Native method)

        "RenderThread" prio=7 tid=12 Native
        """;

    private static async Task<ErrorLog> NewLogAsync()
    {
        var log = new ErrorLog(new FakeErrorLogStore(), new FakeClock(Now));
        await log.LoadAsync();
        return log;
    }

    [Fact]
    public async Task Crash_IsReportedDatedWhenItHappened_WithoutThisRunsScreen()
    {
        var log = await NewLogAsync();
        log.Visit("/seeker");
        var at = Now.AddHours(-3);

        PastExits.Report(log, new PastExit(ExitCause.NativeCrash, true, "crash", null, at));

        var report = log.Queued.Should().ContainSingle().Subject;
        report.Kind.Should().Be(ErrorKind.Crash);
        report.Source.Should().Be(PastExits.Source);
        report.Message.Should().Contain("native");
        report.Detail.Should().Be("crash");
        report.FirstAt.Should().Be(at);
        report.Route.Should().BeNull();
        report.Trail.Should().BeNull();
    }

    [Fact]
    public async Task Anr_KeepsTheMainThreadOnly()
    {
        var log = await NewLogAsync();

        PastExits.Report(log, new PastExit(ExitCause.NotResponding, true, "Input dispatching timed out", AnrDump, Now));

        var detail = log.Queued.Should().ContainSingle().Subject.Detail;
        detail.Should().StartWith("Input dispatching timed out").And.Contain("Waiting.lock").And.NotContain("Signal Catcher").And.NotContain("RenderThread");
    }

    [Theory]
    [InlineData(ExitCause.Other, true)]
    [InlineData(ExitCause.LowMemory, false)]
    public async Task NormalExits_AndBackgroundMemoryKills_AreNotReported(ExitCause cause, bool onScreen)
    {
        var log = await NewLogAsync();

        PastExits.Report(log, new PastExit(cause, onScreen, "user request", null, Now));

        log.Queued.Should().BeEmpty();
    }

    [Fact]
    public async Task MemoryKillOnScreen_IsReported()
    {
        var log = await NewLogAsync();

        PastExits.Report(log, new PastExit(ExitCause.LowMemory, true, null, null, Now));

        log.Queued.Should().ContainSingle().Which.Message.Should().Contain("memory");
    }

    [Fact]
    public void Unseen_SkipsWhatWasAlreadyReported_OldestFirst()
    {
        var seen = Now.AddDays(-1);
        var exits = new[]
        {
            new PastExit(ExitCause.Crash, true, null, null, Now),
            new PastExit(ExitCause.Crash, true, null, null, seen),
            new PastExit(ExitCause.Crash, true, null, null, Now.AddHours(-2)),
        };

        PastExits.Unseen(exits, seen).Select(e => e.At).Should().Equal(Now.AddHours(-2), Now);
    }
}
