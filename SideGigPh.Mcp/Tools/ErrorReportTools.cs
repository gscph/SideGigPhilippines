using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using SideGigPh.AzureSQL;

namespace SideGigPh.Mcp.Tools;

/// <summary>
/// The problems the app had on people's phones (<c>dbo.ErrorReports</c>: errors, crashes, screens stuck loading),
/// already scrubbed by the app and the server, kept 90 days. No answer carries the install id, only how many phones.
/// </summary>
[McpServerToolType]
public sealed class ErrorReportTools(SideGigPhDb db)
{
    private const int KeptDays = 90;
    private const string ChannelHelp = "Only this channel: prod (store builds), beta or dev; leave empty for all.";
    private const string KindHelp = "Only this kind: error, crash or slow (a screen stuck loading); leave empty for all.";

    [McpServerTool(Name = "error_summary", ReadOnly = true, Idempotent = true)]
    [Description("The app's problems grouped by where they happened (source) and message, on the most phones first: reports, " +
                 "total times (a phone counts repeats), distinct phones, first and last seen (UTC), and the newest report's id " +
                 "to open with error_report.")]
    public async Task<IReadOnlyList<ErrorGroup>> Summary(
        [Description("How many days back, 1-90.")] int days = 7,
        [Description(ChannelHelp)] string? channel = null,
        [Description(KindHelp)] string? kind = null,
        [Description("How many rows, 1-200.")] int top = 30,
        CancellationToken cancellationToken = default) =>
        await Reports(days, channel, kind)
            .GroupBy(r => new { r.Kind, r.Source, r.Message })
            .OrderByDescending(g => g.Select(r => r.InstallId).Distinct().Count()).ThenByDescending(g => g.Sum(r => (long)r.Count))
            .Take(ToolInput.Top(top))
            .Select(g => new ErrorGroup(g.Key.Kind, g.Key.Source, g.Key.Message, g.Count(), g.Sum(r => (long)r.Count),
                g.Select(r => r.InstallId).Distinct().Count(), g.Min(r => r.FirstAt), g.Max(r => r.LastAt), g.Max(r => r.Id)))
            .ToListAsync(cancellationToken);

    [McpServerTool(Name = "recent_errors", ReadOnly = true, Idempotent = true)]
    [Description("The latest error reports, newest first, without the stack trace (open one with error_report). " +
                 "'search' matches inside the message, source or screen route.")]
    public async Task<IReadOnlyList<ErrorLine>> Recent(
        [Description("How many days back, 1-90.")] int days = 1,
        [Description(ChannelHelp)] string? channel = null,
        [Description(KindHelp)] string? kind = null,
        [Description("Words to find in the message, source or route; leave empty for all.")] string? search = null,
        [Description("How many rows, 1-200.")] int top = 50,
        CancellationToken cancellationToken = default)
    {
        var reports = Reports(days, channel, kind);
        if (ToolInput.Filter(search) is { } words)
        {
            reports = reports.Where(r => r.Message.Contains(words) || r.Source.Contains(words) || (r.Route != null && r.Route.Contains(words)));
        }

        return await reports
            .OrderByDescending(r => r.ReceivedAt).ThenByDescending(r => r.Id)
            .Take(ToolInput.Top(top))
            .Select(r => new ErrorLine(r.Id, r.ReceivedAt, r.Kind, r.Channel, r.AppVersion, r.CodeVersion, r.Platform, r.Device,
                r.Source, r.Message, r.Route, r.Count))
            .ToListAsync(cancellationToken);
    }

    [McpServerTool(Name = "error_report", ReadOnly = true, Idempotent = true)]
    [Description("One error report in full: the exception and stack trace (detail), the screens and steps just before it " +
                 "(trail), the build (app and git code version, channel) and the phone (platform, device). Null when there is no such id.")]
    public Task<ErrorDetail?> Report(
        [Description("The report's id, from error_summary or recent_errors.")] long id,
        CancellationToken cancellationToken = default) =>
        db.ErrorReports
            .Where(r => r.Id == id)
            .Select(r => new ErrorDetail(r.Id, r.Kind, r.Channel, r.AppVersion, r.CodeVersion, r.Platform, r.Device, r.Source,
                r.Message, r.Detail, r.Route, r.Trail, r.Count, r.FirstAt, r.LastAt, r.ReceivedAt))
            .FirstOrDefaultAsync(cancellationToken);

    [McpServerTool(Name = "errors_by_version", ReadOnly = true, Idempotent = true)]
    [Description("Problems per app build (app version, git code version, channel): reports by kind and distinct phones, " +
                 "latest build first. Shows whether a release made things better or worse.")]
    public async Task<IReadOnlyList<VersionErrors>> ByVersion(
        [Description("How many days back, 1-90.")] int days = 14,
        [Description(ChannelHelp)] string? channel = null,
        CancellationToken cancellationToken = default) =>
        await Reports(days, channel, kind: null)
            .GroupBy(r => new { r.AppVersion, r.CodeVersion, r.Channel })
            .OrderByDescending(g => g.Max(r => r.ReceivedAt))
            .Take(ToolInput.MaxRows)
            .Select(g => new VersionErrors(g.Key.AppVersion, g.Key.CodeVersion, g.Key.Channel,
                g.Count(r => r.Kind == "crash"), g.Count(r => r.Kind == "error"), g.Count(r => r.Kind == "slow"),
                g.Select(r => r.InstallId).Distinct().Count(), g.Max(r => r.ReceivedAt)))
            .ToListAsync(cancellationToken);

    private IQueryable<ErrorReportRow> Reports(int days, string? channel, string? kind)
    {
        var from = DateTimeOffset.UtcNow.AddDays(-ToolInput.Days(days, KeptDays));
        var reports = db.ErrorReports.Where(r => r.ReceivedAt >= from);
        if (ToolInput.Filter(channel) is { } onlyChannel)
        {
            reports = reports.Where(r => r.Channel == onlyChannel);
        }

        if (ToolInput.Filter(kind) is { } onlyKind)
        {
            reports = reports.Where(r => r.Kind == onlyKind);
        }

        return reports;
    }
}

/// <summary>One problem (kind, source, message) across reports; <see cref="LatestId"/> opens with <c>error_report</c>.</summary>
public sealed record ErrorGroup(string Kind, string Source, string Message, int Reports, long Times, int Phones,
    DateTimeOffset FirstAt, DateTimeOffset LastAt, long LatestId);

/// <summary>One report without its stack trace or trail.</summary>
public sealed record ErrorLine(long Id, DateTimeOffset ReceivedAt, string Kind, string Channel, string AppVersion, string CodeVersion,
    string Platform, string? Device, string Source, string Message, string? Route, int Times);

/// <summary>One report in full.</summary>
public sealed record ErrorDetail(long Id, string Kind, string Channel, string AppVersion, string CodeVersion, string Platform,
    string? Device, string Source, string Message, string? Detail, string? Route, string? Trail, int Times,
    DateTimeOffset FirstAt, DateTimeOffset LastAt, DateTimeOffset ReceivedAt);

/// <summary>One build's problems by kind.</summary>
public sealed record VersionErrors(string AppVersion, string CodeVersion, string Channel, int Crashes, int Errors, int Slow,
    int Phones, DateTimeOffset LastReceivedAt);
