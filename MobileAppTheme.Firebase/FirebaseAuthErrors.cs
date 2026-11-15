using System.Text.RegularExpressions;
using MobileAppTheme.Auth;
using Plugin.Firebase.Core.Exceptions;

namespace MobileAppTheme.Firebase;

/// <summary>
/// Turns Firebase / Google Play errors into the skeleton's <see cref="AuthError"/>. Google sign-in
/// failures carry only a Play-services status code in the message (<c>"7:"</c>, <c>"10: …"</c>), so
/// it is parsed from the text: no dependency on the Play-services binding.
/// </summary>
public static partial class FirebaseAuthErrors
{
    /// <summary>Google Play status codes we map; anything else is <see cref="AuthError.Unknown"/>.</summary>
    private static readonly IReadOnlyDictionary<int, (AuthError Error, string Name)> GoogleStatus =
        new Dictionary<int, (AuthError, string)>
        {
            [7] = (AuthError.Network, "NETWORK_ERROR"),
            [10] = (AuthError.Misconfigured, "DEVELOPER_ERROR: this build's SHA-1 isn't registered in Firebase, or google-services.json is stale"),
            [12500] = (AuthError.Misconfigured, "SIGN_IN_FAILED: check the OAuth client / SHA-1 in Firebase"),
            [12501] = (AuthError.Cancelled, "SIGN_IN_CANCELLED"),
            [12502] = (AuthError.Cancelled, "SIGN_IN_CURRENTLY_IN_PROGRESS"),
        };

    /// <summary>The error for a failed Google sign-in; <paramref name="status"/> is a readable status for logs.</summary>
    public static AuthError FromGoogle(Exception ex, out string status)
    {
        if (StatusCode().Match(ex.Message) is { Success: true } match
            && int.TryParse(match.Groups[1].Value, out var code))
        {
            if (GoogleStatus.TryGetValue(code, out var known))
            {
                status = $"{code} {known.Name}";
                return known.Error;
            }

            status = $"{code} (unmapped Google Play status)";
            return AuthError.Unknown;
        }

        status = ex.Message;
        return ex is TaskCanceledException or OperationCanceledException ? AuthError.Cancelled : AuthError.Unknown;
    }

    /// <summary>The error for a failed SMS-code sign-in, or adding a number to the account.</summary>
    /// <summary>Sign in with Apple: the person closing Apple's sheet is a cancel, not an error to show.</summary>
    public static AuthError FromApple(Exception ex) =>
        ex is TaskCanceledException or OperationCanceledException
        || ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase) ? AuthError.Cancelled
        : ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ? AuthError.Network
        : AuthError.Unknown;

    public static AuthError FromPhone(Exception ex) => ex switch
    {
        // Linking a number another account has: "This credential is already associated with a
        // different user account." The plugin has no reason code for it, so the text is checked.
        _ when ex.Message.Contains("already associated", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("already in use", StringComparison.OrdinalIgnoreCase) => AuthError.PhoneInUse,
        FirebaseAuthException { Reason: FIRAuthError.InvalidCredential } => AuthError.InvalidCode,
        _ when ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) => AuthError.Network,
        _ when ex.Message.Contains("too many", StringComparison.OrdinalIgnoreCase) => AuthError.TooManyAttempts,
        _ => AuthError.Unknown,
    };

    [GeneratedRegex(@"^\s*(\d+)\s*:")]
    private static partial Regex StatusCode();
}
