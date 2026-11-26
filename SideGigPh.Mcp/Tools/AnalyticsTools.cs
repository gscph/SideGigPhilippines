using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Mcp.Tools;

/// <summary>
/// Our own usage events (<c>dbo.app_events</c>), the same questions as <c>analytics/weekly.sql</c> with the window as
/// a parameter: days and weeks in Philippine time (UTC+8), weeks starting Monday. Counts only: no answer carries a
/// user id, only how many distinct people.
/// </summary>
[McpServerToolType]
public sealed class AnalyticsTools(SideGigPhDb db)
{
    public const string EventList =
        $"{AppEventNames.SignUp}, {AppEventNames.Search}, {AppEventNames.SearchEmpty}, {AppEventNames.RequestCreated}, " +
        $"{AppEventNames.RequestMatched}, {AppEventNames.JobCompleted}, {AppEventNames.Rehire}, " +
        $"{AppEventNames.WorkerProfileComplete}, {AppEventNames.WorkerFirstJob}, {AppEventNames.ReportUser}, {AppEventNames.BlockUser}";

    private const int PhilippineHours = 8;

    // A Monday: a day's distance from it, mod 7, is how far that day is into its week.
    private static readonly DateTime Monday = new(1900, 1, 1);

    [McpServerTool(Name = "events_per_week", ReadOnly = true, Idempotent = true)]
    [Description("How many of each usage event happened per week (weeks start Monday, Philippine time), and how many distinct people. " +
                 "A 'request' is a household's job post; 'matched' is the household picking a worker's offer. Events: " + EventList + ".")]
    public async Task<IReadOnlyList<EventCount>> EventsPerWeek(
        [Description("Only this event name; leave empty for all.")] string? eventName = null,
        [Description("How many weeks, this one included, 1-52.")] int weeks = 12,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.AddHours(PhilippineHours).Date;
        var firstMonday = today.AddDays(-((today - Monday).Days % 7) - 7 * (Math.Clamp(weeks, 1, 52) - 1));
        var rows = await From(firstMonday.AddHours(-PhilippineHours), eventName)
            .Select(e => new { e.EventName, e.UserId, Day = (e.ClientTs ?? e.ServerTs).AddHours(PhilippineHours).Date })
            .GroupBy(e => new { e.EventName, Week = e.Day.AddDays(-(EF.Functions.DateDiffDay(Monday, e.Day) % 7)) })
            .Select(g => new EventCount(DateOnly.FromDateTime(g.Key.Week), g.Key.EventName, g.Count(), g.Select(e => e.UserId).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return [.. rows.OrderByDescending(r => r.Period).ThenBy(r => r.EventName)];
    }

    [McpServerTool(Name = "events_per_day", ReadOnly = true, Idempotent = true)]
    [Description("How many of each usage event happened per day (Philippine time) in the last N days, today included. Events: " + EventList + ".")]
    public async Task<IReadOnlyList<EventCount>> EventsPerDay(
        [Description("Only this event name; leave empty for all.")] string? eventName = null,
        [Description("How many days back, 1-365.")] int days = 14,
        CancellationToken cancellationToken = default)
    {
        var firstDay = DateTime.UtcNow.AddHours(PhilippineHours).Date.AddDays(1 - ToolInput.Days(days));
        var rows = await From(firstDay.AddHours(-PhilippineHours), eventName)
            .GroupBy(e => new { e.EventName, Day = (e.ClientTs ?? e.ServerTs).AddHours(PhilippineHours).Date })
            .Select(g => new EventCount(DateOnly.FromDateTime(g.Key.Day), g.Key.EventName, g.Count(), g.Select(e => e.UserId).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return [.. rows.OrderByDescending(r => r.Period).ThenBy(r => r.EventName)];
    }

    [McpServerTool(Name = "fulfilment", ReadOnly = true, Idempotent = true)]
    [Description("Do households get served? Job posts created, posts matched (a worker's offer picked), jobs completed, rehires, " +
                 "and the average minutes from post to pick, over the last N days.")]
    public async Task<Fulfilment> Fulfilment(
        [Description("How many days back, 1-365.")] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var events = Since(ToolInput.Days(days));
        var matched = events.Where(e => e.EventName == AppEventNames.RequestMatched);
        return new Fulfilment(
            await events.CountAsync(e => e.EventName == AppEventNames.RequestCreated, cancellationToken),
            await matched.Select(e => e.RequestId).Distinct().CountAsync(cancellationToken), // a cancelled pick picked again is one post
            await events.CountAsync(e => e.EventName == AppEventNames.JobCompleted, cancellationToken),
            await events.CountAsync(e => e.EventName == AppEventNames.Rehire, cancellationToken),
            await matched.AverageAsync(e => (double?)e.MinutesToMatch, cancellationToken));
    }

    [McpServerTool(Name = "unmet_demand", ReadOnly = true, Idempotent = true)]
    [Description("The searches that found no worker (search_empty), by town, trade and typed words, most frequent first: " +
                 "what people want that we don't have.")]
    public async Task<IReadOnlyList<SearchCount>> UnmetDemand(
        [Description("How many days back, 1-365.")] int days = 7,
        [Description("How many rows, 1-200.")] int top = 20,
        CancellationToken cancellationToken = default) =>
        await Since(ToolInput.Days(days), AppEventNames.SearchEmpty)
            .GroupBy(e => new { e.Area, e.ServiceCategory, e.SearchTerm })
            .OrderByDescending(g => g.Count())
            .Take(ToolInput.Top(top))
            .Select(g => new SearchCount(g.Key.Area, g.Key.ServiceCategory, g.Key.SearchTerm, g.Count(), g.Count()))
            .ToListAsync(cancellationToken);

    [McpServerTool(Name = "demand_by_trade_and_town", ReadOnly = true, Idempotent = true)]
    [Description("Searches per trade and town, with how many came back empty, over the last N days.")]
    public async Task<IReadOnlyList<SearchCount>> DemandByTradeAndTown(
        [Description("How many days back, 1-365.")] int days = 7,
        CancellationToken cancellationToken = default) =>
        await Since(ToolInput.Days(days), AppEventNames.Search)
            .GroupBy(e => new { e.Area, e.ServiceCategory })
            .OrderByDescending(g => g.Count())
            .Take(ToolInput.MaxRows)
            .Select(g => new SearchCount(g.Key.Area, g.Key.ServiceCategory, null, g.Count(), g.Count(e => e.ResultCount == 0)))
            .ToListAsync(cancellationToken);

    [McpServerTool(Name = "worker_funnel", ReadOnly = true, Idempotent = true)]
    [Description("Skilled workers from sign-up to a published profile to their first completed job (distinct people, all time).")]
    public async Task<WorkerFunnel> WorkerFunnel(CancellationToken cancellationToken = default) => new(
        await People(db.AppEvents.Where(e => e.EventName == AppEventNames.SignUp && e.UserType == AppEventUserTypes.Worker), cancellationToken),
        await People(db.AppEvents.Where(e => e.EventName == AppEventNames.WorkerProfileComplete), cancellationToken),
        await People(db.AppEvents.Where(e => e.EventName == AppEventNames.WorkerFirstJob), cancellationToken));

    [McpServerTool(Name = "trust_reports", ReadOnly = true, Idempotent = true)]
    [Description("Reports and blocks between people, by reason, over the last N days.")]
    public async Task<IReadOnlyList<ReasonCount>> TrustReports(
        [Description("How many days back, 1-365.")] int days = 30,
        CancellationToken cancellationToken = default) =>
        await Since(ToolInput.Days(days))
            .Where(e => e.EventName == AppEventNames.ReportUser || e.EventName == AppEventNames.BlockUser)
            .GroupBy(e => new { e.EventName, e.Reason })
            .OrderByDescending(g => g.Count())
            .Select(g => new ReasonCount(g.Key.EventName, g.Key.Reason, g.Count()))
            .ToListAsync(cancellationToken);

    /// <summary>The events of the last <paramref name="days"/> days, of one name if given.</summary>
    private IQueryable<AppEventRow> Since(int days, string? eventName = null) => From(DateTime.UtcNow.AddDays(-days), eventName);

    /// <summary>The events since <paramref name="from"/> (UTC; when they happened, on the phone if it said), of one name if given.</summary>
    private IQueryable<AppEventRow> From(DateTime from, string? eventName)
    {
        var name = ToolInput.Filter(eventName);
        var events = db.AppEvents.Where(e => (e.ClientTs ?? e.ServerTs) >= from);
        return name is null ? events : events.Where(e => e.EventName == name);
    }

    private static Task<int> People(IQueryable<AppEventRow> events, CancellationToken cancellationToken) =>
        events.Select(e => e.UserId).Distinct().CountAsync(e => e != null, cancellationToken);
}

/// <summary>One event name's count in a day or week (<see cref="Period"/> is the day, or the week's Monday).</summary>
public sealed record EventCount(DateOnly Period, string EventName, int Events, int People);

public sealed record Fulfilment(int PostsCreated, int PostsMatched, int JobsCompleted, int Rehires, double? AvgMinutesToMatch);

/// <summary>Searches for a town and trade (and typed words, for empty ones); <see cref="Empty"/> found no worker.</summary>
public sealed record SearchCount(string? Town, string? Trade, string? SearchTerm, int Searches, int Empty);

public sealed record WorkerFunnel(int SignedUp, int ProfileComplete, int FirstJob);

public sealed record ReasonCount(string EventName, string? Reason, int Events);
