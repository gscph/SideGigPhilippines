using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Brings the signed-in account back onto a phone that has none of their answers (the app was reinstalled, or they
/// signed in on a new phone): a Skilled Worker's live listing (<see cref="IProfessionalProfileRepository.GetOwnDraftAsync"/>)
/// and a household's setup (<see cref="IUserRepository.GetAsync"/>: its finish date, home town and barangay), so launch
/// opens their home instead of the role question and the interview. Only ports are used here.
/// </summary>
public sealed class AccountRestore(
    IOnboardingStore store,
    IIdentityProvider identity,
    IProfessionalProfileRepository profiles,
    IUserRepository users,
    ILogger<AccountRestore> logger)
{
    /// <summary>
    /// Fills <paramref name="state"/> with the signed-in account's sides and saves it, when this phone holds nothing to
    /// keep (<see cref="HoldsNoAnswers"/>): an answered interview, a Seeker side or unsaved edits are left as they are.
    /// The two reads run together. Nothing on the server (a new account), nobody signed in or server errors on both
    /// reads leave it unchanged, so launch carries on as before; a server error on one read still restores what the
    /// other found. A read that got no answer (no internet) is <see cref="AccountRestoreResult.Offline"/>: the launch
    /// asks to try again instead of treating a set-up account as new. Never throws.
    /// </summary>
    public async Task<AccountRestoreResult> RestoreAsync(OnboardingState state)
    {
        if (!HoldsNoAnswers(state))
        {
            return AccountRestoreResult.Unchanged;
        }

        AuthUser? user;
        try
        {
            user = await identity.GetCurrentUserAsync();
        }
        catch (Exception ex) when (NetworkErrors.IsNetwork(ex))
        {
            logger.LogWarning(ex, "No answer reading the signed-in account to restore.");
            return AccountRestoreResult.Offline;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reading the signed-in account to restore failed.");
            return AccountRestoreResult.Unchanged;
        }

        if (user is null)
        {
            return AccountRestoreResult.Unchanged;
        }

        var listing = Read(() => profiles.GetOwnDraftAsync(user.Uid), "the own listing");
        var account = Read(() => users.GetAsync(user.Uid), "the own profile");
        var (draft, listingOffline) = await listing;
        var (profile, accountOffline) = await account;
        if (listingOffline || accountOffline)
        {
            return AccountRestoreResult.Offline;
        }

        var worker = draft is not null;
        var household = profile?.SeekerFinishedAt is not null;
        if (!worker && !household)
        {
            return AccountRestoreResult.Unchanged;
        }

        if (draft is not null)
        {
            var now = DateTimeOffset.Now;
            draft.TakeAccount(user);
            draft.PublishedAt = now;
            draft.ListingOutOfDate = false;
            state.Professional = draft;
            state.ProFinishedAt = now;
        }

        if (household && profile is not null)
        {
            RestoreHousehold(state, profile);
        }

        // The side they last opened, when it came back; else the one that did (a worker first, as before).
        state.Role = profile?.Role is { } last && (last == UserRole.Professional ? worker : household)
            ? last
            : worker ? UserRole.Professional : UserRole.Seeker;
        state.ResumePath = null;
        try
        {
            await store.SaveAsync(state);
        }
        catch (Exception ex)
        {
            // The store hands out this same state, so this run still opens their home; the next launch restores again.
            logger.LogError(ex, "Saving the restored account failed.");
        }

        return AccountRestoreResult.Restored;
    }

    // The household side: finished, its home town (only a town we still serve) and barangay (only one of that town's),
    // and the account's profile photo. A finished Seeker with no trade opens Seeker home (OnboardingSteps.LaunchPath).
    private static void RestoreHousehold(OnboardingState state, UserProfile profile)
    {
        state.SeekerFinishedAt = profile.SeekerFinishedAt;
        if (ReferenceData.AreaOf(profile.HomeTownId) is { } area)
        {
            state.Seeker.CityId = profile.HomeTownId;
            state.Seeker.AreaId = area;
            state.Seeker.BarangayId = ReferenceData.FindBarangay(profile.HomeTownId, profile.HomeBarangayId)?.Id;
        }

        state.Seeker.ProfilePhotoId ??= profile.ProfilePhotoId;
    }

    // One read: its value, or Offline when it got no answer. A server error (or a bug) is logged and reads as nothing,
    // so a broken server never locks the app.
    private async Task<(T? Value, bool Offline)> Read<T>(Func<Task<T?>> read, string what) where T : class
    {
        try
        {
            return (await read(), false);
        }
        catch (Exception ex) when (NetworkErrors.IsNetwork(ex))
        {
            logger.LogWarning(ex, "No answer reading {What} to restore.", what);
            return (null, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reading {What} to restore failed.", what);
            return (null, false);
        }
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
            && !state.HasFinished(UserRole.Seeker)
            && (state.ResumePath is null || state.ResumePath == OnboardingSteps.FirstPath(UserRole.Professional))
            && state.Professional is { CategoryId: null, PublishedAt: null, ProfilePhotoId: null, WorkPhotoIds.Count: 0 });
}

/// <summary>What <see cref="AccountRestore.RestoreAsync"/> did.</summary>
public enum AccountRestoreResult
{
    /// <summary>Nothing to restore (answers on the phone, a new account, server errors): launch goes on.</summary>
    Unchanged,

    /// <summary>The account's side(s) are back on the phone: launch opens their home.</summary>
    Restored,

    /// <summary>A read got no answer: whether the account has a side set up is unknown, so launch asks to try again.</summary>
    Offline,
}
