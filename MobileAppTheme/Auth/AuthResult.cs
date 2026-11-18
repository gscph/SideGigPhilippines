namespace MobileAppTheme.Auth;

/// <summary>
/// Outcome of a sign-in attempt. On failure, <see cref="Error"/> is the provider-neutral reason
/// the UI shows (in the user's language); <see cref="Detail"/> is the provider's own text for logs
/// and debugging, never shown as the main message.
/// </summary>
public sealed class AuthResult
{
    public bool IsSuccess { get; private init; }

    public AuthUser? User { get; private init; }

    public AuthError Error { get; private init; }

    public string? Detail { get; private init; }

    /// <summary>
    /// Adding a number (<see cref="IIdentityProvider.LinkPhoneAsync"/>) found it on another account, so the person was
    /// signed in to that account instead, with the code they just typed: <see cref="User"/> is now a different account.
    /// </summary>
    public bool SwitchedAccount { get; private init; }

    public static AuthResult Ok(AuthUser user) => new() { IsSuccess = true, User = user };

    /// <summary>Signed in to the account that already had the number (see <see cref="SwitchedAccount"/>).</summary>
    public static AuthResult SwitchedTo(AuthUser user) => new() { IsSuccess = true, User = user, SwitchedAccount = true };

    public static AuthResult Fail(AuthError error, string? detail = null) => new() { Error = error, Detail = detail };
}
