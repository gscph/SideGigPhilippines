using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Keeps an open chat's picked-job card current. Marking a job done sends no chat message, so the chat poll never sees
/// it: while the card waits on the other side, this re-reads the picked jobs every <see cref="Interval"/> and reports
/// when they changed, until the newest one is completed or the page disposes it. Skipped while the app is hidden
/// (<see cref="AppVisibility"/>); a failed read (offline) comes back empty and is ignored until the next tick.
/// </summary>
public sealed class PickedJobWatch : IDisposable
{
    /// <summary>How often a waiting card checks whether the other side marked the job done.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly CancellationTokenSource _stop = new();

    private PickedJobWatch(Func<Task<IReadOnlyList<PickedJob>>> read, Func<bool> visible, IReadOnlyList<PickedJob> current,
        Func<IReadOnlyList<PickedJob>, Task> changed, TimeProvider clock) =>
        _ = RunAsync(read, visible, current, changed, clock, _stop.Token);

    /// <summary>Watches the picked jobs between the caller and <paramref name="otherUid"/>, starting from <paramref name="current"/>.</summary>
    public static PickedJobWatch Start(JobBoard board, AppVisibility visibility, string otherUid, IReadOnlyList<PickedJob> current,
        Func<IReadOnlyList<PickedJob>, Task> changed, TimeProvider? clock = null) =>
        new(() => board.PickedJobsAsync(otherUid), () => visibility.IsVisible, current, changed, clock ?? TimeProvider.System);

    /// <summary>Whether the newest job still waits on someone (nothing to watch once it is completed or there is none).</summary>
    public static bool IsWaiting(IReadOnlyList<PickedJob> jobs) => jobs.FirstOrDefault() is { IsCompleted: false };

    public void Dispose() => _stop.Cancel();

    private static async Task RunAsync(Func<Task<IReadOnlyList<PickedJob>>> read, Func<bool> visible, IReadOnlyList<PickedJob> current,
        Func<IReadOnlyList<PickedJob>, Task> changed, TimeProvider clock, CancellationToken stop)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval, clock);
            while (IsWaiting(current) && await timer.WaitForNextTickAsync(stop))
            {
                if (!visible())
                {
                    continue;
                }

                var jobs = await read();
                if (jobs.Count > 0 && !jobs.SequenceEqual(current) && !stop.IsCancellationRequested)
                {
                    current = jobs;
                    await changed(jobs);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed: the chat page closed.
        }
    }
}
