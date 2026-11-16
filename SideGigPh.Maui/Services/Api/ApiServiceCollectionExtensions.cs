using Microsoft.Extensions.DependencyInjection.Extensions;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// The real data, all through SideGigPh.Api: users, listings, reviews, chats and the app config (Azure SQL on the server) and
    /// photos (Azure Blob Storage; replaces the skeleton's on-device <c>FilePhotoStore</c>, so call it after
    /// <c>AddMobileAppTheme</c>). Every request carries the signed-in user's ID token, which the server checks.
    /// </summary>
    public static IServiceCollection AddSideGigPhApi(this IServiceCollection services, ApiOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<ApiClient>();
        services.TryAddSingleton<AppVisibility>(); // AddMobileAppTheme's; tests call only this
        services.AddSingleton<IPhotoStore, ApiPhotoStore>();
        services.AddSingleton<IProfessionalProfileRepository, ApiProfessionalProfileRepository>();
        services.AddSingleton<IUserRepository, ApiUserRepository>();
        services.AddSingleton<IChatRepository, ApiChatRepository>();
        services.AddSingleton<IReviewRepository, ApiReviewRepository>();
        services.AddSingleton<ApiConfigRepository>();
        services.AddSingleton<IConfigSource<AppConfig>>(provider => provider.GetRequiredService<ApiConfigRepository>());
        services.AddSingleton<IOtherJobNoteRepository>(provider => provider.GetRequiredService<ApiConfigRepository>());
        services.AddSingleton<IErrorReportRepository, ApiErrorReportRepository>();
        services.AddSingleton<ISafetyRepository, ApiSafetyRepository>();
        services.AddSingleton<IFeedbackRepository, ApiFeedbackRepository>();
        services.AddSingleton<IVerificationRepository, ApiVerificationRepository>();
        services.AddSingleton<IServerWarmup, ApiServerWarmup>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ServerWarmup>();
        return services;
    }
}
