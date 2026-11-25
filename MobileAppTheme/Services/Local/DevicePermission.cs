using Microsoft.Maui.ApplicationModel;

namespace MobileAppTheme.Services.Local;

/// <summary>The on-device adapters' one way to get a phone permission: asked on the main thread, only when not yet given.</summary>
internal static class DevicePermission
{
    /// <summary>The permission's status, <see cref="PermissionStatus.Granted"/> at once when it was already given.</summary>
    public static Task<PermissionStatus> AskAsync<TPermission>() where TPermission : Permissions.BasePermission, new() =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var current = await Permissions.CheckStatusAsync<TPermission>();
            return current == PermissionStatus.Granted ? current : await Permissions.RequestAsync<TPermission>();
        });
}
