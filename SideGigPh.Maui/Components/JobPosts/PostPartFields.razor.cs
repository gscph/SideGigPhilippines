using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The fields of one change sheet on the new post page: What (picture tiles: kind of work, then its services, like the
/// interview and the search sheet), Where (town, barangay, "Use my
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

    private IReadOnlyList<ChoiceItem> TradeTiles => ChoiceItem.From(ReferenceData.TradeChoices, T);

    private IReadOnlyList<ChoiceItem> ServiceTiles => ChoiceItem.From(ReferenceData.ServicesOf(Draft.CategoryId), T);

    private static IReadOnlyCollection<string> Selected(string? id) => id is null ? [] : [id];

    private void PickTrade(string? categoryId) => PostReview.PickTrade(Draft, categoryId);

    private void PickService(string serviceId) => Draft.ServiceId = serviceId;

    private void PickTown(string? cityId) => PostReview.PickTown(Draft, cityId);
}
