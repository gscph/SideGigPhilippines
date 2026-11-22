using System.Net;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// <see cref="IErrorReportRepository"/> through SideGigPh.Api (<c>POST error-reports</c>). Unlike the other writes it
/// doesn't use <see cref="ApiClient.EnsureWrittenAsync"/>, which logs an error: a failed send would record a report.
/// </summary>
public sealed class ApiErrorReportRepository(ApiClient api) : IErrorReportRepository
{
    public async Task<bool> SendAsync(string installId, DeviceStamp device, IReadOnlyList<ErrorReport> reports)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Post, "error-reports", reports.ToBatch(installId, device), ApiJson.Default.ErrorReportBatch,
            logFailure: false);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return false;
        }

        throw new HttpRequestException($"Error reports weren't sent: {(int)response.StatusCode} {response.ReasonPhrase}.", null, response.StatusCode);
    }
}
