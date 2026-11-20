using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Net.Http.Headers;

namespace SideGigPh.Api;

/// <summary>
/// The operator's web site (<c>SideGigPh.Admin</c>, Blazor WebAssembly) at <c>GET operator/…</c>: static files that
/// <c>deploy-api.yml</c> publishes into the package's <c>operator-site/</c> folder. Served by the API itself so the page
/// and the API share one origin (no CORS) and nothing else needs hosting. The files are public and hold no data: every
/// call the page makes still needs a Firebase ID token, and <c>admin/…</c> only answers the uids in <c>AdminUids</c>.
/// A path without a file extension is a page of the app, so it gets <c>index.html</c>, whose <c>&lt;base href="/"&gt;</c>
/// becomes this route's address. Without the folder (a local run, the tests) every address is 404.
/// </summary>
public sealed class OperatorSiteFunctions
{
    /// <summary>The folder in the deployed package that holds the published site (its <c>wwwroot</c>).</summary>
    public const string Folder = "operator-site";

    internal const string IndexFile = "index.html";

    private static readonly FileExtensionContentTypeProvider ContentTypes = CreateContentTypes();

    internal static string Root { get; set; } = Path.Combine(AppContext.BaseDirectory, Folder);

    [Function("OperatorSite")]
    public Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "operator/{*path}")] HttpRequest request,
        string? path) =>
        Task.FromResult(Serve(request, path));

    private static IActionResult Serve(HttpRequest request, string? path)
    {
        if (!Directory.Exists(Root))
        {
            return new NotFoundResult();
        }

        // "…/operator" → "…/operator/", so the page's relative addresses resolve under it.
        if (string.IsNullOrEmpty(path) && !request.Path.Value!.EndsWith('/'))
        {
            return new RedirectResult(request.Path.Value + "/" + request.QueryString, permanent: false);
        }

        AddSafetyHeaders(request.HttpContext.Response);
        if (IsPage(path))
        {
            return Index(request);
        }

        if (Resolve(Root, path!) is not { } file)
        {
            return new NotFoundResult();
        }

        // Blazor's publish writes a Brotli copy of every file beside it: send that when the browser takes it.
        var accepts = request.Headers.AcceptEncoding.ToString();
        var sent = file;
        if (accepts.Contains("br", StringComparison.OrdinalIgnoreCase) && File.Exists(file + ".br"))
        {
            sent = file + ".br";
            request.HttpContext.Response.Headers.ContentEncoding = "br";
        }

        request.HttpContext.Response.Headers.Vary = HeaderNames.AcceptEncoding;
        request.HttpContext.Response.Headers.CacheControl = "no-cache";
        var info = new FileInfo(sent);
        return new PhysicalFileResult(sent, ContentTypeOf(file))
        {
            LastModified = info.LastWriteTimeUtc,
            EntityTag = new EntityTagHeaderValue($"\"{info.Length:x}-{info.LastWriteTimeUtc.Ticks:x}\""),
        };
    }

    private static IActionResult Index(HttpRequest request)
    {
        var index = Path.Combine(Root, IndexFile);
        if (!File.Exists(index))
        {
            return new NotFoundResult();
        }

        request.HttpContext.Response.Headers.CacheControl = "no-cache";
        return new ContentResult
        {
            Content = WithBase(File.ReadAllText(index), BasePath(request.Path.Value!)),
            ContentType = "text/html; charset=utf-8",
            StatusCode = StatusCodes.Status200OK,
        };
    }

    /// <summary>A page of the app (no file extension, or nothing at all) rather than a file.</summary>
    internal static bool IsPage(string? path) =>
        string.IsNullOrEmpty(path) || !Path.HasExtension(path.TrimEnd('/'));

    /// <summary>The site's address on this host: everything up to and including <c>operator/</c>.</summary>
    internal static string BasePath(string requestPath)
    {
        const string marker = "/operator/";
        var at = requestPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return at < 0 ? "/" : requestPath[..(at + marker.Length)];
    }

    /// <summary>index.html with its <c>&lt;base href="/"&gt;</c> pointing at <paramref name="basePath"/>.</summary>
    internal static string WithBase(string html, string basePath) =>
        html.Replace("<base href=\"/\" />", $"<base href=\"{basePath}\" />", StringComparison.Ordinal);

    /// <summary>The file under <paramref name="root"/> that <paramref name="path"/> names, or null (missing, or outside it).</summary>
    internal static string? Resolve(string root, string path)
    {
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string full;
        try
        {
            full = Path.GetFullPath(Path.Combine(rootFull, path.Replace('\\', '/').TrimStart('/')));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        return full.StartsWith(rootFull, StringComparison.Ordinal) && File.Exists(full) ? full : null;
    }

    internal static string ContentTypeOf(string file) =>
        ContentTypes.TryGetContentType(file, out var type) ? type : "application/octet-stream";

    private static FileExtensionContentTypeProvider CreateContentTypes()
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".wasm"] = "application/wasm";
        provider.Mappings[".dat"] = "application/octet-stream";
        provider.Mappings[".blat"] = "application/octet-stream";
        provider.Mappings[".webcil"] = "application/octet-stream";
        provider.Mappings[".pdb"] = "application/octet-stream";
        return provider;
    }

    private static void AddSafetyHeaders(HttpResponse response)
    {
        response.Headers.XFrameOptions = "DENY";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }
}
