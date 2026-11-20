using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Components.Auth.Views;

/// <summary>
/// Everything before the site: a spinner, "not set up", Sign in with Google, or "not an operator" with the uid to add
/// to <c>AdminUids</c>. The layout shows it whenever <see cref="AuthService.State"/> isn't <see cref="OperatorState.Operator"/>.
/// </summary>
public partial class OperatorGate
{
    [Inject] public required AuthService Auth { get; set; }

    private bool _busy;

    private Task SignInAsync() => RunAsync(Auth.SignInAsync);

    private Task RetryAsync() => RunAsync(Auth.CheckOperatorAsync);

    private Task SignOutAsync() => RunAsync(Auth.SignOutAsync);

    private async Task RunAsync(Func<Task> action)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await action();
        }
        finally
        {
            _busy = false;
        }
    }
}
