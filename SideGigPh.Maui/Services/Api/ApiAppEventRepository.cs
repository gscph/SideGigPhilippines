using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IAppEventRepository"/> through SideGigPh.Api (<c>POST events</c>, answered <c>202</c>). A failed upload is
/// not logged by <see cref="ApiClient"/> (it would become an error report every time the phone is offline); the server
/// keeps each event id once, so sending a batch again is safe.
/// </summary>
public sealed class ApiAppEventRepository(ApiClient api) : IAppEventRepository
{
    public async Task<bool> SendAsync(IReadOnlyList<PendingEvent> events)
    {
        var batch = events.Select(e => new AppEventDto(e.EventId, e.Name, e.SessionId, e.ServiceCategory, e.Area, e.SearchTerm,
            e.ResultCount, e.ClientTs, e.AppVersion)).ToList();
        using var response = await api.SendJsonAsync(HttpMethod.Post, "events", batch, ApiJson.Default.ListAppEventDto, logFailure: false);
        return ApiClient.BatchTaken(response, "App events");
    }
}
