namespace MobileAppTheme.Auth;

/// <summary>Sign-in methods an <see cref="IIdentityProvider"/> supports. Add a flag (and its UI) for a new method.</summary>
[Flags]
public enum AuthMethods
{
    None = 0,
    Phone = 1,
    Google = 2,
}
