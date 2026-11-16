using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MobileAppTheme.Services;

namespace MobileAppTheme.Components.Layout;

/// <summary>
/// <see cref="MainLayout"/>'s error boundary. A page that throws (while rendering or in a tap's handler) would
/// otherwise stop the whole UI with the error only in the WebView's console; here it is logged as an error through
/// <see cref="ILogger"/>, so the app's error reports carry it, and the layout shows its recover screen instead.
/// A lost connection (<see cref="NetworkErrors.IsNetwork"/>) is <see cref="LostConnection"/>: the layout says
/// "no internet, try again" rather than "something broke".
/// </summary>
public sealed class AppErrorBoundary : ErrorBoundary
{
    [Microsoft.AspNetCore.Components.Inject] private ILogger<AppErrorBoundary> Logger { get; set; } = default!;

    /// <summary>The page failed because the connection did, not because of a bug.</summary>
    public bool LostConnection { get; private set; }

    protected override Task OnErrorAsync(Exception exception)
    {
        LostConnection = NetworkErrors.IsNetwork(exception);
        if (LostConnection)
        {
            Logger.LogError(exception, "A screen lost its connection: {Error}", exception.GetBaseException().Message);
        }
        else
        {
            Logger.LogError(exception, "A screen broke: {Error}", exception.GetBaseException().Message);
        }

        return Task.CompletedTask;
    }
}
