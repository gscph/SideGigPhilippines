namespace MobileAppTheme.Auth;

/// <summary>
/// Provider-agnostic representation of an authenticated user. The UI and routing
/// depend on this, never on a provider (e.g. Firebase) type.
/// </summary>
public sealed class AuthUser
{
    public required string Uid { get; init; }

    /// <summary>The number the user verified with an SMS code (E.164), or null if they haven't.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Their full name: from Google, or typed once on <c>NamePage</c> after a phone sign-in.</summary>
    public string? DisplayName { get; init; }

    public string? Email { get; init; }

    public string? PhotoUrl { get; init; }

    public bool HasVerifiedPhone => !string.IsNullOrEmpty(PhoneNumber);

    public bool HasName => !string.IsNullOrWhiteSpace(DisplayName);
}
