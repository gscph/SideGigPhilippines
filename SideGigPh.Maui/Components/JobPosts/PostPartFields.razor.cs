using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The fields of one change sheet on the new post page: What (picture tiles, two pages: the kinds of work, then, on
/// <see cref="JobsStep"/>, the picked trade's services), Where (town, barangay, "Use my
/// location"), When (a calendar from <see cref="Today"/>, or "Any day is fine") or Your budget (from / up to, the job's usual price as a guide). They change <see cref="Draft"/>
/// directly; the page keeps a <see cref="PostSnapshot"/> so Cancel puts the answers back.
/// </summary>
public partial class PostPartFields : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public JobPostDraft Draft { get; set; } = default!;

    [Parameter] public PostPart Part { get; set; }

    /// <summary>Today in the Philippines: the calendar's first day.</summary>
    [Parameter] public DateOnly Today { get; set; }

    /// <summary>The What sheet's second page: the picked trade's jobs instead of the kinds of work.</summary>
    [Parameter] public bool JobsStep { get; set; }

    /// <summary>A kind of work was tapped (the page opens its jobs when it has some, <see cref="PostReview.HasJobChoice"/>).</summary>
    [Parameter] public EventCallback OnTradePicked { get; set; }

    /// <summary>A job was tapped (the page redraws, so its Save turns on).</summary>
    [Parameter] public EventCallback OnJobPicked { get; set; }

    private string? BudgetGuide => Pricing.GuideNote(Draft.ServiceId, Draft.CategoryId) is { } note ? T.Get(note) : null;

    private bool BudgetTopTooLow => Draft.BudgetMinPesos is { } from && Draft.BudgetPesos is { } upTo && upTo < from;

    private IReadOnlyList<ChoiceItem> TradeTiles => ChoiceItem.From(ReferenceData.TradeChoices, T);

    private IReadOnlyList<ChoiceItem> ServiceTiles => ChoiceItem.From(ReferenceData.ServicesOf(Draft.CategoryId), T);

    private static IReadOnlyCollection<string> Selected(string? id) => id is null ? [] : [id];

    private Task PickTradeAsync(string? categoryId)
    {
        PostReview.PickTrade(Draft, categoryId);
        return OnTradePicked.InvokeAsync();
    }

    private Task PickServiceAsync(string serviceId)
    {
        Draft.ServiceId = serviceId;
        return OnJobPicked.InvokeAsync();
    }

    private void PickTown(string? cityId) => PostReview.PickTown(Draft, cityId);

    private DateTime? PickedDate => Draft.NeededOn?.ToDateTime(TimeOnly.MinValue);

    private DateTime FirstDate => Today.ToDateTime(TimeOnly.MinValue);

    private DateTime LastDate => PostDay.LastDay(Today).ToDateTime(TimeOnly.MinValue);

    private void PickDay(DateTime? date) => Draft.NeededOn = date is { } d ? DateOnly.FromDateTime(d) : null;

    private void AnyDay() => Draft.NeededOn = null;
}
