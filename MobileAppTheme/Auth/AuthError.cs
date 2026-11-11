namespace MobileAppTheme.Auth;

/// <summary>
/// Why sign-in failed, in terms every provider can map to. Each adapter translates its own error
/// codes into these, so the UI and its messages never change when the provider does.
/// </summary>
public enum AuthError
{
    None,

    /// <summary>The user closed the account picker or backed out. Show nothing.</summary>
    Cancelled,

    /// <summary>No connection, or the provider couldn't be reached.</summary>
    Network,

    /// <summary>The SMS code was wrong or expired.</summary>
    InvalidCode,

    /// <summary>The phone number was rejected.</summary>
    InvalidPhoneNumber,

    /// <summary>Too many attempts; try again later.</summary>
    TooManyAttempts,

    /// <summary>The app's provider setup is wrong (e.g. Google DEVELOPER_ERROR: unregistered SHA-1). A developer must fix it.</summary>
    Misconfigured,

    /// <summary>Sign-in isn't set up in this app (no provider registered).</summary>
    NotAvailable,

    Unknown,
}
