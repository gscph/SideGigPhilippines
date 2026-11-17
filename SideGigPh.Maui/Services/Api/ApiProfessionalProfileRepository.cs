using System.Net.Http.Json;
using System.Net;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IProfessionalProfileRepository"/> on SideGigPh.Api (<c>professionals</c>). The listing id is the caller's
/// uid and its number the account's verified number: the server takes both from the sign-in token.
/// <para>
/// Listings change rarely, so answers are kept a while: a page of search results for <see cref="SearchFreshFor"/> (the
/// server ranks them, so each query and page is its own request; Back to an earlier search reuses it), a listing for
/// <see cref="ListingFreshFor"/> (the Inbox, the public profile and chats reuse it). Publishing or removing drops both.
/// </para>
/// </summary>
public sealed class ApiProfessionalProfileRepository(ApiClient api) : IProfessionalProfileRepository
{
    public static readonly TimeSpan SearchFreshFor = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan ListingFreshFor = TimeSpan.FromMinutes(5);

    private readonly TimedCache<SearchResult> _searches = new(SearchFreshFor);
    private readonly TimedCache<ProfessionalListing> _listings = new(ListingFreshFor);

    public Task<ProfessionalDraft?> GetOwnDraftAsync(string uid) =>
        api.ReadAsync(async () =>
        {
            // Not from the kept listings: those carry no photo ids, and the restore needs the server's latest.
            if (await api.GetAsync($"professionals/{ApiClient.Escape(uid)}", ApiJson.Default.ListingDto) is not { } listing)
            {
                return null;
            }

            // Both or nothing: a listing restored without its references couldn't be re-published after an edit.
            var references = await api.GetAsync("professionals/me/references", ApiJson.Default.ListReferenceDto)
                ?? throw new HttpRequestException("The references of a live listing were not found.");
            return listing.ToDraft(references);
        }, null, $"the own listing of {uid}");

    public Task<SearchResult> SearchAsync(ProfessionalSearchQuery query, int skip)
    {
        var path = $"search/professionals?categoryId={ApiClient.Escape(query.CategoryId)}"
            + Param("areaId", query.AreaId) + Param("serviceId", query.ServiceId)
            + Param("cityId", query.CityId) + Param("budgetId", query.BudgetId) + Param("otherText", query.OtherText)
            + $"&skip={skip}&take={Paging.Search}";
        if (_searches.TryGet(path, out var cached))
        {
            return Task.FromResult(cached);
        }

        return api.ReadAsync(async () =>
        {
            if (await api.GetAsync(path, ApiJson.Default.SearchPageDto) is not { } page)
            {
                return SearchResult.Empty;
            }

            var result = page.ToModel(skip);
            _searches.Set(path, result);
            foreach (var hit in result.InTown.Concat(result.OtherTowns))
            {
                _listings.Set(hit.Pro.Id, hit.Pro);
            }

            return result;
        }, SearchResult.Empty, $"pros in {query.CategoryId}/{query.AreaId} from {skip}");
    }

    private static string Param(string name, string? value) => value is null ? string.Empty : $"&{name}={ApiClient.Escape(value)}";

    public Task<ProfessionalListing?> GetByIdAsync(string professionalId)
    {
        if (_listings.TryGet(professionalId, out var cached))
        {
            return Task.FromResult<ProfessionalListing?>(cached);
        }

        return api.ReadAsync(async () =>
        {
            // A missing listing isn't kept: it may be published any moment.
            var listing = (await api.GetAsync($"professionals/{ApiClient.Escape(professionalId)}", ApiJson.Default.ListingDto))?.ToModel();
            if (listing is not null)
            {
                _listings.Set(professionalId, listing);
            }

            return listing;
        }, null, $"the listing of {professionalId}");
    }

    public async Task<PublishResult> PublishAsync(ProfessionalListing listing, IReadOnlyList<WorkReference> references)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, "professionals/me",
            listing.ToPublishRequest(references), ApiJson.Default.PublishListingRequest);
        Forget(listing.Id);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var error = await ErrorOf(response);
            if (error == ApiErrors.PhoneTaken)
            {
                return PublishResult.PhoneTaken;
            }

            if (error == ApiErrors.PhotoInUse)
            {
                return PublishResult.PhotoInUse;
            }
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return PublishResult.TooManyTries;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest
            && await ErrorOf(response) == ApiErrors.Invalid("references"))
        {
            return PublishResult.BadReference;
        }

        await api.EnsureWrittenAsync(response, "publish the listing");
        return PublishResult.Published;
    }

    public async Task RemoveAsync(string professionalId)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, "professionals/me");
        Forget(professionalId);
        await api.EnsureWrittenAsync(response, "remove the listing");
    }

    /// <summary>The listing changed or was blocked: the next read of it, and every search, asks the server.</summary>
    public void Forget(string professionalId)
    {
        _listings.Remove(professionalId);
        _searches.Clear();
    }

    private static async Task<string?> ErrorOf(HttpResponseMessage response)
    {
        try
        {
            return (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))?.Error;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
