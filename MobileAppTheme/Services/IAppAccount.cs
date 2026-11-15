using MobileAppTheme.Localization;

namespace MobileAppTheme.Services;

/// <summary>
/// The person using this phone, as the shared <c>MePage</c> sees them: their app language, Start over
/// and Delete account. The app implements it (it knows what to save and what to delete) and registers it.
/// </summary>
public interface IAppAccount
{
    /// <summary>Raised after the language changes, so layouts re-render their labels.</summary>
    event Action? Changed;

    AppLanguage Language { get; }

    /// <summary>Switches <see cref="AppText"/> to the language, saves it, then raises <see cref="Changed"/>.</summary>
    Task SetLanguageAsync(AppLanguage language);

    /// <summary>Deletes everything the person made on this phone, so <c>/</c> starts the app fresh.</summary>
    Task StartOverAsync();

    /// <summary>
    /// Deletes the person's account and everything in it, on the server and at the sign-in provider, then signs them
    /// out (App Store 5.1.1(v), Google Play). Throws when the server didn't confirm it, so the page can say so.
    /// </summary>
    Task DeleteAccountAsync();
}
