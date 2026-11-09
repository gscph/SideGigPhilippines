using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

public static class ListingMapper
{
    /// <summary>The Professional's own draft, shaped like a search result for the preview and Pro home.</summary>
    public static ProfessionalListing ToListing(this ProfessionalDraft draft) => new()
    {
        Id = "draft",
        DisplayName = draft.DisplayName ?? string.Empty,
        CategoryIds = draft.CategoryIds,
        ServiceIds = draft.ServiceIds,
        CityIds = draft.CityIds,
        ExperienceId = draft.ExperienceId,
        RateTierId = draft.RateTierId,
        MobileNumber = draft.MobileNumber ?? string.Empty,
    };
}
