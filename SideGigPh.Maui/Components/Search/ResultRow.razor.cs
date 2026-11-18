using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// One search result, laid out like Airbnb's list card but with a small photo: "Aircon Technician in Angono" with ★ on
/// the right, the name muted, facts joined by · (<see cref="ResultText.FactsLine"/>), the average price in money green,
/// and at most one gray pill ("Fits your budget" or "Above your budget": over the budget only ranks lower, never hides). The whole row opens the worker's profile.
/// </summary>
public partial class ResultRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public SearchHit Hit { get; set; } = default!;

    /// <summary>The search's town: it names the "where".</summary>
    [Parameter, EditorRequired] public SearchRequest Request { get; set; } = default!;

    private string WhatAndWhere => T.WhatAndWhere(Hit.Pro, Request.CityId);

    private string Stars => T.StarsShort(Hit.Reviews);

    private string? Facts => T.FactsLine(Hit.Pro);

    private string? Price => Hit.Pro.RateLabel();

    private LocalizedText? BudgetPill => Hit.FitsBudget ? Copy.FitsBudget : Hit.AboveBudget ? Copy.AboveBudget : null;
}
