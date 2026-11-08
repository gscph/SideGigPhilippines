using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Auth;

/// <summary>Reusable "Continue with Google" button. Emits the <see cref="AuthResult"/> to the parent.</summary>
public partial class GoogleSignInButton : ComponentBase
{
    [Inject] private IIdentityProvider Identity { get; set; } = default!;

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
