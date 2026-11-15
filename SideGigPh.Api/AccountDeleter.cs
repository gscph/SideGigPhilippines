using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace SideGigPh.Api;

/// <summary>
/// Deletes a person's sign-in account at the provider (Delete account: App Store 5.1.1(v), Google Play). Behind a port
/// so the provider can change and tests can run without it.
/// </summary>
public interface IAccountDeleter
{
    /// <summary>Deletes the sign-in account. False when this server can't (no credentials): the app then deletes it itself.</summary>
    Task<bool> DeleteAsync(string uid, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="IAccountDeleter"/> on the Firebase Admin SDK, with the project's service account (the
/// <c>FirebaseServiceAccount</c> setting). Deleting on the server avoids the phone SDK's "sign in again first" rule.
/// </summary>
public sealed class FirebaseAccountDeleter(string serviceAccountJson, string projectId) : IAccountDeleter
{
    private readonly FirebaseAuth _auth = FirebaseAuth.GetAuth(FirebaseApp.Create(new AppOptions
    {
        Credential = CredentialFactory.FromJson<ServiceAccountCredential>(serviceAccountJson).ToGoogleCredential(),
        ProjectId = projectId,
    }, "accounts"));

    public async Task<bool> DeleteAsync(string uid, CancellationToken cancellationToken)
    {
        try
        {
            await _auth.DeleteUserAsync(uid, cancellationToken);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // Already gone: that is what was asked.
        }

        return true;
    }
}

/// <summary>Without the service account (a PC): the database is cleaned and the app deletes the sign-in account itself.</summary>
public sealed class NoAccountDeleter : IAccountDeleter
{
    public Task<bool> DeleteAsync(string uid, CancellationToken cancellationToken) => Task.FromResult(false);
}
