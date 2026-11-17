using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>"🕘 Suspension · Taytay  [Search] [Post]": one recent search on Seeker home; the page decides what the taps do.</summary>
public partial class RecentSearchRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public RecentSearch Search { get; set; } = default!;

    [Parameter] public EventCallback<RecentSearch> OnSearch { get; set; }

    [Parameter] public EventCallback<RecentSearch> OnPost { get; set; }

    /// <summary>"Suspension · Taytay": the job (what they typed for "Others", else the trade when no job) and the town.</summary>
    private string Summary
    {
        get
        {
            var job = Search.OtherJobText
                ?? Label(ReferenceData.Find(ReferenceData.ServicesOf(Search.CategoryId), Search.ServiceId) ?? ReferenceData.FindTrade(Search.CategoryId));
            var town = Label(ReferenceData.Find(ReferenceData.Cities, Search.CityId));
            return string.Join(" · ", new[] { job, town }.Where(part => part.Length > 0));
        }
    }

    private string Label(CatalogOption? option) => option is null ? string.Empty : T.Get(option.Label);
}
