using Microsoft.AspNetCore.Components;
using MobileAppTheme.Components.Auth;
using MobileAppTheme.Services;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// The worker interview's number question: the skeleton's <see cref="PhoneSignIn"/> adds an SMS-verified number to the
/// signed-in account (a Google or Apple sign-in; a phone sign-in already has one and never sees this). "Do this later"
/// always shows, so a code that never comes is no dead end: Publish asks again. A number that belongs to another
/// account (often their own earlier phone sign-in) signs in to that account with the same code and carries on where a
/// sign-in goes, keeping the answers; only where the provider can't do that does it offer "Sign in with this number".
/// </summary>
public partial class NumberField : ComponentBase
{
    [Inject] private IAppAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IAuthLanding Landing { get; set; } = default!;

    /// <summary>The number was verified and added to the account.</summary>
    [Parameter] public EventCallback OnLinked { get; set; }

    /// <summary>"Do this later".</summary>
    [Parameter] public EventCallback OnLater { get; set; }

    /// <summary>The number a code went to, null again back on the number box (the question's title follows it).</summary>
    [Parameter] public EventCallback<string?> SentToChanged { get; set; }

    private PhoneSignIn? _phone;
    private string? _sentTo;
    private string? _error;
    private bool _phoneInUse;
    private bool _busy;

    /// <summary>Back on the code box returns to the number box (true); on the number box it is the question's own Back (false).</summary>
    public async Task<bool> BackAsync()
    {
        if (_sentTo is null || _phone is null)
        {
            return false;
        }

        await _phone.UseOtherNumberAsync();
        return true;
    }

    private async Task HandleLinkedAsync(AuthResult result)
    {
        if (result is { SwitchedAccount: true, User: { } other })
        {
            // The code proved the number, which is another account's: that account is signed in now. Where a sign-in
            // goes (the launch page resumes the interview, which no longer asks for the number).
            await Account.AccountSwitchedAsync();
            Nav.NavigateTo(other.HasName ? await Landing.GetDestinationAsync(other) : AuthRoutes.NamePath, replace: true);
            return;
        }

        if (result.IsSuccess)
        {
            await OnLinked.InvokeAsync();
            return;
        }

        _error = T.Describe(result.Error);
        _phoneInUse = result.Error == AuthError.PhoneInUse;
    }

    private async Task SentToChangedAsync(string? number)
    {
        _sentTo = number;
        _error = null;
        _phoneInUse = false;
        await SentToChanged.InvokeAsync(number);
    }

    /// <summary>The number belongs to another account (often their own earlier phone sign-in): sign out and sign in with it.</summary>
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
}
