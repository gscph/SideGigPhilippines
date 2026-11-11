using System.Globalization;

namespace MobileAppTheme.Localization;

/// <summary>
/// Holds the language the user chose and resolves every <see cref="LocalizedText"/> to it.
/// Registered as a singleton by <c>AddMobileAppTheme</c>; the app applies the saved language at startup.
/// </summary>
public sealed class AppText
{
    private AppLanguage _language = AppLanguage.English;

    public AppLanguage Language => _language;

    public void Use(AppLanguage language) => _language = language;

    public string Get(LocalizedText text) => _language == AppLanguage.Taglish ? text.Taglish : text.English;

    public string Format(LocalizedText text, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(text), args);
}
