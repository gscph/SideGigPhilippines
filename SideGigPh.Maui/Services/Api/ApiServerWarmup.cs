using Microsoft.Extensions.Logging;

namespace SideGigPh.Maui.Services.Api;

/// <summary><c>GET warmup</c> (see <see cref="ServerWarmup"/>). A failure is only a trail line: offline is normal.</summary>
public sealed class ApiServerWarmup(ApiClient api, ILogger<ApiServerWarmup> logger) : IServerWarmup
{
    public async Task WarmUpAsync()
    {
        try
        {
            using var response = await api.SendAsync(HttpMethod.Get, "warmup");
        }
        catch (Exception ex)
        {
            logger.LogInformation("Server warm-up didn't go through: {Error}", ex.GetType().Name);
        }
    }
}
