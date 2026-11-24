using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui.Services.Analytics;

/// <summary>
/// The app's only way to count something (our own analytics: no Google or Firebase Analytics). It never sends while
/// the person is acting: each event is written to the phone (<see cref="IPendingEventStore"/>) and
/// <see cref="AnalyticsUploader"/> uploads them later in the background. It never throws and never makes a caller wait
/// on the file: work runs on the thread pool, a failure is logged as a warning and dropped. The phone keeps at most
/// <see cref="MaxQueued"/> events (the oldest go first) for at most <see cref="KeepFor"/>.
/// </summary>
public sealed class TarbahoAnalytics(IPendingEventStore store, TimeProvider clock, ILogger<TarbahoAnalytics> logger)
{
    /// <summary>The most events kept on the phone; past it the oldest are dropped.</summary>
    public const int MaxQueued = 500;

    /// <summary>How long an event waits on the phone at most (the server wouldn't trust its time after that either).</summary>
    public static readonly TimeSpan KeepFor = AppEventRules.MaxAge;

    /// <summary>Away from the app this long, and coming back starts a new session.</summary>
    public static readonly TimeSpan NewSessionAfter = TimeSpan.FromMinutes(30);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly HashSet<string> _counted = [];
    private PendingEventsState? _state;
    private DateTimeOffset? _hiddenAt;

    /// <summary>The current session: a new id at launch and when the app comes back after <see cref="NewSessionAfter"/> away.</summary>
    public string SessionId { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>The app went to the background.</summary>
    public void AppHidden() => _hiddenAt = clock.GetUtcNow();

    /// <summary>The app is on screen again: after <see cref="NewSessionAfter"/> away it is a new session.</summary>
    public void AppShown()
    {
        if (_hiddenAt is { } at && clock.GetUtcNow() - at >= NewSessionAfter)
        {
            SessionId = Guid.NewGuid().ToString();
            lock (_counted)
            {
                _counted.Clear();
            }
        }

        _hiddenAt = null;
    }

    /// <summary>
    /// A search whose results loaded (typed words or a tapped trade): a <see cref="AppEventNames.Search"/> event, and a
    /// <see cref="AppEventNames.SearchEmpty"/> one too when nothing was found. The same search again in one session (Back
    /// to the results, a new sort or budget) isn't counted twice. Typed words are scrubbed of numbers and emails.
    /// Returns at once; the returned task (writing to the phone, never failing) is only for tests to wait on.
    /// </summary>
    public Task Search(string? term, string? category, string? service, string? area, int resultCount)
    {
        var searchTerm = AppEventRules.SearchTerm(term);
        lock (_counted)
        {
            if (!_counted.Add($"{searchTerm}|{category}|{service}|{area}"))
            {
                return Task.CompletedTask;
            }
        }

        var now = clock.GetUtcNow();
        var version = AppEventRules.Cut(BuildInfo.Current.Version, AppEventRules.MaxVersion);
        var search = new PendingEvent(Guid.NewGuid(), AppEventNames.Search, SessionId, category, area, searchTerm, resultCount, now, version);
        var events = resultCount == 0
            ? [search, search with { EventId = Guid.NewGuid(), Name = AppEventNames.SearchEmpty, ResultCount = null }]
            : new[] { search };
        return Task.Run(() => AddAsync(events));
    }

    /// <summary>The oldest events waiting, at most <paramref name="max"/>.</summary>
    public async Task<IReadOnlyList<PendingEvent>> OldestAsync(int max)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            return state.Events.Take(max).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Takes exactly these events off the phone (they were uploaded, or refused as bad).</summary>
    public async Task RemoveAsync(IReadOnlyCollection<PendingEvent> sent)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            var ids = sent.Select(e => e.EventId).ToHashSet();
            state.Events.RemoveAll(e => ids.Contains(e.EventId));
            await store.SaveAsync(state);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task AddAsync(IReadOnlyList<PendingEvent> events)
    {
        try
        {
            await _gate.WaitAsync();
            try
            {
                var state = await LoadAsync();
                state.Events.AddRange(events);
                Trim(state.Events, clock.GetUtcNow());
                await store.SaveAsync(state);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't keep {Count} usage events; dropped.", events.Count);
        }
    }

    /// <summary>Drops events older than <see cref="KeepFor"/>, then the oldest past <see cref="MaxQueued"/>.</summary>
    public static void Trim(List<PendingEvent> events, DateTimeOffset now)
    {
        events.RemoveAll(e => e.ClientTs < now - KeepFor);
        if (events.Count > MaxQueued)
        {
            events.RemoveRange(0, events.Count - MaxQueued);
        }
    }

    private async Task<PendingEventsState> LoadAsync()
    {
        if (_state is null)
        {
            _state = await store.LoadAsync() ?? new PendingEventsState();
            Trim(_state.Events, clock.GetUtcNow());
        }

        return _state;
    }
}
