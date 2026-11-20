using MudBlazor;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Services;

/// <summary>
/// Turns a MudBlazor <see cref="GridState{T}"/> into an admin list's query string (<c>skip</c>, <c>take</c>, <c>sort</c>,
/// <c>desc</c>, <c>search</c> and the list's filter), so every grid pages and sorts on the server the same way.
/// </summary>
public static class GridQueryExtensions
{
    /// <summary>
    /// The query string for <paramref name="state"/>'s page. <paramref name="sorts"/> maps a column's property name
    /// (<see cref="SortDefinition{T}.SortBy"/>) to the API's sort word (<see cref="AdminLists"/>); a column it doesn't
    /// list sorts by the API's default (newest first). Empty filters are left out.
    /// </summary>
    public static string ToAdminQuery<T>(this GridState<T> state, string? search, IReadOnlyDictionary<string, string> sorts,
        params (string Name, string? Value)[] filters)
    {
        var parts = new List<string>
        {
            $"skip={Math.Max(0, state.Page) * Math.Max(1, state.PageSize)}",
            $"take={Math.Clamp(state.PageSize, 1, AdminLists.MaxTake)}",
        };

        if (state.SortDefinitions.FirstOrDefault() is { } sort && sorts.TryGetValue(sort.SortBy, out var by))
        {
            parts.Add($"sort={by}");
            parts.Add($"desc={(sort.Descending ? "true" : "false")}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        parts.AddRange(filters.Where(f => !string.IsNullOrWhiteSpace(f.Value)).Select(f => $"{f.Name}={Uri.EscapeDataString(f.Value!)}"));
        return string.Join('&', parts);
    }

    public static GridData<T> ToGridData<T>(this IEnumerable<T> items, int total) => new() { Items = items, TotalItems = total };
}
