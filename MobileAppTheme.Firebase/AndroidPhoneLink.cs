#if ANDROID
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Firebase.Auth;
using Plugin.Firebase.Auth;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Adding an SMS-verified number to the signed-in account, on Android, through Firebase's own SDK instead of the
/// plugin. When the number already belongs to another account, Firebase answers with an "updated credential" that signs
/// in to that account without a second code; the plugin (3.1.x) throws that answer away (it rewraps the exception), which
/// left the person typing a second code for the number they had just proved. So the link is made here, with the SMS
/// session id the plugin keeps from <see cref="IFirebaseAuth.VerifyPhoneNumberAsync"/>.
/// </summary>
internal static class AndroidPhoneLink
{
    private const string PluginImplementation = "Plugin.Firebase.Auth.FirebaseAuthImplementation";
    private const string PluginPhoneAuth = "Plugin.Firebase.Auth.Platforms.Android.PhoneNumber.PhoneNumberAuth";
    private const string PluginAssembly = "Plugin.Firebase.Auth";

    /// <summary>What happened to the number.</summary>
    public enum Outcome
    {
        /// <summary>The number is now on the signed-in account (same uid).</summary>
        Linked,

        /// <summary>The number was on another account, which is now signed in instead.</summary>
        SwitchedAccount,
    }

    /// <summary>
    /// The SMS session id the plugin got for the last code sent, or null when it can't be read (a plugin update moved
    /// it): the caller then links through the plugin as before.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, PluginImplementation, PluginAssembly)]
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, PluginPhoneAuth, PluginAssembly)]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The fields are kept by the DynamicDependency attributes above.")]
    public static string? PendingVerificationId(IFirebaseAuth plugin)
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        var phoneAuth = plugin.GetType().GetField("_phoneNumberAuth", Private)?.GetValue(plugin);
        return phoneAuth?.GetType().GetField("_verificationId", Private)?.GetValue(phoneAuth) as string;
    }

    /// <summary>
    /// Adds the number to the signed-in account; when another account has it, signs in to that account with the same
    /// code. Throws Firebase's own exception otherwise (a wrong or expired code, no signal), for the caller to map.
    /// </summary>
    public static async Task<Outcome> LinkOrSignInAsync(string verificationId, string code)
    {
        var auth = FirebaseAuth.Instance;
        var user = auth.CurrentUser ?? throw new InvalidOperationException("Nobody is signed in.");
        var credential = PhoneAuthProvider.GetCredential(verificationId, code);
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
