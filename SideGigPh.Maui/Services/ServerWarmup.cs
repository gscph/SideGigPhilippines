namespace SideGigPh.Maui.Services;

/// <summary>
/// Wakes the server when the app opens and when it comes back on screen after <see cref="IdleAfter"/> or more, so a
/// cold API and a paused database warm up while the person is still looking at the screen, not on their first search.
/// One small call (<see cref="IServerWarmup"/>); never more than once per <see cref="IdleAfter"/>.
/// </summary>
public sealed class ServerWarmup(IServerWarmup server, TimeProvider clock)
{
    /// <summary>How long without a wake before the next open or return to the app wakes the server again.</summary>
    public static readonly TimeSpan IdleAfter = TimeSpan.FromMinutes(5);

    private DateTimeOffset? _lastWake;

    /// <summary>Starts a wake in the background unless one went out within <see cref="IdleAfter"/>. True when it did.</summary>
    public bool WakeIfIdle()
    {
        var now = clock.GetUtcNow();
        lock (this)
        {
            if (_lastWake is { } last && now - last < IdleAfter)
            {
                return false;
            }

            _lastWake = now;
        }

        _ = server.WarmUpAsync();
        return true;
    }
}
