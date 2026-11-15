using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary><see cref="ISafetyRepository"/> on SideGigPh.Api (<c>POST reports</c>, <c>PUT blocks/{uid}</c>).</summary>
public sealed class ApiSafetyRepository(ApiClient api) : ISafetyRepository
{
    public async Task ReportAsync(UserReport report)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, "reports", report.ToDto(), ApiJson.Default.ReportRequest);
        await api.EnsureWrittenAsync(response, "send the report");
    }

    public async Task BlockAsync(string personId)
    {
        using var response = await api.SendAsync(HttpMethod.Put, $"blocks/{ApiClient.Escape(personId)}");
        await api.EnsureWrittenAsync(response, "block the person");
    }
}
