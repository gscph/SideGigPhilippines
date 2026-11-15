namespace MobileAppTheme.Auth;

/// <summary>Sign-in methods an <see cref="IIdentityProvider"/> supports. Add a flag (and its UI) for a new method.</summary>
[Flags]
public enum AuthMethods
{
    None = 0,
    Phone = 1,
    Google = 2,

    /// <summary>Sign in with Apple (iOS only; App Store 4.8 asks for it next to Google).</summary>
    Apple = 4,
}
