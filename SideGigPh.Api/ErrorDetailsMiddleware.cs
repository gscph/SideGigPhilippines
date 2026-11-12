using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Turns an exception from any function into a JSON <see cref="ApiError"/> instead of the host's empty 500:
/// <c>400 bad-body</c> for an unreadable body, <c>500 exception</c> for anything else, each with the full
/// exception and stack trace in <see cref="ApiError.Detail"/> while <see cref="ApiResults.ShowDetails"/> is on.
/// </summary>
internal sealed class ErrorDetailsMiddleware(ILogger<ErrorDetailsMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception thrown)
        {
            var http = context.GetHttpContext();
            if (http is null || http.Response.HasStarted)
            {
                throw;
            }

            var ex = thrown is AggregateException { InnerExceptions.Count: 1 } aggregate ? aggregate.InnerExceptions[0] : thrown;
            logger.LogError(ex, "{Function} failed: {Method} {Path}", context.FunctionDefinition.Name, http.Request.Method, http.Request.Path);

            var badBody = ex is BadBodyException;
            http.Response.StatusCode = badBody ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
            await http.Response.WriteAsJsonAsync(new ApiError(
                badBody ? ApiErrors.BadBody : ApiErrors.Exception,
                ApiResults.ShowDetails ? ex.ToString() : null));
        }
    }
}
