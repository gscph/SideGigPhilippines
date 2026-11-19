using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>The words on the new post's review card and its change sheets.</summary>
public static class PostReviewText
{
    /// <summary>The service ("Aircon cleaning"), else the trade while no service is picked, else null.</summary>
    public static string? PostWhat(this AppText text, JobPostDraft draft) =>
        ReferenceData.FindService(draft.ServiceId) is { } service ? text.Get(service.Label)
        : ReferenceData.FindTrade(draft.CategoryId) is { } trade ? text.Get(trade.Label)
        : null;

    /// <summary>The trade under the service on the card's top ("Aircon Technician"), or null.</summary>
    public static string? PostTradeUnder(this AppText text, JobPostDraft draft) =>
        draft.ServiceId is not null && ReferenceData.FindTrade(draft.CategoryId) is { } trade ? text.Get(trade.Label) : null;

    /// <summary>"Angono · Brgy. San Isidro", or null before a town is picked.</summary>
    public static string? PostWhere(this AppText text, JobPostDraft draft) =>
        draft.CityId is { } cityId ? text.Place(cityId, draft.BarangayId) : null;

    /// <summary>"₱800 – ₱1,500" or "₱1,500", or null before an "up to" is typed.</summary>
    public static string? PostBudgetText(JobPostDraft draft) =>
        draft.BudgetPesos is { } upTo ? Pricing.PostBudget(draft.BudgetMinPesos, upTo) : null;

    /// <summary>The row's button: Change when it has an answer, Add when it is empty.</summary>
    public static string PostAction(this AppText text, JobPostDraft draft, PostPart part) =>
        text.Get(PostReview.Has(draft, part) ? Copy.RequestChange : Copy.PostAdd);

    /// <summary>A change sheet's title.</summary>
    public static string PostSheetTitle(this AppText text, PostPart part) => text.Get(part switch
    {
        PostPart.What => Copy.PostSheetWhat,
        PostPart.Where => Copy.PostSheetWhere,
        _ => Copy.PostBudget,
    });

    /// <summary>A row's note while it is missing ("Pick your town."), or null when the row is done.</summary>
    public static string? PostRowMissing(this AppText text, JobPostDraft draft, PostPart part) =>
        PostReview.MissingIn(draft, part) is { } field ? JobBoard.MissingText(field, text) : null;

    /// <summary>Under the Post button: "Still needed: the service, your budget", or null once it can be posted.</summary>
    public static string? PostStillNeeded(this AppText text, JobPostDraft draft) =>
        JobBoard.Missing(draft) is { Count: > 0 } missing
            ? text.Format(Copy.PostStillNeeded, string.Join(", ", missing.Select(field => text.Get(field switch
            {
                JobPostField.Trade or JobPostField.Service => Copy.PostNeedService,
                JobPostField.Town => Copy.PostNeedTown,
                JobPostField.Budget => Copy.PostNeedBudget,
                _ => Copy.PostNeedProblem,
            }))))
            : null;
}
