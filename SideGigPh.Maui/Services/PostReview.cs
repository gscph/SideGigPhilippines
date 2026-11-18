using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>The parts of a new post the household changes on their own sheet (the card's rows).</summary>
public enum PostPart
{
    What,
    Where,
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
    int? BudgetPesos);

/// <summary>
/// The post form laid out like Airbnb's "Review and continue": one card of rows (What / Where / Your budget), each with
/// a gray Change (or Add) button opening its own full-screen sheet with Cancel and Save. Nothing here talks to the server;
/// <see cref="JobBoard"/> still decides what can be posted.
/// </summary>
public static class PostReview
{
    public static PostSnapshot Take(JobPostDraft draft) => new(
        draft.CategoryId, draft.ServiceId, draft.CityId, draft.BarangayId, draft.Point, draft.BudgetMinPesos, draft.BudgetPesos);

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
    }

    /// <summary>True when the row has an answer (its button says Change), false when it is still empty (Add).</summary>
    public static bool Has(JobPostDraft draft, PostPart part) => part switch
    {
        PostPart.What => draft.ServiceId is not null,
        PostPart.Where => draft.CityId is not null,
        _ => draft.BudgetPesos is not null,
    };

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
