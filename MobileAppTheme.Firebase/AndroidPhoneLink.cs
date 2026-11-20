#if ANDROID
using Firebase.Auth;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Adding an SMS-verified number to the signed-in account, on Android, through Firebase's own SDK instead of the
/// plugin. When the number already belongs to another account, Firebase answers with an "updated credential" that signs
/// in to that account without a second code; the plugin (3.1.x) throws that answer away (it rewraps the exception), which
/// left the person typing a second code for the number they had just proved. So the link is made here, with the
/// credential for the code sent by <see cref="AndroidPhoneVerification"/>.
/// </summary>
internal static class AndroidPhoneLink
{
    /// <summary>What happened to the number.</summary>
    public enum Outcome
    {
        /// <summary>The number is now on the signed-in account (same uid).</summary>
        Linked,

        /// <summary>The number was on another account, which is now signed in instead.</summary>
        SwitchedAccount,
    }

    /// <summary>
    /// Adds the number to the signed-in account; when another account has it, signs in to that account with the same
    /// code. Throws Firebase's own exception otherwise (a wrong or expired code, no signal), for the caller to map.
    /// </summary>
    public static async Task<Outcome> LinkOrSignInAsync(PhoneAuthCredential credential)
    {
        var auth = FirebaseAuth.Instance;
        var user = auth.CurrentUser ?? throw new InvalidOperationException("Nobody is signed in.");
        try
        {
            await user.LinkWithCredentialAsync(credential);
            return Outcome.Linked;
        }
        catch (FirebaseAuthUserCollisionException collision) when (collision.UpdatedCredential is { } proven)
        {
            await auth.SignInWithCredentialAsync(proven);
            return Outcome.SwitchedAccount;
        }
    }
}
#endif
