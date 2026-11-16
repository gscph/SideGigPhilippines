namespace MobileAppTheme.Auth;

/// <summary>
/// What a page checks before it shows anything: is someone signed in, do we have their name and,
/// for pages that need it, a verified phone number? Returns the sign-in step to show first, or
/// null to carry on. With no provider registered (<see cref="NoIdentityProvider"/>) everyone is
/// let through, so an app without auth still runs.
/// </summary>
public sealed class SignInGate(IIdentityProvider identity)
{
    /// <param name="returnPath">Where to come back to once the missing step is done.</param>
    /// <param name="requirePhone">The page needs a verified phone number (e.g. contacting people).</param>
    /// <param name="backPath">Where the add-number step's Back goes (<see cref="AuthRoutes.AddPhone"/>).</param>
    /// <param name="canSkipPhone">The add-number step offers "Do this later" (a later page asks again).</param>
    /// <param name="laterPath">Where the add-number step's "Do this later" goes when the page itself needs the number.</param>
    public async Task<string?> RedirectAsync(
        string returnPath, bool requirePhone = false, string? backPath = null, bool canSkipPhone = false, string? laterPath = null)
    {
        if (identity.Methods == AuthMethods.None)
        {
            return null;
        }

        var user = await identity.GetCurrentUserAsync();
        if (user is null)
        {
            return AuthRoutes.SignIn;
        }

        if (!user.HasName)
        {
            return AuthRoutes.Name(returnPath);
        }

        return requirePhone && !user.HasVerifiedPhone ? AuthRoutes.AddPhone(returnPath, backPath, canSkipPhone, laterPath) : null;
    }
}
