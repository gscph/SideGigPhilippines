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

    /// <summary>Keeps the batch's reports. False for a batch with no install id, too many reports, or a bad one.</summary>
    public async Task<bool> SaveAsync(ErrorReportBatch batch, CancellationToken cancellationToken)
    {
        var appVersion = ErrorReportRules.Clean(batch.AppVersion, ErrorReportRules.MaxVersion);
        var platform = ErrorReportRules.Clean(batch.Platform, ErrorReportRules.MaxVersion);
        if (!Guid.TryParse(batch.InstallId, out var installId) || appVersion is null || platform is null
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
}
