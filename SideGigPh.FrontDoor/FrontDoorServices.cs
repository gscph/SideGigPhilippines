using MudBlazor.Services;
using SideGigPh.FrontDoor.EarlyAccess;
using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor;

/// <summary>The page's services: MudBlazor, the language, the sign-up and the few browser calls.</summary>
public static class FrontDoorServices
{
    public static IServiceCollection AddFrontDoor(this IServiceCollection services, string apiUrl)
    {
        services.AddMudServices();
        services.AddScoped<PageLanguage>();
        services.AddScoped<SignupState>();
        services.AddScoped<PageScript>();
        services.AddScoped<IEarlyAccessSignup>(_ => new HttpEarlyAccessSignup(new HttpClient { BaseAddress = new Uri(apiUrl) }));
        return services;
    }
}
