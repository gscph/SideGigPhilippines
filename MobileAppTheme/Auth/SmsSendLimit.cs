namespace MobileAppTheme.Auth;

/// <summary>When this phone last asked for SMS codes (<see cref="Services.Local.PreferencesSmsSendHistory"/>), kept across restarts.</summary>
public interface ISmsSendHistory
{
    IReadOnlyList<DateTimeOffset> Load();

    void Save(IReadOnlyList<DateTimeOffset> sends);
}

/// <summary>
/// How often this phone may ask for an SMS sign-in code. Each SMS costs money and a person should need one in their
/// lifetime, so: another code to the same number only <see cref="Wait"/> after the last one, and at most
/// <see cref="PerDay"/> in any 24 hours, whatever the number. A typo fixed with "Use a different number" may get its code
/// at once (it still counts toward the day). The last number is kept only while the app runs; after a restart the wait
/// applies to any number. The sign-in provider has its own limits too; this keeps us well inside them and stops a stuck
/// person from spending a code a minute. It lives on the phone, so a reinstall resets it: the provider's limits and App
/// Check are what guard the bill.
/// </summary>
public sealed class SmsSendLimit(ISmsSendHistory history, TimeProvider clock)
{
    // The number the last code went to in this run of the app (E.164), null when unknown.
    private string? _lastNumber;

    /// <summary>The wait between two codes.</summary>
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(360);

    /// <summary>Most codes in any 24 hours.</summary>
    public const int PerDay = 3;

    private static readonly TimeSpan Day = TimeSpan.FromHours(24);

    /// <summary>
    /// How long until another code may be sent to <paramref name="number"/> (E.164; null or not yet valid: any number):
    /// zero when it may now, null when today's codes are used up.
    /// </summary>
    public TimeSpan? WaitLeft(string? number = null)
    {
        var now = clock.GetUtcNow();
        var recent = Recent(now);
        if (recent.Count >= PerDay)
        {
            return null;
        }

        if (recent.Count == 0 || (number is not null && _lastNumber is not null && number != _lastNumber))
        {
            return TimeSpan.Zero;
        }

        var left = recent[^1] + Wait - now;
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    /// <summary>A code was just sent to <paramref name="number"/> (E.164).</summary>
    public void Record(string number)
    {
        _lastNumber = number;
        var now = clock.GetUtcNow();
        history.Save([.. Recent(now), now]);
    }

    // The sends of the last 24 hours, oldest first.
    private List<DateTimeOffset> Recent(DateTimeOffset now) =>
        [.. history.Load().Where(at => at > now - Day && at <= now).Order()];
}
