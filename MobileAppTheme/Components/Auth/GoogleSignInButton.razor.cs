using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Auth;

/// <summary>"Continue with Google" through the registered <see cref="IIdentityProvider"/>. Reports the <see cref="AuthResult"/> to the parent.</summary>
public partial class GoogleSignInButton : ComponentBase
{
    [Inject] private IIdentityProvider Identity { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public EventCallback<AuthResult> OnAuthenticated { get; set; }

    private bool _busy;

    private async Task SignInAsync()
    {
        _busy = true;
        try
        {
            var result = await Identity.SignInWithGoogleAsync();
            await OnAuthenticated.InvokeAsync(result);
        }
        finally
        {
            _busy = false;
        }
    }
}
