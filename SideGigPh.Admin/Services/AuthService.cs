using SideGigPh.Contracts;

namespace SideGigPh.Admin.Services;

/// <summary>Where the operator's session stands: the layout shows the site only in <see cref="Operator"/>.</summary>
public enum OperatorState { Loading, NotConfigured, SignedOut, NotOperator, Operator }

/// <summary>
/// The operator's session: Google sign-in (<see cref="IAdminSignIn"/>), then <c>GET admin/me</c> decides. Only a uid in
/// the API's <c>AdminUids</c> setting is an operator; the site keeps no list of its own. Raises <see cref="Changed"/>
/// on every move, so the layout swaps between the sign-in screens and the site.
/// </summary>
public sealed class AuthService(IAdminSignIn signIn, AdminApi api)
{
    private bool _started;

    public OperatorState State { get; private set; } = OperatorState.Loading;

    public AdminUser? User { get; private set; }

    /// <summary>Why the last step failed, for the screen; null when it didn't.</summary>
    public string? Error { get; private set; }

    public event Action? Changed;

    /// <summary>Picks up a sign-in kept from an earlier visit. Runs once; later calls do nothing.</summary>
    public async Task StartAsync()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        if (!signIn.IsConfigured)
        {
            Move(OperatorState.NotConfigured);
            return;
        }

        try
        {
            User = await signIn.StartAsync();
        }
        catch (Exception ex)
        {
            Error = $"Couldn't start the sign-in ({ex.Message}).";
        }

        if (User is null)
        {
            Move(OperatorState.SignedOut);
            return;
        }

        await CheckOperatorAsync();
    }

    public async Task SignInAsync()
    {
        Error = null;
        try
        {
            User = await signIn.SignInAsync();
            await CheckOperatorAsync();
        }
        catch (AdminSignInException ex)
        {
            Error = ex.Cancelled ? null : ex.Message;
            Move(OperatorState.SignedOut);
        }
    }

    public async Task CheckOperatorAsync()
    {
        Error = null;
        try
        {
            var status = await api.GetAsync("admin/me", ApiJson.Default.AdminStatusDto);
            Move(status.IsOperator ? OperatorState.Operator : OperatorState.NotOperator);
        }
        catch (AdminApiException ex)
        {
            Error = ex.Message;
            Move(OperatorState.NotOperator);
        }
    }

    public async Task SignOutAsync()
    {
        Error = null;
        try
        {
            await signIn.SignOutAsync();
        }
        catch (Exception ex)
        {
            Error = $"Sign-out didn't finish ({ex.Message}).";
        }

        User = null;
        Move(OperatorState.SignedOut);
    }

    private void Move(OperatorState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
