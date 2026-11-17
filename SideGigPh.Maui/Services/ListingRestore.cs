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
    /// Fills <paramref name="state"/> with the signed-in account's live listing and saves it, when this phone has no
    /// side picked yet (nothing here to overwrite: a picked side, an unfinished interview or unsaved edits are left
    /// as they are). A Seeker, someone with no listing, nobody signed in or a failed read leaves it unchanged, so
    /// launch carries on to the role question. Never throws. True when the listing was restored.
    /// </summary>
    public async Task<bool> RestoreAsync(OnboardingState state)
    {
        if (state.Role is not null)
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
}
