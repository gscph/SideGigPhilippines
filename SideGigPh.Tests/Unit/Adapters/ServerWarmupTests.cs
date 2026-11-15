using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// The app wakes a cold API and a paused database when it opens and when it comes back after a while, with one small
/// call, never more often than <see cref="ServerWarmup.IdleAfter"/> however often the person flips between apps.
/// </summary>
public class ServerWarmupTests
{
    private sealed class CountingServer : IServerWarmup
    {
        public int Calls { get; private set; }

        public Task WarmUpAsync()
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void OpeningTheApp_WakesTheServer()
    {
        var server = new CountingServer();
        new ServerWarmup(server, new ManualClock()).WakeIfIdle().Should().BeTrue();
        server.Calls.Should().Be(1);
    }

    [Fact]
    public void FlippingBackAndForth_WakesOnlyOncePerIdleWindow()
    {
        var server = new CountingServer();
        var clock = new ManualClock();
        var warmup = new ServerWarmup(server, clock);

        warmup.WakeIfIdle();
        for (var i = 0; i < 20; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(10));
            warmup.WakeIfIdle().Should().BeFalse();
        }

        server.Calls.Should().Be(1);
    }

    [Fact]
    public void ComingBackAfterTheIdleWindow_WakesAgain()
    {
        var server = new CountingServer();
        var clock = new ManualClock();
        var warmup = new ServerWarmup(server, clock);

        warmup.WakeIfIdle();
        clock.Advance(ServerWarmup.IdleAfter);

        warmup.WakeIfIdle().Should().BeTrue();
        server.Calls.Should().Be(2);
    }
}
