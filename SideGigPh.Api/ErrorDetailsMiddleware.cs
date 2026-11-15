using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Turns an exception from any function into a JSON <see cref="ApiError"/> instead of the host's empty 500:
/// <c>400 bad-body</c> for an unreadable body, <c>500 exception</c> for anything else, each with the full
/// exception and stack trace in <see cref="ApiError.Detail"/> while <see cref="ApiResults.ShowDetails"/> is on.
/// It also says which builds met: every answer carries this API's version (<see cref="ApiHeaders.ApiVersion"/>), and a
/// crash or a refusal (400, 403, 409, 429…) is logged with the app version that called (<see cref="ApiHeaders.AppVersion"/>).
/// </summary>
internal sealed class ErrorDetailsMiddleware(ILogger<ErrorDetailsMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is not null)
        {
            http.Response.Headers[ApiHeaders.ApiVersion] = ApiVersion.Current;
        }

        try
        {
            await next(context);
            if (http is not null && RefusalOf(context) is { } status)
            {
                logger.LogWarning("{Function} answered {Status} to app {AppVersion}: {Method} {Path}",
                    context.FunctionDefinition.Name, status, CallerOf(http), http.Request.Method, http.Request.Path);
            }
        }
        catch (Exception thrown)
        {
            if (http is null || http.Response.HasStarted)
            {
                throw;
            }

            var ex = thrown is AggregateException { InnerExceptions.Count: 1 } aggregate ? aggregate.InnerExceptions[0] : thrown;
            logger.LogError(ex, "{Function} failed for app {AppVersion}: {Method} {Path}",
                context.FunctionDefinition.Name, CallerOf(http), http.Request.Method, http.Request.Path);

            var badBody = ex is BadBodyException;
            http.Response.StatusCode = badBody ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
            await http.Response.WriteAsJsonAsync(new ApiError(
                badBody ? ApiErrors.BadBody : ApiErrors.Exception,
                ApiResults.ShowDetails ? ex.ToString() : null));
        }
    }

    /// <summary>The app build that called (its <see cref="ApiHeaders.AppVersion"/> header), cut short; "unknown" for an app from before versions.</summary>
    public static string CallerOf(HttpContext http) =>
        http.Request.Headers[ApiHeaders.AppVersion].ToString() is { Length: > 0 } version
            ? version.Length > MaxCaller ? version[..MaxCaller] : version
            : "unknown";

    /// <summary>The longest caller version kept in a log line.</summary>
    public const int MaxCaller = 80;

    /// <summary>
    /// True for an answer worth a log line: the function refused the request (400, 403, 409, 429…) or failed (5xx).
    /// 401 (signed out) and 404 (no profile yet, a listing that's gone) are everyday answers.
    /// </summary>
    public static bool IsRefusal(int status) => status >= 400 && status is not (StatusCodes.Status401Unauthorized or StatusCodes.Status404NotFound);

    // The host writes a function's result after the middleware returns, so the status is read from the result itself.
    private static int? RefusalOf(FunctionContext context) =>
        context.Features is not null && context.GetInvocationResult().Value is IStatusCodeActionResult { StatusCode: { } status } && IsRefusal(status)
            ? status
            : null;
}
