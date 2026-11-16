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
        var content = new ByteArrayContent(photo);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        using var response = await api.SendAsync(HttpMethod.Post, ApiMapping.ToSubmitPath(badge), content);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return BadgeSubmitResult.AlreadySent;
        }

        await api.EnsureWrittenAsync(response, "send a badge proof");
        return BadgeSubmitResult.Sent;
    }
}
