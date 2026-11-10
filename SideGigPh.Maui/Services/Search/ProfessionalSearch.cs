using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Search;

/// <summary>
/// Runs a Seeker's search: fetches the category's pros from every town (so pros from nearby
/// towns can follow the local ones), adds their star ratings, and ranks them with
/// <see cref="SearchRanker"/>. Free text is turned into a request by <see cref="SearchIntentParser"/> first.
/// </summary>
public sealed class ProfessionalSearch(IProfessionalProfileRepository profiles, IReviewRepository reviews)
{
    public async Task<SearchResult> SearchAsync(SearchRequest request)
    {
        if (request.CategoryId is not { } categoryId)
        {
            return SearchResult.Empty;
        }

        var pros = await profiles.SearchAsync(new ProfessionalSearchQuery(categoryId, null));
        var summaries = await reviews.GetSummariesAsync(pros.Select(pro => pro.Id).ToList());
        return SearchRanker.Rank(pros, request, summaries);
    }
}
