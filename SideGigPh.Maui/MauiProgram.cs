using Microsoft.Extensions.Logging;
using AppTheme = SideGigPh.Maui.Components.Layout.AppTheme; // MAUI has an AppTheme enum too
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
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
        private const string ApiUrl = "https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/";
        private const string PhotoBaseUrl = "https://sidegigphbfb9.blob.core.windows.net/photos/";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // --- MobileAppTheme skeleton: MudBlazor, brand/theme, AppText, and the on-device
            // adapters for IContactLauncher, IPhotoPicker and IPhotoStore ---
            builder.Services.AddMobileAppTheme(AppTheme.Brand);

            // --- Ports & Adapters (see CLAUDE.md) ---
            // Sign-in provider. To switch, replace this line (and the project reference) with another
            // IIdentityProvider adapter; nothing else in the app knows which provider it is.
            builder.AddFirebaseAuth(GoogleWebClientId);
            // New-message notifications (Firebase Cloud Messaging). The server picks the channel: customers writing to a
            // worker ring; workers answering a Seeker are silent (only the dot on the app icon).
            builder.AddFirebasePush(
                new PushChannel(PushData.CustomersChannel, Copy.PushChannelCustomers.English, Alert: true),
                new PushChannel(PushData.RepliesChannel, Copy.PushChannelReplies.English, Alert: false));
            builder.Services.AddSingleton<ChatAlerts>();
            builder.Services.AddSingleton<IAuthLanding, SideGigPhAuthLanding>();
            builder.Services.AddSingleton<IPhoneNumberFormat, PhilippinePhoneNumberFormat>();
            // Users, listings, reviews, chats and the trades and jobs (Azure SQL) and photos (Blob Storage), all through SideGigPh.Api.
            // Replaces the skeleton's on-device FilePhotoStore.
            builder.Services.AddSideGigPhApi(new ApiOptions(ApiUrl, PhotoBaseUrl));
            builder.Services.AddSingleton<IOnboardingStore, FileOnboardingStore>();
            // The app config (trades and jobs, towns, prices…): loaded once while the splash shows (LaunchRouter), kept
            // on the phone (config.json) and used with no network call for 12 hours, then re-checked by ETag.
            builder.Services.AddSingleton<IConfigCache<AppConfig>>(services => new FileConfigCache<AppConfig>(
                "config.json", AppConfigJsonContext.Default.StampedAppConfig, services.GetRequiredService<ILogger<FileConfigCache<AppConfig>>>()));
            builder.Services.AddSingleton(AppConfigService.Rules);
            builder.Services.AddSingleton<RemoteConfig<AppConfig>>();
            builder.Services.AddSingleton<AppConfigService>();
            builder.Services.AddSingleton<OtherJobNotes>();
            builder.Services.AddSingleton<ProWorkspace>();
            builder.Services.AddSingleton<ProfessionalSearch>();
            builder.Services.AddSingleton<SeekerWorkspace>();
            builder.Services.AddSingleton<LocalAccount>();
            builder.Services.AddSingleton<IAppAccount>(services => services.GetRequiredService<LocalAccount>());

            // Error reports (Services/Diagnostics): errors, crashes and stuck loading screens, scrubbed of personal data, kept
            // on the phone (diagnostics.json) and sent in the background while signed in; the Me page's switch turns it off.
            builder.Services.AddSingleton(new DeviceStamp(
                $"{AppInfo.VersionString} ({AppInfo.BuildString})",
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
