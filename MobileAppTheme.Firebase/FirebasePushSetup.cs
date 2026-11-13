using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.LifecycleEvents;
using MobileAppTheme.Services;
using Plugin.Firebase.CloudMessaging;
#if ANDROID
using Android.App;
using Android.Content;
#endif

namespace MobileAppTheme.Firebase;

/// <summary>
/// One call in <c>MauiProgram</c> turns on notifications through Firebase Cloud Messaging:
/// <c>builder.AddFirebasePush(channels)</c>. It starts the SDK during platform startup, creates the Android
/// notification channels, hands notification taps to the plugin, and registers
/// <see cref="FirebasePushNotifications"/> as the <see cref="IPushNotifications"/>.
/// </summary>
public static class FirebasePushSetup
{
    /// <param name="channels">The Android channels the server sends to (the first one is also the default).</param>
    public static MauiAppBuilder AddFirebasePush(this MauiAppBuilder builder, params PushChannel[] channels)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnCreate((activity, _) =>
                {
                    FirebaseCore.Start(activity);
                    FirebaseCore.TryRun("notification channels", () => CreateChannels(channels));
                    // The app was opened by tapping a notification while it was closed.
                    FirebaseCore.TryRun("notification tap", () => FirebaseCloudMessagingImplementation.OnNewIntent(activity.Intent));
                })
                .OnNewIntent((_, intent) =>
                    FirebaseCore.TryRun("notification tap", () => FirebaseCloudMessagingImplementation.OnNewIntent(intent))));
#elif IOS
            events.AddiOS(ios => ios.WillFinishLaunching((_, __) =>
            {
                FirebaseCore.Start();
                FirebaseCore.TryRun("notifications", FirebaseCloudMessagingImplementation.Initialize);
                return false;
            }));
#endif
        });

        // Replace, not TryAdd: the skeleton registers NoPushNotifications as the default.
        builder.Services.Replace(ServiceDescriptor.Singleton<IPushNotifications, FirebasePushNotifications>());
        return builder;
    }

#if ANDROID
    /// <summary>Android 8+ shows a notification only on a channel the app made. Quiet channels make no sound and no pop-up but still put the dot on the icon.</summary>
    private static void CreateChannels(IReadOnlyList<PushChannel> channels)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)
            || Android.App.Application.Context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
        {
            return;
        }

        foreach (var wanted in channels)
        {
            var channel = new NotificationChannel(wanted.Id, wanted.Name, wanted.Alert ? NotificationImportance.High : NotificationImportance.Low);
            channel.SetShowBadge(true);
            manager.CreateNotificationChannel(channel);
        }

        if (channels.Count > 0)
        {
            FirebaseCloudMessagingImplementation.ChannelId = channels[0].Id;
        }
    }
#endif
}
