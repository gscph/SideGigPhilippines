using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// One usage event (<c>app_events</c>) as the API writes it. Every field is optional except the name; never a name,
/// number, email, address or point: <see cref="UserId"/> is the account's opaque id and <see cref="Area"/> a town id.
/// </summary>
public sealed record AppEvent(string Name)
{
    public string? UserId { get; init; }
    public string? UserType { get; init; }
    public string? ServiceCategory { get; init; }
    public string? Area { get; init; }
    public int? MinutesToMatch { get; init; }
    public string? Reason { get; init; }
    public string? RequestId { get; init; }
}

/// <summary>
/// Our own usage counts, one plain parameterised INSERT per event into <c>app_events</c> (no analytics SDK). The
/// stores call <see cref="LogAsync"/> right after the real change is saved (source <c>server</c>, the arrival time is
/// the event time); the app's queued searches arrive through <see cref="SaveFromAppAsync"/>. Counting is never worth a
/// failed request: every write here catches, logs and carries on. Delete account clears the person's id
/// (<see cref="ForgetAsync"/>), and rows older than <see cref="KeepFor"/> are deleted.
/// </summary>
public sealed class AppEventStore(SideGigPhDb db, ILogger<AppEventStore> logger)
{
    /// <summary>How long an event is kept (the privacy policy says so too).</summary>
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(365);

    /// <summary>How often one API instance deletes what is older than <see cref="KeepFor"/>.</summary>
    public static readonly TimeSpan PurgeEvery = TimeSpan.FromDays(1);

    public const string FromServer = "server";
    public const string FromApp = "app";

    /// <summary>
    /// The longest a store's request waits on an event (the insert, or a <see cref="HasAsync"/> check), retries
    /// included: a slow or flaky database costs the count, never a slow answer.
    /// </summary>
    public static readonly TimeSpan GiveUpAfter = TimeSpan.FromSeconds(3);

    private static long _lastPurgeTicks;

    /// <summary>Writes a business event now. Never throws: a failure is logged and the caller's request goes on.</summary>
    public async Task LogAsync(AppEvent e)
    {
        try
        {
            using var giveUp = new CancellationTokenSource(GiveUpAfter);
            await InsertAsync(Guid.NewGuid(), Cut(e.Name, AppEventRules.MaxName)!, FromServer, Cut(e.UserId, 64), Cut(e.UserType, 10),
                sessionId: null, Cut(e.ServiceCategory, AppEventRules.MaxCategory), Cut(e.Area, AppEventRules.MaxArea), searchTerm: null,
                resultCount: null, e.MinutesToMatch, Cut(e.Reason, 40), Cut(e.RequestId, 64), appVersion: null, clientTs: null, giveUp.Token);
        }
        catch (Exception ex)
        {
            Failed(ex, e.Name);
        }
    }

    /// <summary>A store couldn't work out an event's details: logged, and its request goes on.</summary>
    public void Failed(Exception ex, string name) => logger.LogWarning(ex, "Couldn't log the {Event} event", name);

    /// <summary>True when <paramref name="userId"/> already has a <paramref name="name"/> event (the "first time only" ones).</summary>
    public async Task<bool> HasAsync(string name, string userId)
    {
        try
        {
            using var giveUp = new CancellationTokenSource(GiveUpAfter);
            return await db.AppEvents.AnyAsync(e => e.EventName == name && e.UserId == userId, giveUp.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't read the {Event} events", name);
            return true; // unsure: better a missing first-time event than a second one
        }
    }

    /// <summary>
    /// Keeps the app's queued events for <paramref name="uid"/>: only <see cref="AppEventNames.FromApp"/> names, a phone
    /// time that can't be believed dropped (<see cref="AppEventRules.Believable"/>), text cut to its column and search
    /// words scrubbed. An event id already kept (the upload sent twice) is skipped. False for a batch that is empty, too
    /// big or names any other event: nothing of it is kept.
    /// </summary>
    public async Task<bool> SaveFromAppAsync(string uid, IReadOnlyList<AppEventDto?>? events, CancellationToken cancellationToken)
    {
        if (events is not { Count: > 0 and <= AppEventRules.MaxBatch }
            || events.Any(e => e is null || e.EventId == Guid.Empty || !AppEventNames.FromApp.Contains(e.Name)))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var e in events.Distinct())
        {
            try
            {
                await InsertAsync(e!.EventId, e.Name, FromApp, uid, AppEventUserTypes.Customer, Cut(e.SessionId, AppEventRules.MaxSession),
                    Cut(e.ServiceCategory, AppEventRules.MaxCategory), Cut(e.Area, AppEventRules.MaxArea), AppEventRules.SearchTerm(e.SearchTerm),
                    e.ResultCount is >= 0 ? e.ResultCount : null, minutesToMatch: null, reason: null, requestId: null,
                    Cut(e.AppVersion, AppEventRules.MaxVersion), AppEventRules.Believable(e.ClientTs, now)?.UtcDateTime, cancellationToken);
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                // Already kept: the phone sent it again because the answer was lost on the way.
            }
        }

        await PurgeAsync(cancellationToken);
        return true;
    }

    /// <summary>Delete account: the person's events stay in the counts, without their id.</summary>
    public static Task ForgetAsync(SideGigPhDb db, string uid, CancellationToken cancellationToken) =>
        db.AppEvents.Where(e => e.UserId == uid).ExecuteUpdateAsync(set => set.SetProperty(e => e.UserId, (string?)null), cancellationToken);

    /// <summary>Deletes events older than <see cref="KeepFor"/>, at most once per <see cref="PurgeEvery"/> on this instance.</summary>
    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var last = Interlocked.Read(ref _lastPurgeTicks);
        if (now.Ticks - last < PurgeEvery.Ticks || Interlocked.CompareExchange(ref _lastPurgeTicks, now.Ticks, last) != last)
        {
            return;
        }

        try
        {
            var expired = now - KeepFor;
            await db.AppEvents.Where(e => e.ServerTs < expired).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Couldn't delete the old app events");
        }
    }

    private Task InsertAsync(Guid eventId, string name, string source, string? userId, string? userType, string? sessionId,
        string? serviceCategory, string? area, string? searchTerm, int? resultCount, int? minutesToMatch, string? reason,
        string? requestId, string? appVersion, DateTime? clientTs, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync($"""
            INSERT INTO dbo.app_events
                (event_id, event_name, source, user_id, user_type, session_id, service_category, area, search_term,
                 result_count, minutes_to_match, reason, request_id, app_version, client_ts)
            VALUES
                ({eventId}, {name}, {source}, {userId}, {userType}, {sessionId}, {serviceCategory}, {area}, {searchTerm},
                 {resultCount}, {minutesToMatch}, {reason}, {requestId}, {appVersion}, {clientTs})
            """, cancellationToken);

    private static string? Cut(string? text, int max) => AppEventRules.Cut(text, max);
}
