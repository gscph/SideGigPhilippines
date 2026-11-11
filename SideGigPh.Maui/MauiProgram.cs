using Microsoft.Extensions.Logging;
using AppTheme = SideGigPh.Maui.Components.Layout.AppTheme; // MAUI has an AppTheme enum too
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Dev;
using SideGigPh.Maui.Services.Local;
using SideGigPh.Maui.Services.Search;
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

            // --- MobileAppTheme skeleton: MudBlazor, brand/theme, AppText, and the on-device
            // adapters for IContactLauncher, IPhotoPicker and IPhotoStore ---
            builder.Services.AddMobileAppTheme(AppTheme.Brand);

            // --- Ports & Adapters (see CLAUDE.md) ---
            builder.RegisterFirebaseServices();
            builder.Services.AddSingleton<IIdentityProvider, FirebaseIdentityProvider>();
            builder.Services.AddSingleton<IProfessionalProfileRepository, StubProfessionalProfileRepository>();
            builder.Services.AddSingleton<IOnboardingStore, FileOnboardingStore>();
            builder.Services.AddSingleton<IInquiryRepository, StubInquiryRepository>();
            builder.Services.AddSingleton<IReviewRepository, StubReviewRepository>();
            builder.Services.AddSingleton<ProWorkspace>();
            builder.Services.AddSingleton<ProfessionalSearch>();
            builder.Services.AddSingleton<SeekerWorkspace>();
            builder.Services.AddSingleton<LocalAccount>();
            builder.Services.AddSingleton<IAppAccount>(services => services.GetRequiredService<LocalAccount>());

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
