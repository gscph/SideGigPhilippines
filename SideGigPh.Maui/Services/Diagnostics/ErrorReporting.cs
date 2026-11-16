using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// Turns error reporting on when the app starts (<see cref="Start"/>, from <c>App</c>): crashes and unobserved task
/// errors go into the <see cref="ErrorLog"/> (a crash saves it before the app closes), a loading screen up longer than
/// <see cref="BusyIndicator.StuckAfter"/> is a <see cref="ErrorKind.Slow"/> report, and the waiting reports are sent
/// <see cref="FirstTry"/> after launch and each time the app goes to the background (<see cref="SendInBackground"/>,
/// from <c>App</c>), never on a timer: a phone with nothing to report never calls, and one with reports calls once. Errors the app logs
/// arrive through <see cref="ErrorLogLoggerProvider"/>; screens through <c>Components/Shared/ScreenTrail</c>.
/// </summary>
public sealed class ErrorReporting(ErrorLog log, ErrorReportSender sender, BusyIndicator busy)
{
    /// <summary>The first send after launch: late enough not to compete with the app's own start-up calls.</summary>
    public static readonly TimeSpan FirstTry = TimeSpan.FromSeconds(30);

    public const string CrashSource = "Crash";
    public const string LoadingSource = "Loading";

    private int _started;

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            log.Record(ErrorKind.Crash, CrashSource, exception?.GetBaseException().Message ?? "Unhandled exception", exception);
            log.SaveBeforeExit();
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
            log.Record(ErrorKind.Error, CrashSource, e.Exception.GetBaseException().Message, e.Exception);
        busy.Stuck += _ =>
            log.Record(ErrorKind.Slow, LoadingSource, $"The loading screen stayed up over {(int)BusyIndicator.StuckAfter.TotalSeconds} s.");
        busy.Unstuck += waited => log.Step($"The loading screen closed after {(int)waited.TotalSeconds} s.");

        _ = SendAfterLaunchAsync();
    }

    /// <summary>The app just went to the background: send what is waiting (nothing when nothing is).</summary>
    public void SendInBackground() => _ = SendSafelyAsync();

    private async Task SendAfterLaunchAsync()
    {
        try
        {
            await log.LoadAsync();
            await Task.Delay(FirstTry);
        }
        catch (Exception)
        {
            return;
        }

        await SendSafelyAsync();
    }

    private async Task SendSafelyAsync()
    {
        try
        {
            await sender.SendAsync();
        }
        catch (Exception)
        {
            // Best effort: never take the app down over its error reports.
        }
    }
}
