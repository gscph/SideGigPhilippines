using Microsoft.Maui.Storage;
using MobileAppTheme.Auth;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// <see cref="ISmsSendHistory"/> in the phone's app preferences (a few timestamps, Unix milliseconds). Where there are
/// no preferences (tests, an unsupported platform) nothing is remembered, so the limit then only lasts while the app runs.
/// </summary>
public sealed class PreferencesSmsSendHistory : ISmsSendHistory
{
    private const string Key = "auth.smsSends";

    private IReadOnlyList<DateTimeOffset> _memory = [];

    public IReadOnlyList<DateTimeOffset> Load()
    {
        try
        {
            var saved = Preferences.Default.Get(Key, string.Empty);
            return [.. saved.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => long.TryParse(part, out var ms) ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : (DateTimeOffset?)null)
                .OfType<DateTimeOffset>()];
        }
        catch (Exception)
        {
            return _memory;
        }
    }

    public void Save(IReadOnlyList<DateTimeOffset> sends)
    {
        _memory = sends;
        try
        {
            Preferences.Default.Set(Key, string.Join(',', sends.Select(at => at.ToUnixTimeMilliseconds())));
        }
        catch (Exception)
        {
            // Kept in memory only.
        }
    }
}
