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

    /// <summary>
    /// Signs out but keeps this phone's answers, so the next sign-in carries on where the person was (e.g. their
    /// number already belongs to another account, which they then sign in to). Forgets what was loaded for this account.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Another account is now signed in on this phone: adding a number found it on that account, which the person was
    /// signed in to with the same code (<see cref="Auth.AuthResult.SwitchedAccount"/>). Keeps this phone's answers and
    /// forgets what was loaded for the last account, like <see cref="SignOutAsync"/> without signing out.
    /// </summary>
    Task AccountSwitchedAsync();

    /// <summary>Deletes everything the person made on this phone, so <c>/</c> starts the app fresh.</summary>
    Task StartOverAsync();

    /// <summary>
    /// Deletes the person's account and everything in it, on the server and at the sign-in provider, then signs them
    /// out (App Store 5.1.1(v), Google Play). Throws when the server didn't confirm it, so the page can say so.
    /// </summary>
    Task DeleteAccountAsync();
}
