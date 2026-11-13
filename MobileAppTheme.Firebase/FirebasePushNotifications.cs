using Microsoft.Extensions.Logging;
using MobileAppTheme.Services;
using Plugin.Firebase.CloudMessaging;
#if ANDROID
using Android.App;
using Android.Content;
#elif IOS
using UIKit;
using UserNotifications;
#endif

namespace MobileAppTheme.Firebase;

/// <summary>
/// Firebase Cloud Messaging behind the skeleton's <see cref="IPushNotifications"/> (Plugin.Firebase.CloudMessaging 3.1.x),
/// plus the app icon badge. Registered by <see cref="FirebasePushSetup.AddFirebasePush"/>. Failures are logged in full
/// and come back as "no notifications" (false / null), never as a crash.
///
/// Android: the server's notification shows on one of the channels made at launch; the launcher puts a dot on the icon
/// while one is in the tray, and <see cref="ShowOnly"/> clears the ones that were read. iOS: the server sets the badge
/// number; <see cref="ShowOnly"/> sets it again when the app knows better and clears read notifications.
/// </summary>
public sealed class FirebasePushNotifications : IPushNotifications
{
    private readonly ILogger<FirebasePushNotifications> _logger;
    private Action<IReadOnlyDictionary<string, string>>? _tapped;
    private IReadOnlyDictionary<string, string>? _missedTap;

    public FirebasePushNotifications(ILogger<FirebasePushNotifications> logger)
    {
        _logger = logger;
        if (!FirebaseCore.IsRunning)
        {
            return;
        }

        try
        {
            var messaging = CrossFirebaseCloudMessaging.Current;
            messaging.TokenChanged += (_, e) => TokenChanged?.Invoke(e.Token);
            messaging.NotificationReceived += (_, e) => Received?.Invoke(DataOf(e.Notification));
            messaging.NotificationTapped += (_, e) => OnTapped(DataOf(e.Notification));
            messaging.Error += (_, e) => _logger.LogWarning("Firebase Cloud Messaging: {Message}", e.Message);
            Platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase Cloud Messaging couldn't start; this phone gets no notifications.");
        }
    }

    public string? Platform { get; }

    public event Action<string>? TokenChanged;

    public event Action<IReadOnlyDictionary<string, string>>? Received;

    public event Action<IReadOnlyDictionary<string, string>>? Tapped
    {
        add
        {
            _tapped += value;
            if (_missedTap is { } missed && value is not null)
            {
                _missedTap = null;
                value(missed);
            }
        }
        remove => _tapped -= value;
    }

    public async Task<bool> IsAllowedAsync()
    {
        if (Platform is null)
        {
            return false;
        }

        try
        {
#if ANDROID
            return AndroidX.Core.App.NotificationManagerCompat.From(Android.App.Application.Context)?.AreNotificationsEnabled() ?? false;
#elif IOS
            var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
            return settings.AuthorizationStatus is UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional;
#else
            return await Task.FromResult(false);
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't read whether notifications are allowed.");
            return false;
        }
    }

    public async Task<bool> RequestPermissionAsync()
    {
        if (Platform is null)
        {
            return false;
        }

        try
        {
#if ANDROID
            // Android 13+ asks; older versions allow notifications unless the person turned them off.
            await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.PostNotifications>);
#endif
            // iOS: asks for alerts, badges and sounds, then registers with Apple's push service.
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't ask to allow notifications.");
        }

        return await IsAllowedAsync();
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!await IsAllowedAsync())
        {
            return null;
        }

        try
        {
#if IOS
            // Re-registers with Apple's push service at each launch (no dialog once the person has answered).
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
#endif
            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't get this phone's notification token.");
            return null;
        }
    }

    public void ShowOnly(IReadOnlyCollection<string> unreadTags)
    {
        if (Platform is null)
        {
            return;
        }

        try
        {
#if ANDROID
            if (Android.App.Application.Context.GetSystemService(Context.NotificationService) is NotificationManager manager)
            {
                foreach (var shown in manager.GetActiveNotifications() ?? [])
                {
                    if (shown.Tag is { } tag && !unreadTags.Contains(tag))
                    {
                        manager.Cancel(tag, shown.Id);
                    }
                }
            }
#elif IOS
            var center = UNUserNotificationCenter.Current;
            center.GetDeliveredNotifications(shown =>
            {
                var read = shown.Select(n => n.Request.Identifier).Where(id => !unreadTags.Contains(id)).ToArray();
                if (read.Length > 0)
                {
                    center.RemoveDeliveredNotifications(read);
                }
            });
            if (OperatingSystem.IsIOSVersionAtLeast(16))
            {
                center.SetBadgeCount(unreadTags.Count, null);
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => UIApplication.SharedApplication.ApplicationIconBadgeNumber = unreadTags.Count);
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't update the notifications and badge.");
        }
    }

    private void OnTapped(IReadOnlyDictionary<string, string> data)
    {
        if (_tapped is { } tapped)
        {
            tapped(data);
        }
        else
        {
            _missedTap = data; // the app opened from the tap and nobody listens yet
        }
    }

    private static IReadOnlyDictionary<string, string> DataOf(FCMNotification? notification) =>
        notification?.Data is { } data ? new Dictionary<string, string>(data) : new Dictionary<string, string>();
}
