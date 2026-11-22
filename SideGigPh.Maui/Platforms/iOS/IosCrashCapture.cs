using ObjCRuntime;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui.Platforms.iOS;

/// <summary>
/// The iOS failures .NET's own handlers never see, written into the <see cref="ErrorLog"/>: an Objective-C exception
/// (a UIKit or Firebase SDK call that threw) on its way into .NET, recorded as an error since .NET code may still catch
/// it, and a .NET exception on its way out into native code (a callback from iOS that threw), which ends the app before
/// <c>AppDomain.UnhandledException</c> runs, so that one is a crash and the log is saved at once for the next launch.
/// Started from <c>MauiProgram</c> right after the app is built. Never throws.
/// </summary>
public static class IosCrashCapture
{
    public const string ObjCSource = "Crash.ObjC";
    public const string NativeSource = "Crash.Native";

    public static void Install(ErrorLog log)
    {
        Runtime.MarshalObjectiveCException += (_, e) =>
        {
            try
            {
                var error = e.Exception;
                log.RecordText(ErrorKind.Error, ObjCSource, $"{error?.Name}: {error?.Reason}",
                    error?.CallStackSymbols is { } stack ? string.Join('\n', stack) : null);
            }
            catch (Exception)
            {
                // Best effort: the error itself must not be hidden by its report.
            }
        };
        Runtime.MarshalManagedException += (_, e) =>
        {
            try
            {
                log.Record(ErrorKind.Crash, NativeSource, e.Exception?.GetBaseException().Message ?? "Unhandled exception", e.Exception);
                log.SaveBeforeExit();
            }
            catch (Exception)
            {
                // Best effort: the crash itself must not be hidden by its report.
            }
        };
    }
}
