using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>Mock search results with the Professional's draft card in the middle.</summary>
public partial class ListingPreview : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>What a customer would have searched to find them, e.g. "Linis ng aircon · Taytay".</summary>
    private string SearchText => string.Join(" · ", new[]
    {
        ReferenceData.FindService(Listing.ServiceIds.FirstOrDefault()),
        ReferenceData.Find(ReferenceData.Cities, Listing.CityIds.FirstOrDefault()),
    }.OfType<CatalogOption>().Select(option => T.Get(option.Label)));
}
