using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>Why the operating system says a previous run of the app ended.</summary>
public enum ExitCause
{
    /// <summary>A Java or .NET exception nothing caught.</summary>
    Crash,

    /// <summary>A crash in native code (the WebView, a graphics driver, the runtime).</summary>
    NativeCrash,

    /// <summary>The screen froze long enough for "App isn't responding" (ANR).</summary>
    NotResponding,

    /// <summary>Android closed it to free memory.</summary>
    LowMemory,

    /// <summary>It failed while starting.</summary>
    StartFailed,

    /// <summary>Anything normal: the person closed it, an update, the app's own exit.</summary>
    Other,
}

/// <summary>
/// How one previous run ended, as the phone reports it at the next launch (Android: <c>ApplicationExitInfo</c>).
/// </summary>
/// <param name="WasOnScreen">The app was in front of the person when it ended.</param>
/// <param name="Description">The system's one-line reason.</param>
/// <param name="Trace">The thread dump an ANR leaves (text), if any.</param>
public sealed record PastExit(ExitCause Cause, bool WasOnScreen, string? Description, string? Trace, DateTimeOffset At);

/// <summary>
/// Turns the ways previous runs ended into crash reports, so a crash the app could not see (native code, a frozen
/// screen, Android killing it) still reaches us. Each exit is reported once (<see cref="Unseen"/>).
/// </summary>
public static class PastExits
{
    public const string Source = "Crash.LastRun";

    /// <summary>How many past exits are asked for at each launch.</summary>
    public const int LookBack = 10;

    /// <summary>The exits after <paramref name="lastSeen"/>, oldest first.</summary>
    public static IReadOnlyList<PastExit> Unseen(IEnumerable<PastExit> exits, DateTimeOffset lastSeen) =>
        [.. exits.Where(e => e.At > lastSeen).OrderBy(e => e.At)];

    /// <summary>Writes the exit into the log; a normal exit, or low memory while in the background, is ignored.</summary>
    public static void Report(ErrorLog log, PastExit exit)
    {
        var message = exit.Cause switch
        {
            ExitCause.Crash => "Last run crashed",
            ExitCause.NativeCrash => "Last run crashed in native code",
            ExitCause.NotResponding => "Last run froze (app not responding)",
            ExitCause.StartFailed => "Last run failed to start",
            ExitCause.LowMemory when exit.WasOnScreen => "Last run was closed by Android for memory while on screen",
            _ => null,
        };
        if (message is null)
        {
            return;
        }

        log.RecordFromLastRun(ErrorKind.Crash, Source, message, Detail(exit), exit.At);
    }

    /// <summary>The system's reason, then the main thread's stack from an ANR dump (the part that shows the freeze).</summary>
    public static string? Detail(PastExit exit)
    {
        var main = MainThread(exit.Trace);
        var parts = new[] { exit.Description, main }.Where(p => !string.IsNullOrWhiteSpace(p));
        var text = string.Join("\n", parts);
        return text.Length == 0 ? null : text;
    }

    /// <summary>The <c>"main"</c> thread's block of an ANR dump, or null.</summary>
    public static string? MainThread(string? trace)
    {
        if (string.IsNullOrEmpty(trace))
        {
            return null;
        }

        var start = trace.IndexOf("\"main\"", StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var end = trace.IndexOf("\n\n", start, StringComparison.Ordinal);
        return (end < 0 ? trace[start..] : trace[start..end]).Trim();
    }
}
