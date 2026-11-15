namespace MobileAppTheme.Auth;

/// <summary>
/// The <see cref="IIdentityProvider"/> registered until the app adds a real one (e.g.
/// <c>AddFirebaseAuth</c> from MobileAppTheme.Firebase). Nobody is signed in and every sign-in
/// reports <see cref="AuthError.NotAvailable"/>. <see cref="SignInGate"/> lets everyone through
/// when this is registered, so an app without auth still runs.
/// </summary>
public sealed class NoIdentityProvider : IIdentityProvider
{
    public AuthMethods Methods => AuthMethods.None;

    public Task<PhoneVerification> SignUpWithPhoneAsync(string phoneNumber) =>
        throw new NotSupportedException("No identity provider is registered.");

    public Task<AuthResult> VerifyPhoneOtpAsync(string verificationId, string userEnteredCode) => NotAvailable();

    public Task<AuthResult> LinkPhoneAsync(string verificationId, string userEnteredCode) => NotAvailable();

    public Task<AuthResult> SignInWithGoogleAsync() => NotAvailable();

    public Task<AuthResult> SignInWithAppleAsync() => NotAvailable();

    public Task<AuthResult> SetDisplayNameAsync(string displayName) => NotAvailable();

    public Task<AuthUser?> GetCurrentUserAsync() => Task.FromResult<AuthUser?>(null);

    public Task<string?> GetIdTokenAsync() => Task.FromResult<string?>(null);

    public Task SignOutAsync() => Task.CompletedTask;

    public Task DeleteAccountAsync() => Task.CompletedTask;

    private static Task<AuthResult> NotAvailable() => Task.FromResult(AuthResult.Fail(AuthError.NotAvailable));
}
