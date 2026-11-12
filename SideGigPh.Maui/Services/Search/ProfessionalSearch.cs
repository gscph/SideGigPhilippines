using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Search;

/// <summary>
/// Runs a Seeker's search: fetches the category's pros from every town in the Seeker's area (so pros
/// from nearby towns can follow the local ones), each with their star rating, and ranks them with
/// <see cref="SearchRanker"/>. Free text is turned into a request by <see cref="SearchIntentParser"/> first.
/// </summary>
public sealed class ProfessionalSearch(IProfessionalProfileRepository profiles)
{
    public async Task<SearchResult> SearchAsync(SearchRequest request)
    {
        if (request.CategoryId is not { } categoryId)
        {
            return SearchResult.Empty;
        }

        var pros = await profiles.SearchAsync(new ProfessionalSearchQuery(categoryId, ReferenceData.AreaOf(request.CityId)));
        return SearchRanker.Rank(pros, request);
    }
}
