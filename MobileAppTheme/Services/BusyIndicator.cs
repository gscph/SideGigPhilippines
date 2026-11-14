namespace MobileAppTheme.Services;

/// <summary>
/// Says when the app is waiting on something the user started (a save, an upload, a sign-in), so the
/// <c>BusyOverlay</c> in <c>MainLayout</c> covers the screen with the brand splash and a changing line instead of
/// looking frozen. Pages wrap the slow call: <c>await Busy.RunAsync(() =&gt; DoItAsync())</c>.
/// Calls can overlap; the overlay stays up until the last one ends. When it stays up longer than
/// <see cref="StuckAfter"/> it raises <see cref="Stuck"/> (once per wait, even if the wait never ends), and
/// <see cref="Unstuck"/> with the whole wait when it finally does, so the app can report screens that look frozen.
/// </summary>
public sealed class BusyIndicator
{
    /// <summary>How long the overlay may stay up before it counts as stuck.</summary>
    public static readonly TimeSpan StuckAfter = TimeSpan.FromSeconds(20);

    private readonly TimeProvider _clock;
    private readonly object _gate = new();
    private int _running;
    private ITimer? _stuckTimer;
    private DateTimeOffset _since;
    private bool _stuck;

    public BusyIndicator(TimeProvider clock) => _clock = clock;

    public bool IsBusy => Volatile.Read(ref _running) > 0;

    /// <summary>Raised when the app becomes busy or idle (not for every nested call).</summary>
    public event Action? Changed;

    /// <summary>Raised (off the UI thread) when the overlay has been up for <see cref="StuckAfter"/>.</summary>
    public event Action<TimeSpan>? Stuck;

    /// <summary>Raised when a wait that was <see cref="Stuck"/> ends, with how long it took in all.</summary>
    public event Action<TimeSpan>? Unstuck;

    public async Task<T> RunAsync<T>(Func<Task<T>> work)
    {
        Enter();
        try
        {
            return await work();
        }
        finally
        {
            Leave();
        }
    }

    public async Task RunAsync(Func<Task> work)
    {
        Enter();
        try
        {
            await work();
        }
        finally
        {
            Leave();
        }
    }

    private void Enter()
    {
        if (Interlocked.Increment(ref _running) != 1)
        {
            return;
        }

        lock (_gate)
        {
            _since = _clock.GetUtcNow();
            _stuck = false;
            _stuckTimer?.Dispose();
            _stuckTimer = _clock.CreateTimer(_ => OnStuck(), null, StuckAfter, Timeout.InfiniteTimeSpan);
        }

        Changed?.Invoke();
    }

    private void Leave()
    {
        if (Interlocked.Decrement(ref _running) != 0)
        {
            return;
        }

        bool wasStuck;
        TimeSpan waited;
        lock (_gate)
        {
            _stuckTimer?.Dispose();
            _stuckTimer = null;
            wasStuck = _stuck;
            waited = _clock.GetUtcNow() - _since;
        }

        if (wasStuck)
        {
            Unstuck?.Invoke(waited);
        }

        Changed?.Invoke();
    }

    private void OnStuck()
    {
        TimeSpan waited;
        lock (_gate)
        {
            if (!IsBusy || _stuck)
            {
                return;
            }

            _stuck = true;
            waited = _clock.GetUtcNow() - _since;
        }

        Stuck?.Invoke(waited);
    }
}
