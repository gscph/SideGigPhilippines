using Microsoft.EntityFrameworkCore;

namespace SideGigPh.AzureSQL;

/// <summary>
/// Wakes the database (a serverless Azure SQL database pauses when idle and takes seconds to resume) with the
/// cheapest query there is, so the person's first real read after opening the app isn't the one that waits.
/// </summary>
public sealed class WarmupStore(SideGigPhDb db)
{
    /// <summary>How long after a wake this server instance skips the next one: the database is surely awake.</summary>
    public static readonly TimeSpan AwakeFor = TimeSpan.FromMinutes(1);

    private static long _lastWakeTicks;

    /// <summary>Runs <c>SELECT 1</c>, unless this instance already did within <see cref="AwakeFor"/>.</summary>
    public async Task WakeAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.UtcTicks;
        var last = Interlocked.Read(ref _lastWakeTicks);
        if (now - last < AwakeFor.Ticks || Interlocked.CompareExchange(ref _lastWakeTicks, now, last) != last)
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
    }
}
