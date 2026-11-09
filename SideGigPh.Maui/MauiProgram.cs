using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Dev;
using SideGigPh.Maui.Services.Local;
using SideGigPh.Maui.Services.Localization;
#if ANDROID || IOS
using SideGigPh.Maui.Services.Firebase;
#endif

namespace SideGigPh.Maui
{
    public static class MauiProgram
    {
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

            // --- Ports & Adapters (see CLAUDE.md) ---
            builder.RegisterFirebaseServices();
            builder.Services.AddSingleton<IIdentityProvider, FirebaseIdentityProvider>();
            builder.Services.AddSingleton<IProfessionalProfileRepository, StubProfessionalProfileRepository>();
            builder.Services.AddSingleton<IOnboardingStore, FileOnboardingStore>();
            builder.Services.AddSingleton<IContactLauncher, DeviceContactLauncher>();
            builder.Services.AddSingleton<AppText>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
