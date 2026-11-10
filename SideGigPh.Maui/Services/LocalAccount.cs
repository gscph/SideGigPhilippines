using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The person on this phone, whichever side they're on: their app language and Start over,
/// both on the Me page of each side. Until auth, "the account" is what's saved on this phone;
/// the auth gate will turn Start over into signing out and deleting the account.
/// Only ports are used here, never adapters.
/// </summary>
public sealed class LocalAccount(
    IOnboardingStore store,
    IPhotoStore photos,
    IProfessionalProfileRepository profiles,
    IInquiryRepository inquiries,
    IReviewRepository reviews,
    ProWorkspace pro,
    SeekerWorkspace seeker,
    AppText text)
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
        Changed?.Invoke();
    }

    /// <summary>
    /// Deletes everything this person made on the phone (answers, listing, photos, messages,
    /// reviews) so <c>/</c> starts the onboarding again from the language screen.
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
            await inquiries.DeleteForPersonAsync(personId);
            await reviews.DeleteForPersonAsync(personId);
        }

        await store.ClearAsync();
        pro.Forget();
        seeker.Forget();
    }
}
