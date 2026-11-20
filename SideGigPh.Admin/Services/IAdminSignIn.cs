namespace SideGigPh.Admin.Services;

/// <summary>
/// The operator's sign-in, provider-neutral (the adapter is <see cref="FirebaseWebSignIn"/>). Pages and
/// <see cref="AdminApi"/> use only this, never the provider's SDK.
/// </summary>
public interface IAdminSignIn
{
    /// <summary>False until the provider's web settings are filled in (<c>wwwroot/appsettings.json</c>).</summary>
    bool IsConfigured { get; }

    /// <summary>Starts the provider and answers with whoever is still signed in from last time, or null.</summary>
    Task<AdminUser?> StartAsync();

    /// <summary>Signs in with Google. Throws <see cref="AdminSignInException"/> when it didn't happen.</summary>
    Task<AdminUser> SignInAsync();

    Task SignOutAsync();

    /// <summary>A current ID token for the API, or null when nobody is signed in.</summary>
    Task<string?> IdTokenAsync();
}

/// <summary>A sign-in that didn't happen; <see cref="Cancelled"/> when the person closed the pop-up.</summary>
public sealed class AdminSignInException(string message, bool cancelled, Exception? inner = null) : Exception(message, inner)
{
    public bool Cancelled { get; } = cancelled;
}
