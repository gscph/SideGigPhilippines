using Microsoft.Extensions.Logging;
using AppTheme = SideGigPh.Maui.Components.Layout.AppTheme; // MAUI has an AppTheme enum too
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Maui.Services.Dev;
using SideGigPh.Maui.Services.Firestore;
using SideGigPh.Maui.Services.Local;
using SideGigPh.Maui.Services.Search;
using MobileAppTheme.Firebase;

namespace SideGigPh.Maui
{
    public static class MauiProgram
    {
        // Web OAuth client (client_type 3) from google-services.json, for Google sign-in on Android.
        private const string GoogleWebClientId = "742654871295-4slv8fpgpbrllq8aoe3vbiphv3e64q3f.apps.googleusercontent.com";

        // SideGigPh.Api (Azure Functions) and the public blob container its photos are readable from. Not secrets.
        private const string PhotoApiUrl = "https://fredhopper-bdhed2e6dtegb9gz.uksouth-01.azurewebsites.net/api/";
        private const string PhotoBaseUrl = "https://realtimeordertracker.blob.core.windows.net/photos/";

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
            builder.Services.AddSingleton<IAuthLanding, SideGigPhAuthLanding>();
            builder.Services.AddSingleton<IPhoneNumberFormat, PhilippinePhoneNumberFormat>();
            // Photos go to Azure Blob Storage through SideGigPh.Api (replaces the skeleton's on-device FilePhotoStore).
            builder.Services.AddSingleton(new PhotoApiOptions(PhotoApiUrl, PhotoBaseUrl));
            builder.Services.AddSingleton<IPhotoStore, ApiPhotoStore>();
            builder.Services.AddSingleton<IOnboardingStore, FileOnboardingStore>();
#if STUB_DATA
            // On-phone stand-ins (dotnet build -p:UseStubData=true): sample listings, chats and reviews in files.
            builder.Services.AddSingleton<IProfessionalProfileRepository, StubProfessionalProfileRepository>();
            builder.Services.AddSingleton<IUserRepository, StubUserRepository>();
            builder.Services.AddSingleton<IChatRepository, StubChatRepository>();
            builder.Services.AddSingleton<IReviewRepository, StubReviewRepository>();
#else
            // Users, listings, reviews and chats in Cloud Firestore (security rules: firebase/firestore.rules).
            builder.Services.AddFirestoreData();
#endif
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
