namespace SideGigPh.Maui.Services.Firestore;

public static class FirestoreServiceCollectionExtensions
{
    /// <summary>
    /// The real data: users, listings, reviews and chats in Cloud Firestore. One call in <c>MauiProgram</c>
    /// replaces the four <c>Dev/Stub*Repository</c> stand-ins. The native SDK is started with the rest of Firebase
    /// (<c>AddFirebaseAuth</c>), and every request carries the signed-in user, which is what the rules check.
    /// </summary>
    public static IServiceCollection AddFirestoreData(this IServiceCollection services)
    {
        services.AddSingleton<IProfessionalProfileRepository, FirestoreProfessionalProfileRepository>();
        services.AddSingleton<IUserRepository, FirestoreUserRepository>();
        services.AddSingleton<IChatRepository, FirestoreChatRepository>();
        services.AddSingleton<IReviewRepository, FirestoreReviewRepository>();
        return services;
    }
}
