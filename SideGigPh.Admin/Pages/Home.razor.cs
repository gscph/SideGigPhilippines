using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Pages;

/// <summary>
/// The site's one page: sign in with Google, then <c>GET admin/me</c> decides. An operator (a uid in the API's
/// <c>AdminUids</c> setting) sees the badge requests; anyone else sees their uid, to send to whoever sets that list.
/// </summary>
public partial class Home
{
    private enum HomeState { Loading, NotConfigured, SignedOut, NotOperator, Operator }

    [Inject] private IAdminSignIn SignIn { get; set; } = default!;
    [Inject] private AdminApi Api { get; set; } = default!;

    private HomeState _state = HomeState.Loading;
    private AdminUser? _user;
    private bool _busy;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        if (!SignIn.IsConfigured)
        {
            _state = HomeState.NotConfigured;
            return;
        }

        try
        {
            _user = await SignIn.StartAsync();
        }
        catch (Exception ex)
        {
            _error = $"Couldn't start the sign-in ({ex.Message}).";
        }

        if (_user is null)
        {
            _state = HomeState.SignedOut;
            return;
        }

        await CheckOperatorAsync();
    }

    private async Task SignInAsync()
    {
        if (_busy) return;
        _busy = true;
        _error = null;
        try
        {
            _user = await SignIn.SignInAsync();
            await CheckOperatorAsync();
        }
        catch (AdminSignInException ex)
        {
            _error = ex.Cancelled ? null : ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task CheckOperatorAsync()
    {
        _error = null;
        try
        {
            _state = await Api.IsOperatorAsync() ? HomeState.Operator : HomeState.NotOperator;
        }
        catch (AdminApiException ex)
        {
            _error = ex.Message;
            _state = HomeState.NotOperator;
        }
    }

    private async Task SignOutAsync()
    {
        try
        {
            await SignIn.SignOutAsync();
        }
        catch (Exception ex)
        {
            _error = $"Sign-out didn't finish ({ex.Message}).";
        }

        _user = null;
        _state = HomeState.SignedOut;
    }
}
