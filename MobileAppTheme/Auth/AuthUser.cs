namespace MobileAppTheme.Auth;

/// <summary>
/// Provider-agnostic representation of an authenticated user. The UI and routing
/// depend on this, never on a provider (e.g. Firebase) type.
/// </summary>
public sealed class AuthUser
{
    public required string Uid { get; init; }
    public string? PhoneNumber { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? PhotoUrl { get; init; }
}
