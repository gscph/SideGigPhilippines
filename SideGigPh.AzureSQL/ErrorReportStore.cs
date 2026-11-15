using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// The problems the app had on people's phones (<c>ErrorReports</c>), for us to fix. Diagnostics only: keyed by a
/// random install id, never the account, every text scrubbed again here (<see cref="ErrorReportRules.Scrub"/>) and cut
/// to its column, and each save first deletes what is older than <see cref="KeepFor"/>.
/// </summary>
public sealed class ErrorReportStore(SideGigPhDb db)
{
    /// <summary>How long a report is kept (the privacy policy says so too).</summary>
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(90);

    /// <summary>The most one report may say the problem repeated.</summary>
    public const int MaxCount = 100_000;

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

        var now = DateTimeOffset.UtcNow;
        var device = ErrorReportRules.Clean(batch.Device, ErrorReportRules.MaxDevice);
        foreach (var report in reports)
        {
            var lastAt = report.LastAt > now ? now : report.LastAt;
            db.ErrorReports.Add(new ErrorReportRow
            {
                InstallId = installId.ToString(),
                AppVersion = appVersion,
                CodeVersion = codeVersion,
                Channel = batch.Channel,
                Platform = platform,
                Device = device,
                Kind = report.Kind,
                Source = ErrorReportRules.Clean(report.Source, ErrorReportRules.MaxSource) ?? "—",
                Message = ErrorReportRules.Clean(report.Message, ErrorReportRules.MaxMessage) ?? "—",
                Detail = ErrorReportRules.Clean(report.Detail, ErrorReportRules.MaxDetail),
                Route = ErrorReportRules.Clean(report.Route, ErrorReportRules.MaxRoute),
                Trail = ErrorReportRules.Clean(report.Trail, ErrorReportRules.MaxTrail),
                Count = Math.Clamp(report.Count, 1, MaxCount),
                FirstAt = report.FirstAt > lastAt ? lastAt : report.FirstAt,
                LastAt = lastAt,
                ReceivedAt = now,
            });
        }

        var expired = now - KeepFor;
        await db.ErrorReports.Where(r => r.ReceivedAt < expired).ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>The most a <see cref="RecentAsync"/> read returns.</summary>
    public const int MaxRead = 200;

    /// <summary>
    /// The newest reports first (by when the error last happened), for the operator and for Claude while building. Every
    /// field was scrubbed on the phone and again here when it was saved; the install id is left out.
    /// </summary>
    public async Task<IReadOnlyList<ErrorReportView>> RecentAsync(int take, string? channel, CancellationToken cancellationToken) =>
        await db.ErrorReports.AsNoTracking()
            .Where(r => channel == null || r.Channel == channel)
            .OrderByDescending(r => r.LastAt)
            .Take(Math.Clamp(take, 1, MaxRead))
            .Select(r => new ErrorReportView(
                r.Id, r.AppVersion, r.CodeVersion, r.Channel, r.Platform, r.Device, r.Kind, r.Source, r.Message,
                r.Detail, r.Route, r.Trail, r.Count, r.FirstAt, r.LastAt, r.ReceivedAt))
            .ToListAsync(cancellationToken);
}

/// <summary>One stored error report as <see cref="ErrorReportStore.RecentAsync"/> reads it (no install id).</summary>
public sealed record ErrorReportView(
    long Id, string AppVersion, string CodeVersion, string Channel, string Platform, string? Device, string Kind,
    string Source, string Message, string? Detail, string? Route, string? Trail, int Count,
    DateTimeOffset FirstAt, DateTimeOffset LastAt, DateTimeOffset ReceivedAt);
