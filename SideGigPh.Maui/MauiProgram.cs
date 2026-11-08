using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Dev;
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

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
