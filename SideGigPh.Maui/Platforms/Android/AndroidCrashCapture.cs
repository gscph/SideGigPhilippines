using Android.App;
using Android.Content;
using Android.Runtime;
using MobileAppTheme.Services;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;
using AndroidLog = Android.Util.Log;

namespace SideGigPh.Maui.Platforms.Android;

/// <summary>
/// The Android crashes .NET's own handlers never see, written into the <see cref="ErrorLog"/>:
/// a Java exception nothing caught (an SDK's background thread, a Java callback), and at each launch how the previous
/// runs ended (<c>ApplicationExitInfo</c>, Android 11+): native crashes, frozen screens (ANR) and kills for memory,
/// which leave nothing in the app's own log. Started from <c>MainApplication.OnCreate</c>. Never throws.
/// </summary>
public static class AndroidCrashCapture
{
    public const string JavaSource = "Crash.Java";
    public const string NetworkSource = "Crash.Network";

    // When the newest exit already reported happened (Unix ms), so each one is reported once.
    private const string LastSeenKey = "diagnostics.lastExitSeen";

    public static void Install(Context context, ErrorLog log)
    {
        try
        {
            var next = Java.Lang.Thread.DefaultUncaughtExceptionHandler;
            Java.Lang.Thread.DefaultUncaughtExceptionHandler = new JavaCrashHandler(log, next);
        }
        catch (Exception e)
        {
            AndroidLog.Warn(LogcatLoggerProvider.Tag, $"Crash capture: Java handler not installed: {e}");
        }

        // A lost connection that escaped every catch (a Java callback, a tap's handler) is not worth closing the app
        // over: it is reported and the app keeps running. Anything else still crashes, as it must.
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
        {
            if (NetworkErrors.IsNetwork(e.Exception))
            {
                e.Handled = true;
                log.Record(ErrorKind.Error, NetworkSource, "A lost connection reached the top; the app kept running", e.Exception);
            }
        };

        _ = Task.Run(() => ReportPastExits(context, log));
    }

    private static void ReportPastExits(Context context, ErrorLog log)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            return;
        }

        try
        {
            if (context.GetSystemService(Context.ActivityService) is not ActivityManager activities)
            {
                return;
            }

            var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(Preferences.Default.Get(LastSeenKey, 0L));
            var infos = activities.GetHistoricalProcessExitReasons(context.PackageName, 0, PastExits.LookBack) ?? [];
            var exits = PastExits.Unseen(infos.Select(ToPastExit), lastSeen);
            foreach (var exit in exits)
            {
                PastExits.Report(log, exit);
            }

            if (exits.Count > 0)
            {
                Preferences.Default.Set(LastSeenKey, exits[^1].At.ToUnixTimeMilliseconds());
            }
        }
        catch (Exception e)
        {
            AndroidLog.Warn(LogcatLoggerProvider.Tag, $"Crash capture: past exits not read: {e}");
        }
    }

    private static PastExit ToPastExit(ApplicationExitInfo info)
    {
        var cause = (int)info.Reason switch
        {
            4 => ExitCause.Crash,           // REASON_CRASH
            5 => ExitCause.NativeCrash,     // REASON_CRASH_NATIVE
            6 => ExitCause.NotResponding,   // REASON_ANR
            3 => ExitCause.LowMemory,       // REASON_LOW_MEMORY
            7 => ExitCause.StartFailed,     // REASON_INITIALIZATION_FAILURE
            _ => ExitCause.Other,
        };

        // 100 foreground, 125 foreground service, 200 visible: the person was looking at it.
        var onScreen = (int)info.Importance <= 200;
        var trace = cause == ExitCause.NotResponding ? ReadTrace(info) : null; // a native crash's trace is binary
        return new PastExit(cause, onScreen, info.Description, trace, DateTimeOffset.FromUnixTimeMilliseconds(info.Timestamp));
    }

    private static string? ReadTrace(ApplicationExitInfo info)
    {
        try
        {
            using var stream = info.TraceInputStream;
            if (stream is null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            var buffer = new char[64 * 1024]; // the main thread comes early in the dump
            var read = reader.ReadBlock(buffer, 0, buffer.Length);
            return new string(buffer, 0, read);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed class JavaCrashHandler(ErrorLog log, Java.Lang.Thread.IUncaughtExceptionHandler? next)
        : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        public void UncaughtException(Java.Lang.Thread thread, Java.Lang.Throwable exception)
        {
            try
            {
                // A .NET exception surfacing in Java was already recorded by ErrorReporting's .NET handler.
                if (exception.Class.Name != "android.runtime.JavaProxyThrowable")
                {
                    var message = $"{exception.Class.Name}: {exception.Message}";
                    log.RecordText(ErrorKind.Crash, JavaSource, message, $"Thread {thread.Name}\n{AndroidLog.GetStackTraceString(exception)}");
                    log.SaveBeforeExit();
                }
            }
            catch (Exception)
            {
                // Crashing anyway; Android's own handler below still runs.
            }

            next?.UncaughtException(thread, exception);
        }
    }
}
