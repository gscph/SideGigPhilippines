using Microsoft.Extensions.Logging;
using AppTheme = SideGigPh.Maui.Components.Layout.AppTheme; // MAUI has an AppTheme enum too
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Maui.Services.Diagnostics;
using SideGigPh.Maui.Services.Local;
using SideGigPh.Maui.Services.Search;
using MobileAppTheme.Firebase;
using SideGigPh.Contracts;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui
{
    public static class MauiProgram
    {
        // Web OAuth client (client_type 3) from google-services.json, for Google sign-in on Android.
        private const string GoogleWebClientId = "742654871295-4slv8fpgpbrllq8aoe3vbiphv3e64q3f.apps.googleusercontent.com";

        // SideGigPh.Api (Azure Functions) and the public blob container its photos are readable from. Not secrets.
        internal const string ApiUrl = "https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/";
        private const string PhotoBaseUrl = "https://sidegigphbfb9.blob.core.windows.net/photos/";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();

            builder.Services.AddMauiBlazorWebView();

            // --- MobileAppTheme skeleton: MudBlazor, brand/theme, AppText, and the on-device
            // adapters for IContactLauncher, IPhotoPicker and IPhotoStore ---
            builder.Services.AddMobileAppTheme(AppTheme.Brand);
#if ANDROID
            // The selfie opens on the front camera (the skeleton's MAUI camera call can't choose the lens).
            builder.Services.AddSingleton<IPhotoPicker, SideGigPh.Maui.Platforms.Android.AndroidPhotoPicker>();
#endif

            // --- Ports & Adapters (see CLAUDE.md) ---
            // Sign-in provider. To switch, replace this line (and the project reference) with another
            // IIdentityProvider adapter; nothing else in the app knows which provider it is.
            builder.AddFirebaseAuth(GoogleWebClientId);
            // Notifications (Firebase Cloud Messaging). The server picks the channel: customers writing to a worker ring;
            // workers answering a Seeker are silent (only the dot on the app icon); a new post near a worker and a new
            // offer on a household's post ring.
            builder.AddFirebasePush(
                new PushChannel(PushData.CustomersChannel, Copy.PushChannelCustomers.English, Alert: true),
                new PushChannel(PushData.RepliesChannel, Copy.PushChannelReplies.English, Alert: false),
                new PushChannel(PushData.RequestsChannel, Copy.PushChannelRequests.English, Alert: true),
                new PushChannel(PushData.OffersChannel, Copy.PushChannelOffers.English, Alert: true));
            // Optional, each behind a build flag that is off by default (Directory.Build.props): usage analytics (Google
            // Analytics for Firebase) and A/B tests (Firebase Remote Config + A/B Testing). With a flag off the call
            // does nothing and the skeleton's do-nothing adapter stays, so the app builds with nothing set up in Firebase.
            builder.AddFirebaseAnalytics();
            builder.AddFirebaseExperiments(BuildInfo.Current.Channel == AppChannels.Dev ? TimeSpan.Zero : null);
            builder.Services.AddSingleton(BuildInfo.Current);
            builder.Services.AddSingleton<AppAnalytics>();
            builder.Services.AddSingleton<FeedbackBoard>();
            builder.Services.AddSingleton<JobBoard>();
            builder.Services.AddSingleton<ChatAlerts>();
            builder.Services.AddSingleton<IPhoneNumberFormat, PhilippinePhoneNumberFormat>();
            // Users, listings, reviews, chats and the trades and jobs (Azure SQL) and photos (Blob Storage), all through SideGigPh.Api.
            // Replaces the skeleton's on-device FilePhotoStore.
            // Every call names this build (X-App-Version), so the server's error logs say which app version called.
            var build = BuildInfo.Current;
            builder.Services.AddSideGigPhApi(new ApiOptions(ApiUrl, PhotoBaseUrl, ClientVersion: build.ClientHeader));
            builder.Services.AddSingleton<IOnboardingStore, FileOnboardingStore>();
            builder.Services.AddSingleton<ISplashLines, RoleSplashLines>();
            // The app config (trades and jobs, towns, prices…): loaded once while the splash shows (LaunchRouter), kept
            // on the phone (config.json) and used with no network call for 12 hours, then re-checked by ETag.
            builder.Services.AddSingleton<IConfigCache<AppConfig>>(services => new FileConfigCache<AppConfig>(
                "config.json", AppConfigJsonContext.Default.StampedAppConfig, services.GetRequiredService<ILogger<FileConfigCache<AppConfig>>>()));
            builder.Services.AddSingleton(AppConfigService.Rules);
            builder.Services.AddSingleton<RemoteConfig<AppConfig>>();
            builder.Services.AddSingleton<AppConfigService>();
            builder.Services.AddSingleton<OtherJobNotes>();
            builder.Services.AddSingleton<ProWorkspace>();
            builder.Services.AddSingleton<ListingRestore>();
            builder.Services.AddSingleton<ProBadges>();
            builder.Services.AddSingleton<BadgeReview>();
            builder.Services.AddSingleton<ProfessionalSearch>();
            builder.Services.AddSingleton<SeekerWorkspace>();
            builder.Services.AddSingleton<LocalAccount>();
            builder.Services.AddSingleton<IAppAccount>(services => services.GetRequiredService<LocalAccount>());

            // Error reports (Services/Diagnostics): errors, crashes and stuck loading screens, scrubbed of personal data, kept
            // on the phone (diagnostics.json) and sent in the background while signed in; the Me page's switch turns it off.
            builder.Services.AddSingleton(new DeviceStamp(
                build.WithStoreBuild(AppInfo.BuildString),
                build.CodeVersion,
                build.Channel,
                $"{DeviceInfo.Platform} {DeviceInfo.VersionString}".ToLowerInvariant(),
                $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}"));
            builder.Services.AddSingleton<IErrorLogStore, FileErrorLogStore>();
            builder.Services.AddSingleton<ErrorLog>();
            builder.Services.AddSingleton<ErrorReportSender>();
            builder.Services.AddSingleton<ErrorReporting>();
            builder.Services.AddSingleton<ILoggerProvider>(services => new ErrorLogLoggerProvider(services.GetRequiredService<ErrorLog>()));

#if ANDROID
            // App warnings and errors (API failures, photo picker) in logcat: adb logcat -s SideGigPh
            builder.Logging.AddProvider(new LogcatLoggerProvider());
#endif

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
