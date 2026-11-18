using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

public static class ListingMapper
{
    /// <summary>
    /// Id of the pro's own listing when nobody is signed in (no sign-in provider registered). With an account
    /// the id is the user's uid (<c>ProWorkspace.MyId</c>), which is what SideGigPh.Api checks.
    /// </summary>
    public const string OwnListingId = "own";

    /// <summary>
    /// The listing's name and number come from the account, never typed in the interview: the
    /// account's name (from Google, or asked once after a phone sign-in) and its verified number.
    /// Does nothing when nobody is signed in (no sign-in provider registered).
    /// </summary>
    public static void TakeAccount(this ProfessionalDraft draft, AuthUser? user)
    {
        if (user is null)
        {
            return;
        }

        draft.DisplayName = user.DisplayName;
        draft.MobileNumber = user.PhoneNumber;
    }

    /// <summary>The Professional's own draft, shaped like a search result for the preview and Pro home.</summary>
    public static ProfessionalListing ToListing(
        this ProfessionalDraft draft, string? id = null, string? photoUrl = null, IReadOnlyList<string>? workPhotoUrls = null) => new()
    {
        Id = id ?? OwnListingId,
        DisplayName = draft.DisplayName ?? string.Empty,
        CategoryIds = draft.CategoryId is null ? [] : [draft.CategoryId],
        ServiceIds = draft.ServiceIds,
        CityIds = draft.CityIds,
        ExperienceId = draft.ExperienceId,
        RatePesos = draft.RatePesos,
        OtherWork = draft.OtherJobText,
        MobileNumber = draft.MobileNumber ?? string.Empty,
        PhotoUrl = photoUrl,
        WorkPhotoUrls = workPhotoUrls ?? [],
        BasePoint = draft.BasePoint,
        RadiusKm = draft.RadiusKm,
    };
}
