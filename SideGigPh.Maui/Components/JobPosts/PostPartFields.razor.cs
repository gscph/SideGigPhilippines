using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The fields of one change sheet on the new post page: What (kind of work + service), Where (town, barangay, "Use my
/// location") or Your budget (from / up to, the suggested minimum price as a guide). They change <see cref="Draft"/>
/// directly; the page keeps a <see cref="PostSnapshot"/> so Cancel puts the answers back.
/// </summary>
public partial class PostPartFields : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPostDraft Draft { get; set; } = default!;

    [Parameter] public PostPart Part { get; set; }

    private string BudgetGuide => T.Format(Copy.BudgetFromHelp, Money.Peso(ReferenceData.MinimumWage));

    private bool BudgetTopTooLow => Draft.BudgetMinPesos is { } from && Draft.BudgetPesos is { } upTo && upTo < from;

    private void PickTrade(string? categoryId) => PostReview.PickTrade(Draft, categoryId);

    private void PickTown(string? cityId) => PostReview.PickTown(Draft, cityId);
}
