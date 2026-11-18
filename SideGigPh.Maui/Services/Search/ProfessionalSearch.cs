using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Search;

/// <summary>
/// Runs a Seeker's search a page at a time: the server ranks the trade's workers from every town in the Seeker's area
/// (their town first, then nearby towns; budget, stars, experience, photos) and sends <c>Paging.Search</c> at a time.
/// Free text is turned into a request by <see cref="SearchIntentParser"/> first.
/// </summary>
public sealed class ProfessionalSearch(IProfessionalProfileRepository profiles)
{
    /// <summary>The first page of results.</summary>
    public Task<SearchResult> SearchAsync(SearchRequest request) => PageAsync(request, SearchResult.Empty);

    /// <summary><paramref name="shown"/> with the next page added under it, or as it is when there is no more.</summary>
    public Task<SearchResult> MoreAsync(SearchRequest request, SearchResult shown) =>
        shown.HasMore ? PageAsync(request, shown) : Task.FromResult(shown);

    private async Task<SearchResult> PageAsync(SearchRequest request, SearchResult shown)
    {
        if (request.CategoryId is not { } categoryId)
        {
            return SearchResult.Empty;
        }

        var query = new ProfessionalSearchQuery(
            categoryId, ReferenceData.AreaOf(request.CityId), request.ServiceId, request.CityId, request.BudgetId,
            OtherTextFor(request), request.Order);
        var page = await profiles.SearchAsync(query, shown.Count);

        // A page that didn't load (offline) leaves what is shown; scrolling again tries again.
        return shown.Count == 0 ? page : page.Total == 0 ? shown : shown.Append(page);
    }

    // What they typed under "Others", sent only when searching an "Others" job (it orders workers who typed alike first).
    private static string? OtherTextFor(SearchRequest request) =>
        ReferenceData.IsOtherJob(request.ServiceId) && request.OtherText?.Trim() is { Length: > 0 } typed ? typed : null;
}
