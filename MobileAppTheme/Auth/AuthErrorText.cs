using MobileAppTheme.Localization;

namespace MobileAppTheme.Auth;

/// <summary>The user-facing message for an <see cref="AuthError"/>, in the user's language.</summary>
public static class AuthErrorText
{
    /// <summary>Null when nothing should be shown (success, or the user cancelled).</summary>
    public static string? Describe(this AppText text, AuthError error) => error switch
    {
        AuthError.None or AuthError.Cancelled => null,
        AuthError.Network => text.Get(CommonCopy.AuthErrorNetwork),
        AuthError.InvalidCode => text.Get(CommonCopy.AuthErrorInvalidCode),
        AuthError.InvalidPhoneNumber => text.Get(CommonCopy.AuthErrorInvalidPhone),
        AuthError.TooManyAttempts => text.Get(CommonCopy.AuthErrorTooManyAttempts),
        AuthError.Misconfigured or AuthError.NotAvailable => text.Get(CommonCopy.AuthErrorNotAvailable),
        _ => text.Get(CommonCopy.AuthErrorUnknown),
    };
}
