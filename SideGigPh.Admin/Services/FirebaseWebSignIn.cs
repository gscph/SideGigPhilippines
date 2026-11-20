using Microsoft.JSInterop;

using SideGigPh.Admin.Options;

namespace SideGigPh.Admin.Services;

/// <summary>
/// <see cref="IAdminSignIn"/> with the Firebase JS SDK (<c>wwwroot/js/firebase-auth.js</c>, Google pop-up). The only
/// C# that knows Firebase.
/// </summary>
public sealed class FirebaseWebSignIn(IJSRuntime js, FirebaseWebOptions options) : IAdminSignIn, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public bool IsConfigured => options.IsComplete;

    public async Task<AdminUser?> StartAsync()
    {
        var module = await ModuleAsync();
        return await module.InvokeAsync<AdminUser?>("start", new
        {
            apiKey = options.ApiKey,
            authDomain = options.AuthDomain,
            projectId = options.ProjectId,
            appId = options.AppId,
        });
    }

    public async Task<AdminUser> SignInAsync()
    {
        try
        {
            return await (await ModuleAsync()).InvokeAsync<AdminUser?>("signIn")
                ?? throw new AdminSignInException("Google didn't return an account.", cancelled: false);
        }
        catch (JSException ex)
        {
            var cancelled = ex.Message.Contains("popup-closed-by-user", StringComparison.Ordinal)
                || ex.Message.Contains("cancelled-popup-request", StringComparison.Ordinal);
            var message = ex.Message.Contains("unauthorized-domain", StringComparison.Ordinal)
                ? "This site's address isn't in Firebase's Authorized domains yet."
                : ex.Message.Contains("popup-blocked", StringComparison.Ordinal)
                    ? "The browser blocked the sign-in pop-up. Allow pop-ups for this site and try again."
                    : "Sign-in didn't work. Try again.";
            throw new AdminSignInException(message, cancelled, ex);
        }
    }

    public async Task SignOutAsync() => await (await ModuleAsync()).InvokeVoidAsync("signOut");

    public async Task<string?> IdTokenAsync() => await (await ModuleAsync()).InvokeAsync<string?>("idToken");

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/firebase-auth.js");

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
