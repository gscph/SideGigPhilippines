namespace SideGigPh.Maui.Models;

/// <summary>Outcome of an authentication attempt.</summary>
public sealed class AuthResult
{
    public bool IsSuccess { get; private init; }
    public AuthUser? User { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static AuthResult Ok(AuthUser user) => new() { IsSuccess = true, User = user };

    public static AuthResult Fail(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
