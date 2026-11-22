#if IOS
using Firebase.Auth;
using Foundation;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Adding an SMS-verified number to the signed-in account, on iOS, through Firebase's own SDK. When the number already
/// belongs to another account, Firebase answers "credential already in use" (17025) with an updated credential that
/// signs in to that account without a second code; the plugin (3.1.x) threw it away, so the person was sent to sign out
/// and type a second code for the number they had just proved. Same as <see cref="AndroidPhoneLink"/>.
/// </summary>
internal static class IosPhoneLink
{
    private const long CredentialAlreadyInUse = 17025;

    /// <summary>
    /// Adds the number to the signed-in account; when another account has it, signs in to that account with the same
    /// code. Throws Firebase's own exception otherwise (a wrong or expired code, no signal), for the caller to map.
    /// </summary>
    public static async Task<PhoneLinkOutcome> LinkOrSignInAsync(PhoneAuthCredential credential)
    {
        var auth = global::Firebase.Auth.Auth.DefaultInstance ?? throw new InvalidOperationException("Firebase is not running.");
        var user = auth.CurrentUser ?? throw new InvalidOperationException("Nobody is signed in.");
        try
        {
            await user.LinkAsync(credential);
            return PhoneLinkOutcome.Linked;
        }
        catch (NSErrorException collision) when (collision.Error.Code == CredentialAlreadyInUse
            && collision.Error.UserInfo?[global::Firebase.Auth.Auth.ErrorUserInfoUpdatedCredentialKey] is AuthCredential proven)
        {
            await auth.SignInWithCredentialAsync(proven);
            return PhoneLinkOutcome.SwitchedAccount;
        }
    }
}
#endif
