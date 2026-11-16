using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileAppTheme.Auth;
using MobileAppTheme.Branding;
using MobileAppTheme.Localization;
using MobileAppTheme.Services;
using MobileAppTheme.Services.Local;
using MudBlazor.Services;

namespace MobileAppTheme;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the skeleton: MudBlazor, the app's <paramref name="brand"/>, <see cref="AppText"/>
    /// and a default adapter for each skeleton port. The defaults use <c>TryAdd</c>, so an app that
    /// registers its own adapter for a port first keeps it (and one registered later wins).
    /// Auth defaults: <see cref="NoIdentityProvider"/> (no sign-in until the app adds a provider such
    /// as <c>AddFirebaseAuth</c>), <see cref="HomeAuthLanding"/> and <see cref="E164PhoneNumberFormat"/>;
    /// push: <see cref="NoPushNotifications"/> (until the app adds a provider such as <c>AddFirebasePush</c>);
    /// analytics and A/B tests: <see cref="NoAnalytics"/> and <see cref="NoExperiments"/> (until <c>AddFirebaseAnalytics</c> / <c>AddFirebaseExperiments</c>).
    /// Busy splash lines: <see cref="BusySplashLines"/> (until the app registers its own <see cref="ISplashLines"/>).
    /// The app still registers <see cref="IAppAccount"/> (needed by <c>MePage</c>) and its own ports.
    /// </summary>
    public static IServiceCollection AddMobileAppTheme(this IServiceCollection services, AppBrand brand)
    {
        services.AddMudServices();
        services.AddSingleton(brand);
        services.AddSingleton<AppText>();
        services.AddSingleton<BusyIndicator>();
        services.TryAddSingleton<ISplashLines, BusySplashLines>();
        services.TryAddSingleton<AppVisibility>();
        services.TryAddSingleton<INetworkStatus, DeviceNetworkStatus>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISmsSendHistory, PreferencesSmsSendHistory>();
        services.TryAddSingleton<SmsSendLimit>();

        services.TryAddSingleton<IContactLauncher, DeviceContactLauncher>();
        services.TryAddSingleton<DevicePhotoPicker>();
        services.TryAddSingleton<IPhotoPicker>(provider => provider.GetRequiredService<DevicePhotoPicker>());
        services.TryAddSingleton<IPhotoStore, FilePhotoStore>();
        services.TryAddSingleton<IPushNotifications, NoPushNotifications>();
        services.TryAddSingleton<IAnalytics, NoAnalytics>();
        services.TryAddSingleton<IExperiments, NoExperiments>();

        services.TryAddSingleton<IIdentityProvider, NoIdentityProvider>();
        services.TryAddSingleton<IAuthLanding, HomeAuthLanding>();
        services.TryAddSingleton<SignInGate>();
        services.TryAddSingleton<IPhoneNumberFormat, E164PhoneNumberFormat>();
        return services;
    }
}
