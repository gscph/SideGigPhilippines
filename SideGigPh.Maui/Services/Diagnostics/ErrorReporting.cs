using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// Turns error reporting on when the app starts (<see cref="Start"/>, from <c>App</c>): crashes and unobserved task
/// errors go into the <see cref="ErrorLog"/> (a crash saves it before the app closes), a loading screen up longer than
/// <see cref="BusyIndicator.StuckAfter"/> is a <see cref="ErrorKind.Slow"/> report. A new problem is sent
/// <see cref="SendSoonAfter"/> after it is recorded (a burst is one send), and what is waiting is sent again when the
/// app goes to the background (<see cref="AppHidden"/>), <see cref="FirstTry"/> after launch and every
/// <see cref="Every"/> while the app is on screen (what waited offline or signed out). Errors the app logs arrive
/// through <see cref="ErrorLogLoggerProvider"/>, every failed API call among them (<c>ApiClient</c>); screens through
/// <c>Components/Shared/ScreenTrail</c>.
/// </summary>
public sealed class ErrorReporting(ErrorLog log, ErrorReportSender sender, BusyIndicator busy, AppVisibility visibility, TimeProvider clock)
{
    /// <summary>The first send after launch: late enough not to compete with the app's own start-up calls.</summary>
    public static readonly TimeSpan FirstTry = TimeSpan.FromSeconds(30);

    /// <summary>How often waiting reports are sent after that, for what couldn't go when it happened.</summary>
    public static readonly TimeSpan Every = TimeSpan.FromMinutes(10);

    /// <summary>How long after a problem is recorded it is sent: long enough that a burst of errors is one send.</summary>
    public static readonly TimeSpan SendSoonAfter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// After a quick send that couldn't go (offline, signed out, a failing sign-in token that itself logs an error), the
    /// next quick send waits this long, so a failure that repeats never turns into a send every few seconds.
    /// </summary>
    public static readonly TimeSpan QuietAfterFailedSend = TimeSpan.FromMinutes(1);

    private int _sendScheduled;
    private long _quietUntilTicks;

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
        log.Recorded += () => _ = SendSoonAsync();

        _ = SendInTheBackgroundAsync();
    }

    /// <summary>
    /// The app went to the background (the window's <c>Stopped</c>): send what is waiting now, while the system still
    /// gives the app a few seconds, so a problem the person just had doesn't wait for the next launch.
    /// </summary>
    public void AppHidden() => _ = SendNowAsync();

    private async Task SendSoonAsync()
    {
        if (Interlocked.Exchange(ref _sendScheduled, 1) == 1)
        {
            return; // a send is already on its way and takes this one too
        }

        try
        {
            await Task.Delay(SendSoonAfter, clock);
        }
        finally
        {
            Volatile.Write(ref _sendScheduled, 0);
        }

        if (visibility.IsVisible && clock.GetUtcNow().UtcTicks >= Interlocked.Read(ref _quietUntilTicks)
            && !await SendNowAsync())
        {
            Interlocked.Exchange(ref _quietUntilTicks, (clock.GetUtcNow() + QuietAfterFailedSend).UtcTicks);
        }
    }

    private async Task<bool> SendNowAsync()
    {
        try
        {
            return await sender.SendAsync();
        }
        catch (Exception)
        {
            return false; // Best effort: never take the app down over its error reports.
        }
    }

    private async Task SendInTheBackgroundAsync()
    {
        try
        {
            await log.LoadAsync();
            await Task.Delay(FirstTry);
            using var timer = new PeriodicTimer(Every);
            do
            {
                if (visibility.IsVisible)
                {
                    await sender.SendAsync();
                }
            }
            while (await timer.WaitForNextTickAsync());
        }
        catch (Exception)
        {
            // Best effort: never take the app down over its error reports.
        }
    }
}
