using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// and the on-device adapters for the skeleton's ports. The adapters use <c>TryAdd</c>, so an app
    /// that registers its own adapter for a port first keeps it. The app still registers
    /// <see cref="IAppAccount"/> (needed by <c>MePage</c>) and its own ports.
    /// </summary>
    public static IServiceCollection AddMobileAppTheme(this IServiceCollection services, AppBrand brand)
    {
        services.AddMudServices();
        services.AddSingleton(brand);
        services.AddSingleton<AppText>();

        services.TryAddSingleton<IContactLauncher, DeviceContactLauncher>();
        services.TryAddSingleton<IPhotoPicker, DevicePhotoPicker>();
        services.TryAddSingleton<IPhotoStore, FilePhotoStore>();
        return services;
    }
}
