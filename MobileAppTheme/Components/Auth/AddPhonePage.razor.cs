using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Auth;

/// <summary>
/// <c>/verify/phone</c>: adds a verified mobile number to an account that has none (a Google
/// sign-in), by SMS code, keeping the same account. <see cref="SignInGate"/> sends people here
/// from pages that need a number; afterwards they go back to <c>then</c>.
/// </summary>
public partial class AddPhonePage : ComponentBase
{
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IAppAccount Account { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "then")] public string? Then { get; set; }

    [SupplyParameterFromQuery(Name = "back")] public string? Back { get; set; }

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

    private void HandleLinked(AuthResult result)
    {
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
