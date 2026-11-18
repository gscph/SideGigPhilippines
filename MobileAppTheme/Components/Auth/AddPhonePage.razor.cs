using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Auth;

/// <summary>
/// <c>/verify/phone</c>: adds a verified mobile number to an account that has none (a Google
/// sign-in), by SMS code, keeping the same account. <see cref="SignInGate"/> sends people here
/// from pages that need a number; afterwards they go back to <c>then</c>. With <c>skip=true</c> (an early ask that a later
/// page repeats) "Do this later" goes on to <c>then</c> without a number, so an SMS that never arrives is no dead end; a
/// page that needs the number passes <c>later</c> instead (e.g. home), so someone out of codes for today can still leave.
/// A number that is another account's signs in to that account with the same code
/// (<see cref="AuthResult.SwitchedAccount"/>), never a second one, and goes where a sign-in goes.
/// </summary>
public partial class AddPhonePage : ComponentBase
{
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IAppAccount Account { get; set; } = default!;
    [Inject] private IAuthLanding Landing { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "then")] public string? Then { get; set; }

    [SupplyParameterFromQuery(Name = "back")] public string? Back { get; set; }

    [SupplyParameterFromQuery(Name = "skip")] public bool Skip { get; set; }

    [SupplyParameterFromQuery(Name = "later")] public string? Later { get; set; }

    private string? _error;
    private PhoneSignIn? _phone;
    private string? _sentTo;
    private bool _phoneInUse;
    private bool _busy;

    private string Destination => AuthRoutes.SafeReturn(Then) ?? "/";

    protected override async Task OnInitializedAsync()
    {
        var user = await Identity.GetCurrentUserAsync();
        if (user is null)
        {
            Nav.NavigateTo(AuthRoutes.SignIn, replace: true);
        }
        else if (user.HasVerifiedPhone)
        {
            Nav.NavigateTo(Destination, replace: true);
        }
    }

    private async Task HandleLinkedAsync(AuthResult result)
    {
        if (result is { SwitchedAccount: true, User: { } other })
        {
            // The code proved the number, which is another account's: that account is signed in now, so go where a
            // sign-in goes (Account keeps this phone's answers).
            await Account.AccountSwitchedAsync();
            Nav.NavigateTo(other.HasName ? await Landing.GetDestinationAsync(other) : AuthRoutes.NamePath, replace: true);
            return;
        }

        if (result.IsSuccess)
        {
            Nav.NavigateTo(Destination, replace: true);
            return;
        }

        _error = T.Describe(result.Error);
        _phoneInUse = result.Error == AuthError.PhoneInUse;
    }

    /// <summary>
    /// The number belongs to another account (often the person's own earlier phone sign-in): sign out of this one and
    /// sign in with the number. Without this the only offer was a message, and nothing in the app signs out.
    /// </summary>
    private async Task SignInWithNumberAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await Account.SignOutAsync();
            Nav.NavigateTo(AuthRoutes.SignIn, replace: true);
        }
        catch (Exception)
        {
            _error = T.Get(CommonCopy.AuthSignOutFailed);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Where "Do this later" goes: <c>later</c>, else <c>then</c> when skipping is allowed; null hides it.</summary>
    private string? LaterDestination => AuthRoutes.SafeReturn(Later) ?? (Skip ? Destination : null);

    private void DoLater() => Nav.NavigateTo(LaterDestination ?? Destination, replace: true);

    private void SentToChanged(string? number)
    {
        _sentTo = number;
        _error = null;
        _phoneInUse = false;
    }

    // From the code step, back to the number; from the number, out (to `back`, else home).
    private async Task GoBackAsync()
    {
        if (_sentTo is not null && _phone is not null)
        {
            await _phone.UseOtherNumberAsync();
            return;
        }

        Nav.NavigateTo(AuthRoutes.SafeReturn(Back) ?? "/");
    }
}
