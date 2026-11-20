using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IBadgeReviewRepository"/> on SideGigPh.Api: <c>GET ops/me</c>, <c>GET ops/verifications</c>,
/// <c>GET ops/verifications/{id}/document</c> and <c>POST ops/verifications/{id}/decision</c>.
/// </summary>
public sealed class ApiBadgeReviewRepository(ApiClient api) : IBadgeReviewRepository
{
    public Task<bool> IsOperatorAsync() => api.ReadAsync(async () =>
        (await api.GetAsync("ops/me", ApiJson.Default.AdminStatusDto))?.IsOperator == true,
        false, "whether this account is an operator");

    public Task<IReadOnlyList<PendingBadge>> PendingAsync(int take) => api.ReadAsync<IReadOnlyList<PendingBadge>>(async () =>
        (await api.GetAsync($"ops/verifications?take={take}", ApiJson.Default.ListAdminVerificationDto) ?? [])
            .Select(dto => dto.ToModel()).OfType<PendingBadge>().ToList(),
        [], "the badge requests to check");

    public Task<byte[]?> DocumentAsync(long id) => api.ReadAsync<byte[]?>(async () =>
    {
        using var response = await api.SendAsync(HttpMethod.Get, $"ops/verifications/{id}/document");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }, null, "a badge proof photo");

    public async Task DecideAsync(long id, bool approve, string? reason)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, $"ops/verifications/{id}/decision",
            new VerificationDecisionRequest(approve, reason), ApiJson.Default.VerificationDecisionRequest);
        await api.EnsureWrittenAsync(response, "decide a badge request");
    }
}
