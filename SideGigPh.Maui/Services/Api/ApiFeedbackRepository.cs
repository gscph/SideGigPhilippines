using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary><see cref="IFeedbackRepository"/> on SideGigPh.Api (<c>feedback</c>): the board, look-alikes, posting, votes.</summary>
public sealed class ApiFeedbackRepository(ApiClient api) : IFeedbackRepository
{
    public Task<FeedbackPage> GetPageAsync(int skip) => api.ReadAsync(async () =>
        (await api.GetAsync($"feedback?skip={skip}&take={Paging.Feedback}", ApiJson.Default.FeedbackPageDto))?.ToModel() ?? Empty,
        Empty, "the feedback board");

    public Task<IReadOnlyList<FeedbackPost>> FindSimilarAsync(string text) => api.ReadAsync<IReadOnlyList<FeedbackPost>>(async () =>
        (await api.GetAsync($"feedback/similar?q={ApiClient.Escape(text)}", ApiJson.Default.ListFeedbackPostDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], "suggestions like this one");

    public async Task<FeedbackPost> AddAsync(string title, string? details, UserRole? role)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, "feedback", ApiMapping.ToFeedbackRequest(title, details, role), ApiJson.Default.NewFeedbackRequest,
            repeatable: false); // a repeat would post it twice
        await api.EnsureWrittenAsync(response, "post the suggestion");
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.FeedbackPostDto))!.ToModel();
    }

    public async Task<int> VoteAsync(long postId, bool on)
    {
        using var response = await api.SendAsync(on ? HttpMethod.Put : HttpMethod.Delete, $"feedback/{postId}/vote");
        await api.EnsureWrittenAsync(response, on ? "vote for the suggestion" : "take the vote back");
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.FeedbackVoteDto))!.Votes;
    }

    public async Task RemoveAsync(long postId)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, $"feedback/{postId}");
        await api.EnsureWrittenAsync(response, "remove the suggestion");
    }

    public async Task ReportAsync(long postId)
    {
        using var response = await api.SendAsync(HttpMethod.Post, $"feedback/{postId}/report");
        await api.EnsureWrittenAsync(response, "report the suggestion");
    }

    private static readonly FeedbackPage Empty = new([], HasMore: false);
}
