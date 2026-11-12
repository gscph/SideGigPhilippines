using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IReviewRepository"/> on SideGigPh.Api (<c>reviews</c>). The server allows a review only from a Seeker who
/// chatted with the Skilled Worker, and never lets a worker delete the reviews about them.
/// </summary>
public sealed class ApiReviewRepository(ApiClient api) : IReviewRepository
{
    public Task<IReadOnlyList<Review>> GetForProfessionalAsync(string professionalId) =>
        ListAsync($"reviews?professionalId={ApiClient.Escape(professionalId)}", $"the reviews of {professionalId}");

    public Task<IReadOnlyList<Review>> GetBySeekerAsync(string seekerId) =>
        ListAsync($"reviews?seekerId={ApiClient.Escape(seekerId)}", $"the reviews written by {seekerId}");

    public async Task SaveAsync(Review review)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, "reviews", review.ToSaveRequest(), ApiJson.Default.SaveReviewRequest);
        await api.EnsureWrittenAsync(response, $"save the review of {review.ProfessionalId}");
    }

    /// <summary>Deletes the reviews this person wrote. The ones about them stay (see the server's ReviewStore).</summary>
    public async Task DeleteForPersonAsync(string personId)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, "reviews/mine");
        await api.EnsureWrittenAsync(response, "delete my reviews");
    }

    private Task<IReadOnlyList<Review>> ListAsync(string path, string what) => api.ReadAsync<IReadOnlyList<Review>>(async () =>
        (await api.GetAsync(path, ApiJson.Default.ListReviewDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], what);
}
