using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>The answers every function shares, with the error codes in <see cref="ApiErrors"/>.</summary>
internal static class ApiResults
{
    /// <summary>
    /// While building the MVP, error answers carry <see cref="ApiError.Detail"/> (exception + stack trace, or what
    /// was wrong with the request). Turn this off before launch: it shows the server's internals to any caller.
    /// </summary>
    public const bool ShowDetails = true;

    public static IActionResult Conflict(string error) => new ConflictObjectResult(new ApiError(error));

    public static IActionResult Forbidden(string error) => new ObjectResult(new ApiError(error)) { StatusCode = StatusCodes.Status403Forbidden };

    public static IActionResult BadRequest(string error = "invalid", string? detail = null) =>
        new BadRequestObjectResult(new ApiError(error, ShowDetails ? detail : null));

    /// <summary>
    /// The JSON body, or null when it's empty (<c>null</c>). A body that doesn't parse throws
    /// <see cref="BadBodyException"/>, which <see cref="ErrorDetailsMiddleware"/> answers as <c>400 bad-body</c>.
    /// </summary>
    public static async Task<T?> ReadAsync<T>(HttpRequest request, CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await request.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
        {
            throw new BadBodyException(
                $"Couldn't read the body as {typeof(T).Name} ({request.ContentType}, {request.ContentLength?.ToString() ?? "no"} Content-Length).", ex);
        }
    }
}

/// <summary>The request body is missing or isn't the expected JSON.</summary>
internal sealed class BadBodyException(string message, Exception inner) : Exception(message, inner);
