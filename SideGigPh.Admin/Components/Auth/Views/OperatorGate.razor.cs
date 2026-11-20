using Microsoft.AspNetCore.Components;
using SideGigPh.Admin.Services;

namespace SideGigPh.Admin.Components.Auth.Views;

/// <summary>
/// Everything before the site: a spinner, "not set up", Sign in with Google, or "not an operator" with the uid to add
/// to <c>AdminUids</c>. The layout shows it whenever <see cref="AuthService.State"/> isn't <see cref="OperatorState.Operator"/>.
/// It listens to <see cref="AuthService.Changed"/> itself: it has no parameters, so the layout re-rendering doesn't
/// re-render it, and it would stay on the spinner once the sign-in check finishes.
/// </summary>
public partial class OperatorGate : IDisposable
{
    [Inject] public required AuthService Auth { get; set; }

    private bool _busy;

    protected override void OnInitialized() => Auth.Changed += OnAuthChanged;

    private void OnAuthChanged() => InvokeAsync(StateHasChanged);

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

    public void Dispose() => Auth.Changed -= OnAuthChanged;
}
