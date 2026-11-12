using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The answers every function shares, with the error codes in <see cref="ApiErrors"/>.</summary>
internal static class ApiResults
{
    public static IActionResult Conflict(string error) => new ConflictObjectResult(new ApiError(error));

    public static IActionResult Forbidden(string error) => new ObjectResult(new ApiError(error)) { StatusCode = StatusCodes.Status403Forbidden };

    public static IActionResult BadRequest(string error = "invalid") => new BadRequestObjectResult(new ApiError(error));

    /// <summary>The JSON body, or null when it's missing or doesn't parse.</summary>
    public static async Task<T?> ReadAsync<T>(HttpRequest request, CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await request.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}
