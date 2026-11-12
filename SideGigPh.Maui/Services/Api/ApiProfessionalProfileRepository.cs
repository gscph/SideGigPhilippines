using System.Net.Http.Json;
using System.Net;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IProfessionalProfileRepository"/> on SideGigPh.Api (<c>professionals</c>). The listing id is the caller's
/// uid and its number the account's verified number: the server takes both from the sign-in token.
/// <para>
/// Listings change rarely, so answers are kept a while: a search for <see cref="SearchFreshFor"/> (changing the
/// Seeker's town, budget or job in the same area ranks the same list again, with no request), a listing for
/// <see cref="ListingFreshFor"/> (the Inbox, the public profile and chats reuse it). Publishing or removing drops both.
/// </para>
/// </summary>
public sealed class ApiProfessionalProfileRepository(ApiClient api) : IProfessionalProfileRepository
{
    public static readonly TimeSpan SearchFreshFor = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan ListingFreshFor = TimeSpan.FromMinutes(5);

    private readonly TimedCache<IReadOnlyList<ProfessionalListing>> _searches = new(SearchFreshFor);
    private readonly TimedCache<ProfessionalListing> _listings = new(ListingFreshFor);

    public async Task<bool> HasProfileAsync(string uid) => await GetByIdAsync(uid) is not null;

    public Task<IReadOnlyList<ProfessionalListing>> SearchAsync(ProfessionalSearchQuery query)
    {
        var key = $"{query.CategoryId}|{query.AreaId}";
        if (_searches.TryGet(key, out var cached))
        {
            return Task.FromResult(cached);
        }

        return api.ReadAsync<IReadOnlyList<ProfessionalListing>>(async () =>
        {
            var path = $"professionals?categoryId={ApiClient.Escape(query.CategoryId)}";
            if (query.AreaId is not null)
            {
                path += $"&areaId={ApiClient.Escape(query.AreaId)}";
            }

            var listings = (await api.GetAsync(path, ApiJson.Default.ListListingDto) ?? []).Select(dto => dto.ToModel()).ToList();
            _searches.Set(key, listings);
            foreach (var listing in listings)
            {
                _listings.Set(listing.Id, listing);
            }

            return listings;
        }, [], $"pros in {query.CategoryId}/{query.AreaId}");
    }

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
        if (response.StatusCode == HttpStatusCode.Conflict
            && await ErrorOf(response) == ApiErrors.PhoneTaken)
        {
            return PublishResult.PhoneTaken;
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

    /// <summary>The worker's own listing changed: the next read of it, and every search, asks the server.</summary>
    private void Forget(string professionalId)
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
