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
    private static volatile bool _schemaReported;

    /// <summary>
    /// Runs <c>SELECT 1</c>, unless this instance already did within <see cref="AwakeFor"/>. Answers the required
    /// schema scripts the database is missing (empty when all are there or it already checked); the first time it finds
    /// some, it also saves an error report about it, so <c>GET error-reports</c> shows it.
    /// </summary>
    public async Task<IReadOnlyList<string>> WakeAsync(string serverVersion, CancellationToken cancellationToken)
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
        if (missing.Count == 0)
        {
            _schemaOk = true;
        }
        else if (!_schemaReported)
        {
            _schemaReported = await ReportAsync(missing, serverVersion, cancellationToken);
        }

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

    private async Task<bool> ReportAsync(IReadOnlyList<string> missing, string serverVersion, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        db.ErrorReports.Add(new ErrorReportRow
        {
            InstallId = Guid.Empty.ToString(),
            AppVersion = "server",
            CodeVersion = serverVersion.Length > 40 ? serverVersion[..40] : serverVersion,
            Channel = "prod",
            Platform = "server",
            Kind = "error",
            Source = "SideGigPh.AzureSQL.WarmupStore",
            Message = $"The database is missing schema scripts: {string.Join(", ", missing)}. Run them (Sql/), or every call reading their tables fails.",
            Count = 1,
            FirstAt = now,
            LastAt = now,
            ReceivedAt = now,
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false; // 010 (ErrorReports) itself is missing; the function's log still says it
        }
    }
}
