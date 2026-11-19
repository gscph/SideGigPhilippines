using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>The parts of a new post the household changes on their own sheet (the card's rows).</summary>
public enum PostPart
{
    What,
    Where,
    When,
    Budget,
}

/// <summary>The answers a change sheet can touch, kept so Cancel puts them back.</summary>
public sealed record PostSnapshot(
    string? CategoryId,
    string? ServiceId,
    string? CityId,
    string? BarangayId,
    GeoPoint? Point,
    int? BudgetMinPesos,
    int? BudgetPesos,
    DateOnly? NeededOn);

/// <summary>
/// The post form laid out like Airbnb's "Review and continue": one card of rows (What / Where / When / Your budget), each with
/// a gray Change (or Add) button opening its own full-screen sheet with Cancel and Save. Each sheet has its own URL under
/// the form (<c>/posts/new/what</c>, <c>/posts/new/what/{trade}</c> for its jobs, <c>/where</c>, <c>/when</c>, <c>/budget</c>; the same
/// under <c>/posts/{id}/edit</c>), so the phone's Back closes the sheet or goes back one page instead of leaving the form.
/// Nothing here talks to the server; <see cref="JobBoard"/> still decides what can be posted.
/// </summary>
public static class PostReview
{
    /// <summary>A sheet's part of the URL ("what", "where", "when", "budget").</summary>
    public static string Slug(PostPart part) => part switch
    {
        PostPart.What => "what",
        PostPart.Where => "where",
        PostPart.When => "when",
        _ => "budget",
    };

    /// <summary>The sheet a URL part names, or null (the form itself, or a word it doesn't know).</summary>
    public static PostPart? FromSlug(string? slug)
    {
        foreach (var part in Enum.GetValues<PostPart>())
        {
            if (string.Equals(Slug(part), slug, StringComparison.OrdinalIgnoreCase))
            {
                return part;
            }
        }

        return null;
    }

    /// <summary>
    /// How many pages back the form is from a sheet: one, or two from the What sheet's jobs page (the trades page sits
    /// between), so Save and Cancel land on the form and the phone's Back after it leaves the form as before.
    /// </summary>
    public static int SheetDepth(string? jobsOfTrade) => jobsOfTrade is null ? 1 : 2;

    /// <summary>The card row (sheet) a missing field is changed on, or null for the problem, typed on the form itself.</summary>
    public static PostPart? PartOf(JobPostField field) => field switch
    {
        JobPostField.Trade or JobPostField.Service => PostPart.What,
        JobPostField.Town => PostPart.Where,
        JobPostField.When => PostPart.When,
        JobPostField.Budget => PostPart.Budget,
        _ => null,
    };

    /// <summary>What is still missing on one row of the card (its note says what to do), or null when the row is done.</summary>
    /// <param name="today">The Philippines' date (<see cref="PostDay.Today"/>); null: the phone's clock now.</param>
    public static JobPostField? MissingIn(JobPostDraft draft, PostPart part, DateOnly? today = null) =>
        JobBoard.Missing(draft, today).Cast<JobPostField?>().FirstOrDefault(field => PartOf(field!.Value) == part);

    public static PostSnapshot Take(JobPostDraft draft) => new(
        draft.CategoryId, draft.ServiceId, draft.CityId, draft.BarangayId, draft.Point, draft.BudgetMinPesos, draft.BudgetPesos, draft.NeededOn);

    /// <summary>Cancel on a sheet: the answers as they were when it opened.</summary>
    public static void Restore(JobPostDraft draft, PostSnapshot snapshot)
    {
        draft.CategoryId = snapshot.CategoryId;
        draft.ServiceId = snapshot.ServiceId;
        draft.CityId = snapshot.CityId;
        draft.BarangayId = snapshot.BarangayId;
        draft.Point = snapshot.Point;
        draft.BudgetMinPesos = snapshot.BudgetMinPesos;
        draft.BudgetPesos = snapshot.BudgetPesos;
        draft.NeededOn = snapshot.NeededOn;
    }

    /// <summary>True when the row has an answer (its button says Change), false when it is still empty (Add).</summary>
    public static bool Has(JobPostDraft draft, PostPart part) => part switch
    {
        PostPart.What => draft.ServiceId is not null,
        PostPart.Where => draft.CityId is not null,
        PostPart.When => draft.NeededOn is not null,
        _ => draft.BudgetPesos is not null,
    };

    /// <summary>
    /// The What sheet is two pages: the kinds of work, then (after a tap) that trade's jobs, with ← back to the trades.
    /// True when <paramref name="categoryId"/> has jobs to choose from, so a tap on it opens its jobs page; "Others"
    /// has one job, itself, picked with the trade.
    /// </summary>
    public static bool HasJobChoice(string? categoryId) => ReferenceData.ServicesOf(categoryId).Count > 1;

    /// <summary>Save on a sheet: the What sheet only once a job is picked (a trade alone can't be posted).</summary>
    public static bool CanSave(JobPostDraft draft, PostPart part) => part != PostPart.What || Has(draft, PostPart.What);

    /// <summary>Another trade has other services: the picked service is cleared. A trade with one service ("Others") picks it.</summary>
    public static void PickTrade(JobPostDraft draft, string? categoryId)
    {
        if (draft.CategoryId != categoryId)
        {
            draft.CategoryId = categoryId;
            draft.ServiceId = ReferenceData.ServicesOf(categoryId) is [var only] ? only.Id : null;
        }
    }

    /// <summary>Another town has other barangays: the picked one is cleared.</summary>
    public static void PickTown(JobPostDraft draft, string? cityId)
    {
        draft.CityId = cityId;
        draft.BarangayId = ReferenceData.FindBarangay(cityId, draft.BarangayId)?.Id;
    }
}
