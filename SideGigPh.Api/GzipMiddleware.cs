using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Net.Http.Headers;

namespace SideGigPh.Api;

/// <summary>
/// Gzips JSON answers for callers that say they accept it (<c>Accept-Encoding: gzip</c>): search pages, Inboxes and
/// the config shrink to a fraction on prepaid mobile data, and the Function App doesn't compress on its own. The host
/// writes a function's <see cref="IActionResult"/> after the middleware returns, so the result itself is wrapped
/// (<see cref="Wrap"/>). Answers without a JSON body (204, 304, 401) and callers that don't ask (old apps) are untouched.
/// </summary>
internal sealed class GzipMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);
        if (context.GetHttpContext() is { } http && context.GetInvocationResult() is { } invocation)
        {
            invocation.Value = Wrap(http.Request, invocation.Value);
        }
    }

    /// <summary>The function's result, gzipped when it has a JSON body and the caller accepts gzip.</summary>
    public static object? Wrap(HttpRequest request, object? result) =>
        result is ObjectResult json && AcceptsGzip(request) ? new GzipResult(json) : result;

    public static bool AcceptsGzip(HttpRequest request) =>
        request.GetTypedHeaders().AcceptEncoding.Any(encoding =>
            encoding.Value.Equals("gzip", StringComparison.OrdinalIgnoreCase) && (encoding.Quality ?? 1) > 0);

    /// <summary>Below this many bytes gzip saves too little to be worth it (the usual threshold of web servers).</summary>
    public const int MinBytes = 860;

    /// <summary>
    /// Writes <paramref name="inner"/>'s JSON into memory (answers are small), then sends it gzipped with
    /// <c>Content-Encoding: gzip</c> and its real length; a body under <see cref="MinBytes"/> goes out as it is.
    /// </summary>
    private sealed class GzipResult(ObjectResult inner) : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            var original = response.Body;
            using var json = new MemoryStream();
            response.Body = json;
            try
            {
                await inner.ExecuteResultAsync(context);
            }
            finally
            {
                response.Body = original;
            }

            response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);
            if (json.Length < MinBytes)
            {
                response.ContentLength = json.Length;
                await original.WriteAsync(json.GetBuffer().AsMemory(0, (int)json.Length), context.HttpContext.RequestAborted);
                return;
            }

            using var zipped = new MemoryStream();
            await using (var gzip = new GZipStream(zipped, CompressionLevel.Fastest, leaveOpen: true))
            {
                await gzip.WriteAsync(json.GetBuffer().AsMemory(0, (int)json.Length), context.HttpContext.RequestAborted);
            }

            response.Headers.ContentEncoding = "gzip";
            response.ContentLength = zipped.Length;
            await original.WriteAsync(zipped.GetBuffer().AsMemory(0, (int)zipped.Length), context.HttpContext.RequestAborted);
        }
    }
}
