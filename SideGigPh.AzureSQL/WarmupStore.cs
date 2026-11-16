using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SideGigPh.AzureSQL;

/// <summary>
/// Wakes the database (a serverless Azure SQL database pauses when idle and takes seconds to resume) with the
/// cheapest query there is, so the person's first real read after opening the app isn't the one that waits. The first
/// wake on each server instance also checks that every <see cref="SchemaScripts.Required"/> script was run: an API
/// deployed before its script fails every call that reads the missing table (search and chat did, for a morning).
/// </summary>
public sealed class WarmupStore(SideGigPhDb db)
{
    /// <summary>How long after a wake this server instance skips the next one: the database is surely awake.</summary>
    public static readonly TimeSpan AwakeFor = TimeSpan.FromMinutes(1);

    private static long _lastWakeTicks;
    private static volatile bool _schemaOk;

    /// <summary>
    /// Runs <c>SELECT 1</c>, unless this instance already did within <see cref="AwakeFor"/>. Answers the required
    /// schema scripts the database is missing (empty when all are there or it already checked); the function reports them.
    /// </summary>
    public async Task<IReadOnlyList<string>> WakeAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.UtcTicks;
        var last = Interlocked.Read(ref _lastWakeTicks);
        if (now - last < AwakeFor.Ticks || Interlocked.CompareExchange(ref _lastWakeTicks, now, last) != last)
        {
            return [];
        }

        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        if (_schemaOk)
        {
            return [];
        }

        var missing = await MissingScriptsAsync(cancellationToken);
        _schemaOk = missing.Count == 0;
        return missing;
    }

    /// <summary>The <see cref="SchemaScripts.Required"/> scripts not recorded in <c>dbo.SchemaChanges</c>.</summary>
    public async Task<IReadOnlyList<string>> MissingScriptsAsync(CancellationToken cancellationToken)
    {
        List<string> applied;
        try
        {
            applied = await db.Database.SqlQueryRaw<string>("SELECT Script AS Value FROM dbo.SchemaChanges").ToListAsync(cancellationToken);
        }
        catch (SqlException)
        {
            applied = []; // no SchemaChanges table at all: nothing after 001 was run
        }

        return [.. SchemaScripts.Required.Except(applied, StringComparer.OrdinalIgnoreCase)];
    }
}
