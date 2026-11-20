using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Services;

/// <summary>
/// SideGigPh's <see cref="IAppAccount"/>: the person on this phone, whichever side they're on: their app language,
/// their saved profile (<see cref="IUserRepository"/>) and Start over, on the Me page of each side. Also the name
/// page's <see cref="IDisplayNameCheck"/>: a new name goes to the server first, which refuses a rude one.
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
    IJobPostRepository jobPosts,
    ProWorkspace pro,
    SeekerWorkspace seeker,
    AppText text,
    ILogger<LocalAccount> logger) : IAppAccount, IDisplayNameCheck
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
        // The server's copy is a backup (logged on failure, saved again with the next change): the screen switches
        // language at once instead of after a cold server or weak 4G answers.
        _ = SaveProfileAsync();
    }

    /// <summary>
    /// Saves the signed-in person's profile from their account (name, number) and their answers
    /// (language, side, profile photo). Called when one of those changes. Does nothing when
    /// nobody is signed in (no sign-in provider registered). A failed save is logged, not thrown.
    /// </summary>
    public async Task SaveProfileAsync()
    {
        if (await identity.GetCurrentUserAsync() is not { } user)
        {
            return;
        }

        try
        {
            await SaveProfileAsync(user, user.DisplayName);
        }
        catch (Exception ex)
        {
            // The profile is a backup of the person's choices, not something the screens wait on:
            // an offline phone must still get through onboarding. The next change saves it again.
            logger.LogWarning(ex, "Saving the profile of {Uid} failed.", user.Uid);
        }
    }

    /// <summary>
    /// The name page's check: the profile is saved with the new name before the account gets it, so a name the server
    /// refuses (rude words, <see cref="TextNotAllowedException"/>) is never kept. Any other failure allows it: the
    /// profile is a backup, and the next change saves it again.
    /// </summary>
    public async Task<bool> AllowsAsync(string displayName)
    {
        if (await identity.GetCurrentUserAsync() is not { } user)
        {
            return true;
        }

        try
        {
            await SaveProfileAsync(user, displayName);
            return true;
        }
        catch (TextNotAllowedException)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Saving the new name of {Uid} failed.", user.Uid);
            return true;
        }
    }

    // One call, no read first: the server keeps the first CreatedAt itself (UserStore.SaveAsync).
    private async Task SaveProfileAsync(AuthUser user, string? displayName)
    {
        var state = await store.LoadAsync();
        var now = DateTimeOffset.Now;
        await users.SaveAsync(new UserProfile(
            user.Uid,
            displayName,
            user.PhoneNumber,
            user.Email,
            state.Language ?? text.Language,
            state.Role,
            now,
            now,
            state.Role == UserRole.Professional ? state.Professional.ProfilePhotoId : state.Seeker.ProfilePhotoId));
    }

    /// <summary>
    /// Deletes everything this person made (answers, profile, listing, photos, chats, reviews)
    /// so <c>/</c> starts the onboarding again from the language screen. They stay signed in.
    /// </summary>
    public async Task SignOutAsync()
    {
        await identity.SignOutAsync();
        ForgetLoaded();
    }

    public Task AccountSwitchedAsync()
    {
        ForgetLoaded();
        return Task.CompletedTask;
    }

    // Both sides loaded this account's id, listing, chats and reviews: the next account loads its own.
    private void ForgetLoaded()
    {
        pro.Forget();
        seeker.Forget();
    }

    public async Task StartOverAsync()
    {
        var state = await store.LoadAsync();
        var draft = state.Professional;
        foreach (var photoId in draft.WorkPhotoIds.Append(draft.ProfilePhotoId).Append(state.Seeker.ProfilePhotoId).OfType<string>().Distinct())
        {
            await photos.DeleteAsync(photoId);
        }

        // Signed in, both sides are the one uid; with no sign-in provider they are the two fixed local ids.
        var user = await identity.GetCurrentUserAsync();
        string[] personIds = user is null ? [ListingMapper.OwnListingId, SeekerWorkspace.LocalSeekerId] : [user.Uid];
        await profiles.RemoveAsync(personIds[0]);

        // Their open requests come down too: workers would otherwise keep sending offers nobody reads.
        foreach (var post in (await jobPosts.GetMineAsync()).Where(post => post.IsOpen))
        {
            await jobPosts.CloseAsync(post.Id);
        }

        foreach (var personId in personIds)
        {
            await chats.DeleteForPersonAsync(personId);
            await reviews.DeleteForPersonAsync(personId);
        }

        if (user is not null)
        {
            await users.DeleteAsync(user.Uid);
        }

        await store.ClearAsync();
        pro.Forget();
        seeker.Forget();
    }

    /// <summary>
    /// Delete account: the server deletes everything the person has there (listing, photos, any old references, chats, the
    /// reviews they wrote and the ones about them) and, when it can, their sign-in account; then the sign-in provider's
    /// part (deletes the account if it's still there, signs out) and this phone's answers. Throws when the server
    /// didn't confirm, before anything on the phone is touched, so the person can try again.
    /// </summary>
    public async Task DeleteAccountAsync()
    {
        if (await identity.GetCurrentUserAsync() is null)
        {
            await StartOverAsync(); // no sign-in provider: the phone is the account
            return;
        }

        var state = await store.LoadAsync();
        var draft = state.Professional;
        foreach (var photoId in draft.WorkPhotoIds.Append(draft.ProfilePhotoId).Append(state.Seeker.ProfilePhotoId).OfType<string>().Distinct())
        {
            try
            {
                // Photos uploaded but not on the listing yet; the server deletes the ones it knows.
                await photos.DeleteAsync(photoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Deleting photo {PhotoId} with the account failed.", photoId);
            }
        }

        await users.DeleteAccountAsync();
        await identity.DeleteAccountAsync();
        await store.ClearAsync();
        pro.Forget();
        seeker.Forget();
    }
}
