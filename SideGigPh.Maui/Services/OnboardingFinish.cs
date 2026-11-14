using Microsoft.Extensions.Logging;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The end of an interview: its answers are saved (on the phone, then the profile on the server) behind a loading
/// screen, but the app opens after <see cref="WaitLimit"/> at most, and also when the save failed. The answers are
/// already on the phone and the home page loads its own data, so a slow network or a sign-in token that never comes
/// must not freeze the person on the loading screen. Either is logged as an error, so it reaches the error reports.
/// </summary>
public static class OnboardingFinish
{
    /// <summary>The longest the loading screen waits for the saves before the app opens anyway.</summary>
    public static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(15);

    /// <summary>Waits for <paramref name="saving"/> up to <see cref="WaitLimit"/>. True when it finished in time and without an error.</summary>
    public static async Task<bool> WaitAsync(Task saving, ILogger logger, TimeProvider clock)
    {
        try
        {
            await saving.WaitAsync(WaitLimit, clock);
            return true;
        }
        catch (TimeoutException)
        {
            logger.LogError("Finishing the interview took over {Seconds} s; opening the app anyway.", WaitLimit.TotalSeconds);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Finishing the interview failed; opening the app anyway.");
            return false;
        }
    }
}
