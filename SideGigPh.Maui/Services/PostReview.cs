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
/// a gray Change (or Add) button opening its own full-screen sheet with Cancel and Save. Nothing here talks to the server;
/// <see cref="JobBoard"/> still decides what can be posted.
/// </summary>
public static class PostReview
{
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
