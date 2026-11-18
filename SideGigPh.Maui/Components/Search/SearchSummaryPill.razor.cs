using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// The top of the results, like Airbnb's "Homes in Baguio · Any weekend · Add guests" pill: "Aircon cleaning in Angono"
/// over "Any time · Any budget". A tap reopens the search sheet on this search.
/// </summary>
public partial class SearchSummaryPill : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public SearchRequest Request { get; set; } = default!;

    /// <summary>The typed words, shown in place of the job when there are some.</summary>
    [Parameter] public string? Words { get; set; }

    [Parameter] public string? UrgencyId { get; set; }

    private static string Href => SeekerRoutes.Find(SeekerRoutes.Search);

    private string Title => T.SummaryTitle(T.What(Words, Request.CategoryId, Request.ServiceId), T.Where(Request.CityId));

    private string Note => T.SummaryNote(UrgencyId, Request.BudgetId);
}
