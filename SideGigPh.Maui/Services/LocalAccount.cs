using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>
/// SideGigPh's <see cref="IAppAccount"/>: the person on this phone, whichever side they're on: their app language,
/// their saved profile (<see cref="IUserRepository"/>) and Start over, on the Me page of each side.
/// Only ports are used here, never adapters.
/// </summary>
public sealed class LocalAccount(
    IOnboardingStore store,
    IIdentityProvider identity,
    IUserRepository users,
    IPhotoStore photos,
    IProfessionalProfileRepository profiles,
    IChatRepository chats,
    IReviewRepository reviews,
    ProWorkspace pro,
    SeekerWorkspace seeker,
    AppText text) : IAppAccount
{
    /// <summary>Raised after the language changes, so the shell re-renders its labels.</summary>
    public event Action? Changed;

    public AppLanguage Language => text.Language;

    public async Task SetLanguageAsync(AppLanguage language)
    {
        text.Use(language);
        var state = await store.LoadAsync();
        state.Language = language;
        await store.SaveAsync(state);
        await SaveProfileAsync();
        Changed?.Invoke();
    }

    /// <summary>
    /// Saves the signed-in person's profile from their account (name, number) and their answers
    /// (language, side, earnings goal). Called when one of those changes. Does nothing when
    /// nobody is signed in (no sign-in provider registered).
    /// </summary>
    public async Task SaveProfileAsync()
    {
        if (await identity.GetCurrentUserAsync() is not { } user)
        {
            return;
        }

        var state = await store.LoadAsync();
        var now = DateTimeOffset.Now;
        var saved = await users.GetAsync(user.Uid);
        await users.SaveAsync(new UserProfile(
            user.Uid,
            user.DisplayName,
            user.PhoneNumber,
            user.Email,
            state.Language ?? text.Language,
            state.Role,
            state.Professional.IncomeGoalId,
            saved?.CreatedAt ?? now,
            now));
    }

    /// <summary>
    /// Deletes everything this person made (answers, profile, listing, photos, chats, reviews)
    /// so <c>/</c> starts the onboarding again from the language screen. They stay signed in.
    /// </summary>
    public async Task StartOverAsync()
    {
        var state = await store.LoadAsync();
        var draft = state.Professional;
        foreach (var photoId in draft.WorkPhotoIds.Append(draft.ProfilePhotoId).OfType<string>().Distinct())
        {
            await photos.DeleteAsync(photoId);
        }

        await profiles.RemoveAsync(ListingMapper.OwnListingId);
        foreach (var personId in new[] { ListingMapper.OwnListingId, SeekerWorkspace.LocalSeekerId })
        {
            await chats.DeleteForPersonAsync(personId);
            await reviews.DeleteForPersonAsync(personId);
        }

        if (await identity.GetCurrentUserAsync() is { } user)
        {
            await users.DeleteAsync(user.Uid);
        }

        await store.ClearAsync();
        pro.Forget();
        seeker.Forget();
    }
}
