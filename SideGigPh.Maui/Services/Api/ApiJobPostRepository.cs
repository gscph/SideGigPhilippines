using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary><see cref="IJobPostRepository"/> on SideGigPh.Api (<c>job-posts</c>): posting, the household's own posts, the worker's feed.</summary>
public sealed class ApiJobPostRepository(ApiClient api) : IJobPostRepository
{
    // A read that failed says nothing about the listing: assume there is one, so the page doesn't ask to publish.
    private static readonly JobPostFeed Empty = new([], HasMore: false, HasListing: true);

    public async Task<JobPost> AddAsync(JobPostDraft draft)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, "job-posts", draft.ToRequest(), ApiJson.Default.NewJobPostRequest);
        await api.EnsureWrittenAsync(response, "post the work needed");
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.JobPostDto))!.ToModel();
    }

    public Task<IReadOnlyList<JobPost>> GetMineAsync() => api.ReadAsync<IReadOnlyList<JobPost>>(async () =>
        (await api.GetAsync("job-posts/mine", ApiJson.Default.ListJobPostDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], "this person's job posts");

    public async Task CloseAsync(long postId)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, $"job-posts/{postId}");
        await api.EnsureWrittenAsync(response, "take the job post down");
    }

    public Task<JobPostFeed> GetFeedAsync(int skip) => api.ReadAsync(async () =>
        (await api.GetAsync($"job-posts/feed?skip={skip}&take={Paging.JobPosts}", ApiJson.Default.JobPostFeedDto))?.ToModel() ?? Empty,
        Empty, "the job posts nearby");

    public async Task ReportAsync(long postId)
    {
        using var response = await api.SendAsync(HttpMethod.Post, $"job-posts/{postId}/report");
        await api.EnsureWrittenAsync(response, "report the job post");
    }
}
