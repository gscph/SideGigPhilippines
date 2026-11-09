using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// A Professional's listing card. The same card is used in Seeker results and in the
/// Professional's own preview, so what they build is exactly what customers will see.
/// </summary>
public partial class ProCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>Optional green badge, e.g. "Fits your budget".</summary>
    [Parameter] public string? Highlight { get; set; }

    /// <summary>Optional buttons (Call / Text) supplied by the page.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Extra CSS classes on the card, e.g. to outline the pro's own listing.</summary>
    [Parameter] public string? Class { get; set; }

    private string Initials =>
        string.Concat(Listing.DisplayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0])));

    private string Trades => string.Join(" · ", Labels(ReferenceData.Categories, Listing.CategoryIds));

    private IReadOnlyList<string> ServiceLabels =>
        Listing.ServiceIds
            .Select(ReferenceData.FindService)
            .OfType<CatalogOption>()
            .Select(option => T.Get(option.Label))
            .ToList();

    private string Towns => string.Join(", ", Labels(ReferenceData.Cities, Listing.CityIds));

    private string? ExperienceLabel => ReferenceData.Find(ReferenceData.Experience, Listing.ExperienceId) is { } option
        ? T.Get(option.Label)
        : null;

    private string Rate => ReferenceData.Find(ReferenceData.PriceTiers, Listing.RateTierId) is { } tier
        ? T.Format(Copy.RatePerJob, T.Get(tier.Label))
        : T.Get(Copy.RateQuote);

    private IEnumerable<string> Labels(IEnumerable<CatalogOption> options, IEnumerable<string> ids) =>
        ids.Select(id => ReferenceData.Find(options, id))
            .OfType<CatalogOption>()
            .Select(option => T.Get(option.Label));
}
