using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Brings a Skilled Worker's live listing back onto a phone that has none of their answers (the app was reinstalled,
/// or they signed in on a new phone), so launch opens Pro home instead of the interview. Only ports are used here.
/// </summary>
public sealed class ListingRestore(
    IOnboardingStore store,
    IIdentityProvider identity,
    IProfessionalProfileRepository profiles,
    ILogger<ListingRestore> logger)
{
    /// <summary>
    /// Fills <paramref name="state"/> with the signed-in account's live listing and saves it, when this phone holds
    /// nothing to keep (<see cref="HoldsNoAnswers"/>): an answered interview, a Seeker side or unsaved edits are left
    /// as they are. A Seeker, someone with no listing, nobody signed in or a server error leaves it unchanged, so
    /// launch carries on as before. A read that got no answer (no internet) is <see cref="ListingRestoreResult.Offline"/>:
    /// the launch asks to try again instead of treating a live worker as new. Never throws.
    /// </summary>
    public async Task<ListingRestoreResult> RestoreAsync(OnboardingState state)
    {
        if (!HoldsNoAnswers(state))
        {
            return ListingRestoreResult.Unchanged;
        }

        AuthUser? user;
        ProfessionalDraft? draft;
        try
        {
            user = await identity.GetCurrentUserAsync();
            draft = user is null ? null : await profiles.GetOwnDraftAsync(user.Uid);
        }
        catch (Exception ex) when (NetworkErrors.IsNetwork(ex))
        {
            logger.LogWarning(ex, "No answer reading the own listing to restore.");
            return ListingRestoreResult.Offline;
        }
        catch (Exception ex)
        {
            // A server error: launch must go on, and the worker answers the role question as on a new phone.
            logger.LogError(ex, "Reading the own listing to restore failed.");
            return ListingRestoreResult.Unchanged;
        }

        if (draft is null)
        {
            return ListingRestoreResult.Unchanged;
        }

        var now = DateTimeOffset.Now;
        draft.TakeAccount(user);
        draft.PublishedAt = now;
        draft.ListingOutOfDate = false;
        state.Professional = draft;
        state.Role = UserRole.Professional;
        state.ResumePath = null;
        state.ProFinishedAt = now;
        try
        {
            await store.SaveAsync(state);
        }
        catch (Exception ex)
        {
            // The store hands out this same state, so this run still opens Pro home; the next launch restores again.
            logger.LogError(ex, "Saving the restored listing failed.");
        }

        return ListingRestoreResult.Restored;
    }

    /// <summary>
    /// No side picked (a reinstall, a new phone), or the worker side picked and still on its first question with nothing answered: the role page
    /// asks a Google sign-in's worker for their number, and "Sign in with this number" brings the account that holds
    /// it (and maybe its live listing) back here before the first question.
    /// </summary>
    private static bool HoldsNoAnswers(OnboardingState state) =>
        state.Role is null
        || (state.Role == UserRole.Professional
            && !state.HasFinished(UserRole.Professional)
            && (state.ResumePath is null || state.ResumePath == OnboardingSteps.FirstPath(UserRole.Professional))
            && state.Professional is { CategoryId: null, PublishedAt: null, ProfilePhotoId: null, WorkPhotoIds.Count: 0 });
}

/// <summary>What <see cref="ListingRestore.RestoreAsync"/> did.</summary>
public enum ListingRestoreResult
{
    /// <summary>Nothing to restore (answers on the phone, a Seeker, no listing, a server error): launch goes on.</summary>
    Unchanged,

    /// <summary>The live listing is back on the phone: launch opens Pro home.</summary>
    Restored,

    /// <summary>The read got no answer: whether there is a listing is unknown, so launch asks to try again.</summary>
    Offline,
}
