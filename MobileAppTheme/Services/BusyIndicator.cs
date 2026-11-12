namespace MobileAppTheme.Services;

/// <summary>
/// Says when the app is waiting on something the user started (a save, an upload, a sign-in), so the
/// <c>BusyOverlay</c> in <c>MainLayout</c> covers the screen with a spinner and a rotating word instead of
/// looking frozen. Pages wrap the slow call: <c>await Busy.RunAsync(() =&gt; DoItAsync())</c>.
/// Calls can overlap; the overlay stays up until the last one ends.
/// </summary>
public sealed class BusyIndicator
{
    private int _running;

    public bool IsBusy => Volatile.Read(ref _running) > 0;

    /// <summary>Raised when the app becomes busy or idle (not for every nested call).</summary>
    public event Action? Changed;

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
        if (Interlocked.Increment(ref _running) == 1)
        {
            Changed?.Invoke();
        }
    }

    private void Leave()
    {
        if (Interlocked.Decrement(ref _running) == 0)
        {
            Changed?.Invoke();
        }
    }
}
