using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MobileAppTheme.Branding;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Auth;

/// <summary>
/// The sign-in sheet (<c>/verify</c>, Airbnb's "Log in or sign up"): a welcome line with the brand,
/// then the methods the registered <see cref="IIdentityProvider"/> offers, phone code first for everyone (it also
/// verifies the number), "or", Apple (iOS) and Google below, then the agreement to the terms and privacy policy
/// (<see cref="LegalConsent"/>). After the code is sent the sheet becomes "Confirm your
/// number" with a back arrow to the number. After sign-in, or if the user is already signed in, it asks for their name if the account
/// has none (<see cref="NamePage"/>), else goes where the app's <see cref="IAuthLanding"/> says. With no provider
/// registered it explains that sign-in isn't available.
/// </summary>
public partial class AuthPage : ComponentBase
{
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private IAuthLanding Landing { get; set; } = default!;
    [Inject] private AppBrand Brand { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string? _error;
    private PhoneSignIn? _phone;

    // The number the SMS code went to; while set, the sheet is "Confirm your number" with a back arrow.
    private string? _sentTo;

    // Only the code step has somewhere to go back to (the number).
    private EventCallback BackHandler => _sentTo is null
        ? default
        : EventCallback.Factory.Create(this, () => _phone?.UseOtherNumberAsync() ?? Task.CompletedTask);

    private bool HasPhone => Identity.Methods.HasFlag(AuthMethods.Phone);

    private bool HasGoogle => Identity.Methods.HasFlag(AuthMethods.Google);

    private bool HasApple => Identity.Methods.HasFlag(AuthMethods.Apple);

    private bool HasOtherMethod => HasGoogle || HasApple;

    protected override async Task OnInitializedAsync()
    {
        if (Identity.Methods == AuthMethods.None)
        {
            _error = T.Describe(AuthError.NotAvailable);
            return;
        }

        if (await Identity.GetCurrentUserAsync() is { } existing)
        {
            await GoOnAsync(existing);
        }
    }

    private async Task HandleAuthenticatedAsync(AuthResult result)
    {
        if (result is { IsSuccess: true, User: { } user })
        {
            _error = null;
            await GoOnAsync(user);
            return;
        }

        _error = T.Describe(result.Error);
    }

    private void SentToChanged(string? number)
    {
        _sentTo = number;
        _error = null;
    }

    // A phone sign-in has no name yet: ask for it once, then the app's landing.
    private async Task GoOnAsync(AuthUser user) =>
        Nav.NavigateTo(user.HasName ? await Landing.GetDestinationAsync(user) : AuthRoutes.NamePath, replace: true);
}
