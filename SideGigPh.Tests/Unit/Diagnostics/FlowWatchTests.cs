using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// Problems nobody sees still reach the error reports: a camera or photo picker that never comes back, an app closed
/// while one was open, and a camera that keeps coming back with no photo.
/// </summary>
public class FlowWatchTests
{
    private readonly FakeErrorLogStore _store = new();
    private readonly ManualClock _clock = new();

    private ErrorLog NewLog() => new(_store, _clock);

    private static WatchedPhotoPicker Watched(IPhotoPicker inner, ErrorLog log, TimeProvider clock) =>
        new(inner, new FlowWatch(log, clock), new LoggerFactory([new ErrorLogLoggerProvider(log)]).CreateLogger<WatchedPhotoPicker>());

    [Fact]
    public async Task AFlowTheAppNeverCameBackFrom_IsReportedAtTheNextLaunch()
    {
        var log = NewLog();
        await log.LoadAsync();
        log.Visit("/start/pro/photo");
        new FlowWatch(log, _clock).Start(WatchedPhotoPicker.CameraFlow, WatchedPhotoPicker.StuckAfter);
        await log.SaveAsync(); // the phone kills the app while the camera is open

        var next = NewLog();
        await next.LoadAsync();

        var report = next.Queued.Should().ContainSingle().Subject;
        report.Kind.Should().Be(ErrorKind.Error);
        report.Source.Should().Be("Flow.Camera");
        report.Message.Should().Be("The app closed while the camera was open.");
        report.Detail.Should().Contain("/start/pro/photo");
        _store.Saved!.OpenFlows.Should().BeEmpty("it is reported once");
    }

    [Fact]
    public async Task AFlowThatCameBack_IsNotReported()
    {
        var log = NewLog();
        await log.LoadAsync();
        using (new FlowWatch(log, _clock).Start(WatchedPhotoPicker.PickerFlow, WatchedPhotoPicker.StuckAfter))
        {
        }

        await log.SaveAsync();

        var next = NewLog();
        await next.LoadAsync();
        next.Queued.Should().BeEmpty();
    }

    [Fact]
    public async Task AFlowStillOpenAfterItsTime_IsASlowReport_Once()
    {
        var log = NewLog();
        await log.LoadAsync();
        using var watch = new FlowWatch(log, _clock).Start(WatchedPhotoPicker.CameraFlow, TimeSpan.FromMinutes(3));

        _clock.Advance(TimeSpan.FromMinutes(2));
        log.Queued.Should().BeEmpty();
        _clock.Advance(TimeSpan.FromMinutes(2));

        var report = log.Queued.Should().ContainSingle().Subject;
        report.Kind.Should().Be(ErrorKind.Slow);
        report.Message.Should().Be("The camera was still open after 180 s.");
    }

    [Fact]
    public async Task TheCameraComingBackEmptyTwiceInARow_IsReported()
    {
        var log = NewLog();
        await log.LoadAsync();
        var camera = new FakePhotoPicker { Next = PhotoPick.Cancelled };
        var picker = Watched(camera, log, _clock);

        await picker.TakeSelfieAsync();
        log.Queued.Should().BeEmpty("one cancel is the person changing their mind");
        await picker.TakeSelfieAsync();

        var report = log.Queued.Should().ContainSingle().Subject;
        report.Message.Should().Contain("no photo 2 times in a row");
        report.Trail.Should().Contain("Camera: Cancelled");
    }

    [Fact]
    public async Task APhotoInBetween_StartsTheCountAgain()
    {
        var log = NewLog();
        await log.LoadAsync();
        var camera = new FakePhotoPicker { Next = PhotoPick.Cancelled };
        var picker = Watched(camera, log, _clock);

        await picker.TakeSelfieAsync();
        camera.Picks(1);
        await picker.TakeSelfieAsync();
        camera.Next = PhotoPick.Cancelled;
        await picker.TakeSelfieAsync();

        log.Queued.Should().BeEmpty();
    }
}
