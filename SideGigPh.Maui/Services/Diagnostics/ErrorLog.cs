using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// The problems the app had on this phone, waiting to be sent (<see cref="ErrorReportSender"/>): errors it logged
/// (<see cref="ErrorLogLoggerProvider"/>), crashes and a loading screen stuck too long (<see cref="ErrorReporting"/>).
/// Each report is scrubbed of personal data the moment it is recorded (<see cref="ErrorReportRules.Scrub"/>), carries
/// the screen it happened on and the last few screens and steps before it (<see cref="Visit"/>, <see cref="Step"/>),
/// and the same problem repeated only counts up. Kept in <c>diagnostics.json</c> (<see cref="IErrorLogStore"/>), at
/// most <see cref="MaxQueued"/> reports for <see cref="KeepFor"/>. Recording never throws and never logs (it is the
/// logger's own sink). With the Me page's switch off nothing is recorded and the queue is emptied.
/// </summary>
public sealed class ErrorLog(IErrorLogStore store, TimeProvider clock)
{
    /// <summary>Most reports kept on the phone; the oldest go first.</summary>
    public const int MaxQueued = 50;

    /// <summary>Screens and steps remembered for the next report's trail.</summary>
    public const int TrailLength = 15;

    /// <summary>A report not sent by then (signed out, offline, switch off) is dropped.</summary>
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(14);

    /// <summary>How long recording waits before writing the file, so a burst of errors is one write.</summary>
    public static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(2);

    private readonly object _gate = new();
    private readonly Queue<string> _trail = new();
    private DiagnosticsState _state = new();
    private Task? _loading;
    private bool _saveScheduled;

    /// <summary>The screen on now, e.g. <c>/start/pro/done</c>.</summary>
    public string? Route { get; private set; }

    public bool SendReports
    {
        get
        {
            lock (_gate)
            {
                return _state.SendReports;
            }
        }
    }

    /// <summary>This install's random id (made on first load).</summary>
    public string InstallId
    {
        get
        {
            lock (_gate)
            {
                return _state.InstallId ??= Guid.NewGuid().ToString();
            }
        }
    }

    /// <summary>The waiting reports, oldest first.</summary>
    public IReadOnlyList<ErrorReport> Queued
    {
        get
        {
            lock (_gate)
            {
                return [.. _state.Reports];
            }
        }
    }

    /// <summary>Reads the saved log once; reports recorded before it finished are kept too.</summary>
    public Task LoadAsync() => _loading ??= ReadAsync();

    /// <summary>The Me page's switch. Turning it off drops every waiting report.</summary>
    public async Task SetSendReportsAsync(bool send)
    {
        await LoadAsync();
        lock (_gate)
        {
            _state.SendReports = send;
            if (!send)
            {
                _state.Reports.Clear();
            }
        }

        await SaveAsync();
    }

    /// <summary>The person opened a screen: it becomes <see cref="Route"/> and a line in the trail.</summary>
    public void Visit(string route)
    {
        Route = ErrorReportRules.Clean(route, ErrorReportRules.MaxRoute);
        Step($"→ {Route}");
    }

    /// <summary>Something worth knowing if a problem follows (scrubbed like a report).</summary>
    public void Step(string text)
    {
        var line = $"{clock.GetUtcNow():HH:mm:ss} {ErrorReportRules.Scrub(text)}";
        lock (_gate)
        {
            _trail.Enqueue(line.Length > 200 ? line[..200] : line);
            while (_trail.Count > TrailLength)
            {
                _trail.Dequeue();
            }
        }
    }

    /// <summary>Keeps one problem (scrubbed), or counts it up when the same one is already waiting.</summary>
    public void Record(ErrorKind kind, string source, string message, Exception? exception = null)
    {
        var now = clock.GetUtcNow();
        lock (_gate)
        {
            if (!_state.SendReports)
            {
                return;
            }

            var report = new ErrorReport(
                kind,
                ErrorReportRules.Clean(source, ErrorReportRules.MaxSource) ?? "—",
                ErrorReportRules.Clean(message, ErrorReportRules.MaxMessage) ?? "—",
                ErrorReportRules.Clean(exception?.ToString(), ErrorReportRules.MaxDetail),
                Route,
                ErrorReportRules.Clean(string.Join('\n', _trail), ErrorReportRules.MaxTrail),
                1,
                now,
                now);
            Merge(report);
        }

        ScheduleSave();
    }

    /// <summary>Writes the log now. Never throws.</summary>
    public async Task SaveAsync()
    {
        DiagnosticsState copy;
        lock (_gate)
        {
            _saveScheduled = false;
            copy = new DiagnosticsState { InstallId = _state.InstallId ??= Guid.NewGuid().ToString(), SendReports = _state.SendReports, Reports = [.. _state.Reports] };
        }

        try
        {
            await store.SaveAsync(copy);
        }
        catch (Exception)
        {
            // The log is best effort; logging this failure would only record it again.
        }
    }

    /// <summary>Writes the log before the app closes after a crash: waits at most two seconds.</summary>
    public void SaveBeforeExit()
    {
        try
        {
            SaveAsync().Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // Closing anyway.
        }
    }

    /// <summary>Drops reports the server kept (a report counted up while sending stays, with only its new count).</summary>
    public async Task RemoveSentAsync(IReadOnlyList<ErrorReport> sent)
    {
        lock (_gate)
        {
            foreach (var report in sent)
            {
                var index = _state.Reports.FindIndex(r => SameProblem(r, report));
                if (index < 0)
                {
                    continue;
                }

                var waiting = _state.Reports[index];
                if (waiting.Count <= report.Count)
                {
                    _state.Reports.RemoveAt(index);
                }
                else
                {
                    _state.Reports[index] = waiting with { Count = waiting.Count - report.Count, FirstAt = report.LastAt };
                }
            }
        }

        await SaveAsync();
    }

    private async Task ReadAsync()
    {
        DiagnosticsState? saved;
        try
        {
            saved = await store.LoadAsync();
        }
        catch (Exception)
        {
            saved = null;
        }

        var oldest = clock.GetUtcNow() - KeepFor;
        lock (_gate)
        {
            var recorded = _state.Reports;
            _state = saved ?? new DiagnosticsState();
            _state.InstallId ??= Guid.NewGuid().ToString();
            _state.Reports = [.. (_state.Reports ?? []).Where(r => r.LastAt >= oldest)];
            if (!_state.SendReports)
            {
                _state.Reports.Clear();
            }
            else
            {
                recorded.ForEach(Merge);
            }
        }

        await SaveAsync();
    }

    // Under _gate.
    private void Merge(ErrorReport report)
    {
        var index = _state.Reports.FindIndex(r => SameProblem(r, report));
        if (index >= 0)
        {
            var waiting = _state.Reports[index];
            _state.Reports[index] = waiting with
            {
                Count = waiting.Count + report.Count,
                LastAt = report.LastAt > waiting.LastAt ? report.LastAt : waiting.LastAt,
                Route = report.Route ?? waiting.Route,
                Trail = report.Trail ?? waiting.Trail,
            };
            return;
        }

        _state.Reports.Add(report);
        if (_state.Reports.Count > MaxQueued)
        {
            _state.Reports.RemoveAt(0);
        }
    }

    // The same problem: same kind, place and message, and the same top of the stack trace.
    private static bool SameProblem(ErrorReport a, ErrorReport b) =>
        a.Kind == b.Kind && a.Source == b.Source && a.Message == b.Message && FirstLines(a.Detail) == FirstLines(b.Detail);

    private static string? FirstLines(string? detail) =>
        detail is null ? null : string.Join('\n', detail.Split('\n').Take(3));

    private void ScheduleSave()
    {
        lock (_gate)
        {
            if (_saveScheduled)
            {
                return;
            }

            _saveScheduled = true;
        }

        _ = Task.Delay(SaveDelay, clock).ContinueWith(_ => SaveAsync(), TaskScheduler.Default).Unwrap();
    }
}
