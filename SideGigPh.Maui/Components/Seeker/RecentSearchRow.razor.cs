using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// One recent search on Seeker home, like Airbnb's recent rows: the job on top, the town muted under it. Tapping the row
/// searches again; the underlined "Post" on the right opens the post form filled with it. The page decides what the taps do.
/// </summary>
public partial class RecentSearchRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public RecentSearch Search { get; set; } = default!;

    [Parameter] public EventCallback<RecentSearch> OnSearch { get; set; }

    [Parameter] public EventCallback<RecentSearch> OnPost { get; set; }

    /// <summary>The job: what they typed for "Others", else the job, else the trade when no job.</summary>
    private string Job => Search.OtherJobText
        ?? Label(ReferenceData.Find(ReferenceData.ServicesOf(Search.CategoryId), Search.ServiceId) ?? ReferenceData.FindTrade(Search.CategoryId));

    private string Town => Label(ReferenceData.Find(ReferenceData.Cities, Search.CityId));

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
