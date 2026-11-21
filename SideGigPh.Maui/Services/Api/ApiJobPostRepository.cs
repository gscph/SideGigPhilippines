using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary><see cref="IJobPostRepository"/> on SideGigPh.Api (<c>job-posts</c>): posting, the household's own posts, the worker's feed, offers.</summary>
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

    public async Task<JobPost> UpdateAsync(long postId, JobPostDraft draft)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, $"job-posts/{postId}", draft.ToRequest(), ApiJson.Default.NewJobPostRequest);
        await api.EnsureWrittenAsync(response, "change the job post");
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

    public async Task SendOfferAsync(long postId, int pricePesos, string? note)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, $"job-posts/{postId}/offers", new SendOfferRequest(pricePesos, note), ApiJson.Default.SendOfferRequest);
        await api.EnsureWrittenAsync(response, "send the offer");
    }

    public async Task WithdrawOfferAsync(long postId)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, $"job-posts/{postId}/offers/mine");
        await api.EnsureWrittenAsync(response, "take the offer back");
    }

    public Task<IReadOnlyList<JobOffer>> GetOffersAsync(long postId) => api.ReadAsync<IReadOnlyList<JobOffer>>(async () =>
        (await api.GetAsync($"job-posts/{postId}/offers", ApiJson.Default.ListJobOfferDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], "the offers on a job post");

    public async Task<JobOffer> PickOfferAsync(long postId, long offerId)
    {
        using var response = await api.SendAsync(HttpMethod.Post, $"job-posts/{postId}/offers/{offerId}/pick");
        await api.EnsureWrittenAsync(response, "pick the offer");
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.JobOfferDto))!.ToModel();
    }

    public Task<string?> GetContactNumberAsync(string otherUid) => api.ReadAsync<string?>(async () =>
        (await api.GetAsync($"contacts/{Uri.EscapeDataString(otherUid)}", ApiJson.Default.ContactDto))?.PhoneNumber,
        null, "a contact number");

    public Task<IReadOnlyList<MyOffer>> GetMyOffersAsync() => api.ReadAsync<IReadOnlyList<MyOffer>>(async () =>
        (await api.GetAsync("offers/mine", ApiJson.Default.ListMyOfferDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], "the worker's offers");

    public Task<IReadOnlyList<PickedJob>> GetPickedJobsAsync(string otherUid) => api.ReadAsync<IReadOnlyList<PickedJob>>(async () =>
        (await api.GetAsync($"picked-jobs/{Uri.EscapeDataString(otherUid)}", ApiJson.Default.ListPickedJobDto) ?? []).Select(dto => dto.ToModel()).ToList(),
        [], "the picked jobs with someone");

    public async Task<PickedJob> MarkDoneAsync(long postId)
    {
        using var response = await api.SendAsync(HttpMethod.Post, $"picked-jobs/{postId}/done");
        await api.EnsureWrittenAsync(response, "mark the job done");
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.PickedJobDto))!.ToModel();
    }

    public async Task<PickedJob> ShareMeetLocationAsync(long postId, GeoPoint point)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, $"picked-jobs/{postId}/location",
            new ShareMeetLocationRequest(point.Latitude, point.Longitude), ApiJson.Default.ShareMeetLocationRequest);
        await api.EnsureWrittenAsync(response, "send the location");
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.PickedJobDto))!.ToModel();
    }

    public async Task CancelPickAsync(long postId, string reason, string? note)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, $"picked-jobs/{postId}/cancel",
            new CancelPickRequest(reason, note), ApiJson.Default.CancelPickRequest);
        await api.EnsureWrittenAsync(response, "cancel the picked job");
    }

    public async Task RateHouseholdAsync(string seekerUid, int rating, string? comment)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, "household-reviews",
            new SaveHouseholdReviewRequest(seekerUid, rating, comment), ApiJson.Default.SaveHouseholdReviewRequest);
        await api.EnsureWrittenAsync(response, "rate the customer");
    }
}
