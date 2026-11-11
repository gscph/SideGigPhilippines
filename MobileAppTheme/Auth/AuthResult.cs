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

    public static AuthResult Ok(AuthUser user) => new() { IsSuccess = true, User = user };

    public static AuthResult Fail(AuthError error, string? detail = null) => new() { Error = error, Detail = detail };
}
