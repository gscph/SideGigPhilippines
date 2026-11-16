using Azure.Data.Tables;
using SideGigPh.Contracts;

namespace SideGigPh.Api.Telemetry;

/// <summary>
/// The problems the app had on people's phones (Table Storage <c>ErrorReports</c>), for us to fix. Diagnostics only:
/// keyed by a random install id, never the account, every text scrubbed again here (<see cref="ErrorReportRules.Scrub"/>)
/// and cut to its limit, and at most once a day per server instance the rows older than <see cref="KeepFor"/> are
/// deleted. One partition per channel, rows newest first (by when the error last happened). Here and not in SQL so a
/// phone's reports never wake the database.
/// </summary>
public sealed class ErrorReportTable(TelemetryTables tables, TimeProvider clock)
{
    /// <summary>How long a report is kept (the privacy policy says so too).</summary>
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(90);

    /// <summary>The most one report may say the problem repeated.</summary>
    public const int MaxCount = 100_000;

    /// <summary>The most a <see cref="RecentAsync"/> read returns.</summary>
    public const int MaxRead = 200;

    /// <summary>How often an instance looks for expired reports.</summary>
    public static readonly TimeSpan CleanEvery = TimeSpan.FromDays(1);

    // Rows deleted per clean-up, so one save never turns into a long job.
    private const int CleanBatch = 500;

    private static long _lastCleanTicks;

    // The partitions: one per channel.
    private static readonly string[] Channels = [AppChannels.Dev, AppChannels.Beta, AppChannels.Prod];

    private ITableRows Rows => tables.ErrorReports;

    /// <summary>Keeps the batch's reports. False for a batch with no install id or version, an unknown channel, too many reports, or a bad one.</summary>
    public async Task<bool> SaveAsync(ErrorReportBatch batch, CancellationToken cancellationToken)
    {
        var appVersion = ErrorReportRules.Clean(batch.AppVersion, ErrorReportRules.MaxVersion);
        var platform = ErrorReportRules.Clean(batch.Platform, ErrorReportRules.MaxVersion);
        var codeVersion = ErrorReportRules.Clean(batch.CodeVersion, ErrorReportRules.MaxCodeVersion) ?? "unknown";
        if (!Guid.TryParse(batch.InstallId, out var installId) || appVersion is null || platform is null || !AppChannels.IsKnown(batch.Channel)
            || batch.Reports is not { Count: > 0 and <= ErrorReportRules.MaxReports } reports
            || reports.Any(r => !ErrorKinds.IsKnown(r?.Kind)))
        {
            return false;
        }

        var now = clock.GetUtcNow();
        var device = ErrorReportRules.Clean(batch.Device, ErrorReportRules.MaxDevice);
        foreach (var report in reports)
        {
            var lastAt = report.LastAt > now ? now : report.LastAt;
            await Rows.AddAsync(new TableEntity(batch.Channel, NewestFirst.Key(lastAt))
            {
                ["InstallId"] = installId.ToString(),
                ["AppVersion"] = appVersion,
                ["CodeVersion"] = codeVersion,
                ["Platform"] = platform,
                ["Device"] = device,
                ["Kind"] = report.Kind,
                ["Source"] = ErrorReportRules.Clean(report.Source, ErrorReportRules.MaxSource) ?? "—",
                ["Message"] = ErrorReportRules.Clean(report.Message, ErrorReportRules.MaxMessage) ?? "—",
                ["Detail"] = ErrorReportRules.Clean(report.Detail, ErrorReportRules.MaxDetail),
                ["Route"] = ErrorReportRules.Clean(report.Route, ErrorReportRules.MaxRoute),
                ["Trail"] = ErrorReportRules.Clean(report.Trail, ErrorReportRules.MaxTrail),
                ["Count"] = Math.Clamp(report.Count, 1, MaxCount),
                ["FirstAt"] = report.FirstAt > lastAt ? lastAt : report.FirstAt,
                ["LastAt"] = lastAt,
                ["ReceivedAt"] = now,
            }, cancellationToken);
        }

        await CleanIfDueAsync(now, cancellationToken);
        return true;
    }

    /// <summary>A problem the server found itself (e.g. missing schema scripts), so <c>GET error-reports</c> shows it.</summary>
    public async Task<bool> ReportServerAsync(string source, string message, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        try
        {
            await Rows.AddAsync(new TableEntity(AppChannels.Prod, NewestFirst.Key(now))
            {
                ["InstallId"] = Guid.Empty.ToString(),
                ["AppVersion"] = "server",
                ["CodeVersion"] = ApiVersion.Current.Length > ErrorReportRules.MaxCodeVersion ? ApiVersion.Current[..ErrorReportRules.MaxCodeVersion] : ApiVersion.Current,
                ["Platform"] = "server",
                ["Kind"] = ErrorKinds.Error,
                ["Source"] = source,
                ["Message"] = ErrorReportRules.Clean(message, ErrorReportRules.MaxMessage) ?? "—",
                ["Count"] = 1,
                ["FirstAt"] = now,
                ["LastAt"] = now,
                ["ReceivedAt"] = now,
            }, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false; // the storage account is unreachable; the function's own log still says it
        }
    }

    /// <summary>
    /// The newest reports first (by when the error last happened), for the operator and for Claude while building. Every
    /// field was scrubbed on the phone and again here when it was saved; the install id is left out.
    /// </summary>
    public async Task<IReadOnlyList<ErrorReportView>> RecentAsync(int take, string? channel, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, MaxRead);
        string[] channels = channel is null ? Channels : AppChannels.IsKnown(channel) ? [channel] : [];
        var views = new List<ErrorReportView>();
        foreach (var partition in channels)
        {
            views.AddRange((await Rows.ReadAsync(partition, null, take, cancellationToken)).Select(View));
        }

        return [.. views.OrderByDescending(v => v.LastAt).Take(take)];
    }

    // Deletes rows that last happened before KeepFor, a batch per channel, at most once per CleanEvery per instance.
    private async Task CleanIfDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var last = Interlocked.Read(ref _lastCleanTicks);
        if (now.UtcTicks - last < CleanEvery.Ticks || Interlocked.CompareExchange(ref _lastCleanTicks, now.UtcTicks, last) != last)
        {
            return;
        }

        var oldest = NewestFirst.Ticks(now - KeepFor);
        foreach (var partition in Channels)
        {
            foreach (var row in await Rows.ReadAsync(partition, oldest, CleanBatch, cancellationToken))
            {
                await Rows.DeleteAsync(row.PartitionKey, row.RowKey, cancellationToken);
            }
        }
    }

    private static ErrorReportView View(TableEntity row) => new(
        row.RowKey,
        row.GetString("AppVersion") ?? "",
        row.GetString("CodeVersion") ?? "",
        row.PartitionKey,
        row.GetString("Platform") ?? "",
        row.GetString("Device"),
        row.GetString("Kind") ?? "",
        row.GetString("Source") ?? "",
        row.GetString("Message") ?? "",
        row.GetString("Detail"),
        row.GetString("Route"),
        row.GetString("Trail"),
        row.GetInt32("Count") ?? 1,
        row.GetDateTimeOffset("FirstAt") ?? default,
        row.GetDateTimeOffset("LastAt") ?? default,
        row.GetDateTimeOffset("ReceivedAt") ?? default);
}

/// <summary>One stored error report as <see cref="ErrorReportTable.RecentAsync"/> reads it (no install id).</summary>
public sealed record ErrorReportView(
    string Id, string AppVersion, string CodeVersion, string Channel, string Platform, string? Device, string Kind,
    string Source, string Message, string? Detail, string? Route, string? Trail, int Count,
    DateTimeOffset FirstAt, DateTimeOffset LastAt, DateTimeOffset ReceivedAt);
