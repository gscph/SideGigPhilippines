using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// The app's config from SideGigPh.Api (<c>GET config</c>, the skeleton's <see cref="IConfigSource{T}"/>; downloaded
/// only when its ETag changed) and the "Others" notes (<c>POST other-jobs</c>).
/// </summary>
public sealed class ApiConfigRepository(ApiClient api) : IConfigSource<AppConfig>, IOtherJobNoteRepository
{
    public Task<ConfigFetch<AppConfig>?> FetchAsync(string? savedVersion, CancellationToken cancellationToken) =>
        api.ReadAsync<ConfigFetch<AppConfig>?>(async () =>
        {
            var (dto, version) = await api.GetIfChangedAsync("config", savedVersion, ApiJson.Default.ConfigDto, cancellationToken);
            return new ConfigFetch<AppConfig>(dto?.ToModel(), version);
        }, null, "the app config");

    public async Task SaveAsync(OtherJobNote note)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, "other-jobs", note.ToRequest(), ApiJson.Default.OtherJobRequest);
        await api.EnsureWrittenAsync(response, $"note an Others job in {note.CategoryId}");
    }
}
