using System.Net.Http.Json;
using System.Net;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IProfessionalProfileRepository"/> on SideGigPh.Api (<c>professionals</c>). The listing id is the caller's
/// uid and its number the account's verified number: the server takes both from the sign-in token.
/// </summary>
public sealed class ApiProfessionalProfileRepository(ApiClient api) : IProfessionalProfileRepository
{
    public async Task<bool> HasProfileAsync(string uid) => await GetByIdAsync(uid) is not null;

    public Task<IReadOnlyList<ProfessionalListing>> SearchAsync(ProfessionalSearchQuery query) => api.ReadAsync<IReadOnlyList<ProfessionalListing>>(async () =>
    {
        var path = $"professionals?categoryId={ApiClient.Escape(query.CategoryId)}";
        if (query.AreaId is not null)
        {
            path += $"&areaId={ApiClient.Escape(query.AreaId)}";
        }

        var listings = await api.GetAsync(path, ApiJson.Default.ListListingDto) ?? [];
        return listings.Select(dto => dto.ToModel()).ToList();
    }, [], $"pros in {query.CategoryId}/{query.AreaId}");

    public Task<ProfessionalListing?> GetByIdAsync(string professionalId) => api.ReadAsync(async () =>
        (await api.GetAsync($"professionals/{ApiClient.Escape(professionalId)}", ApiJson.Default.ListingDto))?.ToModel(),
        null, $"the listing of {professionalId}");

    public async Task<PublishResult> PublishAsync(ProfessionalListing listing)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, "professionals/me", listing.ToPublishRequest(), ApiJson.Default.PublishListingRequest);
        if (response.StatusCode == HttpStatusCode.Conflict
            && await ErrorOf(response) == ApiErrors.PhoneTaken)
        {
            return PublishResult.PhoneTaken;
        }

        api.EnsureWritten(response, "publish the listing");
        return PublishResult.Published;
    }

    public async Task RemoveAsync(string professionalId)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, "professionals/me");
        api.EnsureWritten(response, "remove the listing");
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
