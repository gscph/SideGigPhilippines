using System.Net;
using System.Net.Http.Headers;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IVerificationRepository"/> on SideGigPh.Api: <c>GET verifications/mine</c> and
/// <c>POST verifications/{kind}?consent=true…</c> with the photo as the body.
/// </summary>
public sealed class ApiVerificationRepository(ApiClient api) : IVerificationRepository
{
    public Task<IReadOnlyList<BadgeRequest>> GetMineAsync() => api.ReadAsync<IReadOnlyList<BadgeRequest>>(async () =>
        (await api.GetAsync("verifications/mine", ApiJson.Default.ListVerificationDto) ?? []).Select(dto => dto.ToModel()).OfType<BadgeRequest>().ToList(),
        [], "the worker's badges");

    public async Task<BadgeSubmitResult> SubmitAsync(Badge badge, byte[] photo)
    {
        using var response = await api.SendAsync(HttpMethod.Post, ApiMapping.ToSubmitPath(badge), photo, "image/jpeg");
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            // Sent once more after a lost answer: the "already sent" is the first one, which went through.
            return ApiClient.WasRepeated(response) ? BadgeSubmitResult.Sent : BadgeSubmitResult.AlreadySent;
        }

        await api.EnsureWrittenAsync(response, "send a badge proof");
        return BadgeSubmitResult.Sent;
    }
}
