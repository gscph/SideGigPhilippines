using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// One search result, laid out like Airbnb's list card but with a small photo: the worker's name with ★ on the right,
/// "Aircon Technician in Angono" muted, facts joined by · (<see cref="ResultText.FactsLine"/>), the price in money green, and at
/// most one gray pill ("Fits your budget"). The whole row opens the worker's profile.
/// </summary>
public partial class ResultRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public SearchHit Hit { get; set; } = default!;

    /// <summary>The search's town and job: the town names the "where", the job picks the price.</summary>
    [Parameter, EditorRequired] public SearchRequest Request { get; set; } = default!;

    private string WhatAndWhere => T.WhatAndWhere(Hit.Pro, Request.CityId);

    private string Stars => T.StarsShort(Hit.Reviews);

    private string? Facts => T.FactsLine(Hit.Pro);

    private string? Price => T.RateLabel(Hit.Pro, Request.ServiceId);
}
