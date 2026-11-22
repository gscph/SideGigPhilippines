using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// Watches a flow that leaves the app and should come back (the camera, the photo picker), so a problem nobody sees
/// still reaches the error reports: one still open after its time is a <see cref="ErrorKind.Slow"/> report ("still
/// open after 180 s"), and one the app never came back from (closed by the phone or the person while it was open) is
/// reported at the next launch (<see cref="ErrorLog.FlowStarted"/>). How long it took goes into the trail.
/// </summary>
public sealed class FlowWatch(ErrorLog log, TimeProvider clock)
{
    /// <summary>Starts watching <paramref name="name"/>; dispose the answer when the flow came back.</summary>
    public IDisposable Start(string name, TimeSpan stuckAfter)
    {
        log.FlowStarted(name);
        return new Watch(log, clock, name, stuckAfter);
    }

    private sealed class Watch : IDisposable
    {
        private readonly ErrorLog _log;
        private readonly TimeProvider _clock;
        private readonly string _name;
        private readonly DateTimeOffset _startedAt;
        private readonly ITimer _timer;
        private int _ended;

        public Watch(ErrorLog log, TimeProvider clock, string name, TimeSpan stuckAfter)
        {
            _log = log;
            _clock = clock;
            _name = name;
            _startedAt = clock.GetUtcNow();
            _timer = clock.CreateTimer(_ => Stuck(stuckAfter), null, stuckAfter, Timeout.InfiniteTimeSpan);
        }

        private void Stuck(TimeSpan after)
        {
            if (Volatile.Read(ref _ended) == 0)
            {
                _log.Record(ErrorKind.Slow, ErrorLog.FlowSource(_name),
                    $"The {_name.ToLowerInvariant()} was still open after {(int)after.TotalSeconds} s.");
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 1)
            {
                return;
            }

            _timer.Dispose();
            _log.FlowEnded(_name);
            _log.Step($"{_name} closed after {(int)(_clock.GetUtcNow() - _startedAt).TotalSeconds} s");
        }
    }
}
