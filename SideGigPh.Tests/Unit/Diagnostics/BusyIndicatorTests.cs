using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// A loading screen that stays up looks frozen to the person (the Galaxy A15 "hang after Done"). The skeleton's
/// <see cref="BusyIndicator"/> says so once it passes <see cref="BusyIndicator.StuckAfter"/>, even if the wait never
/// ends, and again with the whole wait when it does.
/// </summary>
public class BusyIndicatorTests
{
    [Fact]
    public async Task LongWait_IsStuck_OnceAndThenUnstuck()
    {
        var clock = new ManualClock();
        var busy = new BusyIndicator(clock);
        var stuck = new List<TimeSpan>();
        var unstuck = new List<TimeSpan>();
        busy.Stuck += stuck.Add;
        busy.Unstuck += unstuck.Add;
        var work = new TaskCompletionSource();

        var running = busy.RunAsync(() => work.Task);
        clock.Advance(BusyIndicator.StuckAfter);
        clock.Advance(TimeSpan.FromSeconds(25));
        work.SetResult();
        await running;

        stuck.Should().Equal(BusyIndicator.StuckAfter);
        unstuck.Should().Equal(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task QuickWait_IsNeverStuck()
    {
        var clock = new ManualClock();
        var busy = new BusyIndicator(clock);
        var raised = 0;
        busy.Stuck += _ => raised++;
        busy.Unstuck += _ => raised++;

        await busy.RunAsync(() => Task.CompletedTask);
        clock.Advance(BusyIndicator.StuckAfter * 2);

        raised.Should().Be(0);
    }
}
