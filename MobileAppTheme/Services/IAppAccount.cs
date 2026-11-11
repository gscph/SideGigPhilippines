using MobileAppTheme.Localization;

namespace MobileAppTheme.Services;

/// <summary>
/// The person using this phone, as the shared <c>MePage</c> sees them: their app language and
/// Start over. The app implements it (it knows what to save and what to delete) and registers it.
/// Until there is sign-in, the "account" is what's saved on the phone; with auth, Start over
/// becomes sign out / delete account.
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
}
