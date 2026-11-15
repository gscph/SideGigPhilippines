using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace MobileAppTheme.Components.Layout;

/// <summary>
/// <see cref="MainLayout"/>'s error boundary. A page that throws (while rendering or in a tap's handler) would
/// otherwise stop the whole UI with the error only in the WebView's console; here it is logged as an error through
/// <see cref="ILogger"/>, so the app's error reports carry it, and the layout shows its recover screen instead.
/// </summary>
public sealed class AppErrorBoundary : ErrorBoundary
{
    [Microsoft.AspNetCore.Components.Inject] private ILogger<AppErrorBoundary> Logger { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "A screen broke: {Error}", exception.GetBaseException().Message);
        return Task.CompletedTask;
    }
}
