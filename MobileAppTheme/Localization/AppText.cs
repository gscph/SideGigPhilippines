using System.Globalization;

namespace MobileAppTheme.Localization;

/// <summary>
/// Holds the language the user chose and resolves every <see cref="LocalizedText"/> to it.
/// Registered as a singleton by <c>AddMobileAppTheme</c>; the app applies the saved language at startup.
/// </summary>
public sealed class AppText
{
    public AppLanguage Language { get; private set; } = AppLanguage.English;

    public void Use(AppLanguage language) => Language = language;

    public string Get(LocalizedText text) => Language == AppLanguage.Taglish ? text.Taglish : text.English;

    public string Format(LocalizedText text, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(text), args);
}
