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
    /// as they are. A Seeker, someone with no listing, nobody signed in or a failed read leaves it unchanged, so
    /// launch carries on as before. Never throws. True when the listing was restored.
    /// </summary>
    public async Task<bool> RestoreAsync(OnboardingState state)
    {
        if (!HoldsNoAnswers(state))
        {
            return false;
        }

        AuthUser? user;
        ProfessionalDraft? draft;
        try
        {
            user = await identity.GetCurrentUserAsync();
            draft = user is null ? null : await profiles.GetOwnDraftAsync(user.Uid);
        }
        catch (Exception ex)
        {
            // Launch must go on: the worker answers the role question as on a new phone.
            logger.LogError(ex, "Reading the own listing to restore failed.");
            return false;
        }

        if (draft is null)
        {
            return false;
        }

        var now = DateTimeOffset.Now;
        draft.TakeAccount(user);
        draft.PublishedAt = now;
        draft.ListingOutOfDate = false;
        state.Professional = draft;
        state.Role = UserRole.Professional;
        state.ResumePath = null;
        state.CompletedAt = now;
        try
        {
            await store.SaveAsync(state);
        }
        catch (Exception ex)
        {
            // The store hands out this same state, so this run still opens Pro home; the next launch restores again.
            logger.LogError(ex, "Saving the restored listing failed.");
        }

        return true;
    }

    /// <summary>
    /// No side picked (a reinstall, a new phone), or the worker side picked and still on its first question with nothing answered: the role page
    /// asks a Google sign-in's worker for their number, and "Sign in with this number" brings the account that holds
    /// it (and maybe its live listing) back here before the first question.
    /// </summary>
    private static bool HoldsNoAnswers(OnboardingState state) =>
        state.Role is null
        || (state.Role == UserRole.Professional
            && state.CompletedAt is null
            && (state.ResumePath is null || state.ResumePath == OnboardingSteps.FirstPath(UserRole.Professional))
            && state.Professional is { CategoryId: null, PublishedAt: null, ProfilePhotoId: null, WorkPhotoIds.Count: 0 });
}
