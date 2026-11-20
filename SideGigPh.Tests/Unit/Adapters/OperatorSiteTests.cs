using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SideGigPh.Api;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// The operator's web site is served by the API (<c>GET operator/…</c>, <see cref="OperatorSiteFunctions"/>): pages get
/// index.html pointed at the route, files come from the published folder only (never outside it), and the Brotli copy
/// goes to browsers that take it.
/// </summary>
[Collection(nameof(OperatorSiteTests))]
public sealed class OperatorSiteTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "operator-site-" + Guid.NewGuid().ToString("N"));
    private readonly string _before = OperatorSiteFunctions.Root;

    public OperatorSiteTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "_framework"));
        File.WriteAllText(Path.Combine(_root, OperatorSiteFunctions.IndexFile), "<html><head><base href=\"/\" /></head></html>");
        File.WriteAllText(Path.Combine(_root, "_framework", "dotnet.wasm"), "wasm");
        File.WriteAllText(Path.Combine(_root, "_framework", "dotnet.wasm.br"), "br");
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "operator-site-secret.txt"), "secret");
        OperatorSiteFunctions.Root = _root;
    }

    public void Dispose()
    {
        OperatorSiteFunctions.Root = _before;
        Directory.Delete(_root, recursive: true);
        File.Delete(Path.Combine(Path.GetTempPath(), "operator-site-secret.txt"));
    }

    private static async Task<(IActionResult Result, HttpResponse Response)> GetAsync(string? path, string acceptEncoding = "")
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/operator/" + path;
        if (acceptEncoding.Length > 0)
        {
            http.Request.Headers.AcceptEncoding = acceptEncoding;
        }

        return (await new OperatorSiteFunctions().Get(http.Request, path), http.Response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("badges")]
    [InlineData("some/page/")]
    public async Task Pages_GetIndex_WithTheRouteAsBase(string? path)
    {
        var (result, response) = await GetAsync(path);

        var page = result.Should().BeOfType<ContentResult>().Subject;
        page.Content.Should().Contain("<base href=\"/api/operator/\" />");
        response.Headers.XFrameOptions.ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task RouteWithoutSlash_RedirectsToTheSlash()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/operator";

        (await new OperatorSiteFunctions().Get(http.Request, null))
            .Should().BeOfType<RedirectResult>().Which.Url.Should().Be("/api/operator/");
    }

    [Fact]
    public async Task Files_ComeFromTheSite_WithTheirType()
    {
        var (result, response) = await GetAsync("_framework/dotnet.wasm");

        var file = result.Should().BeOfType<PhysicalFileResult>().Subject;
        file.FileName.Should().Be(Path.Combine(_root, "_framework", "dotnet.wasm"));
        file.ContentType.Should().Be("application/wasm");
        response.Headers.ContentEncoding.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task BrowsersThatTakeBrotli_GetTheBrotliCopy()
    {
        var (result, response) = await GetAsync("_framework/dotnet.wasm", "gzip, br");

        var file = result.Should().BeOfType<PhysicalFileResult>().Subject;
        file.FileName.Should().EndWith("dotnet.wasm.br");
        file.ContentType.Should().Be("application/wasm", "the type is the original file's");
        response.Headers.ContentEncoding.ToString().Should().Be("br");
    }

    [Theory]
    [InlineData("../operator-site-secret.txt")]
    [InlineData("..%2Foperator-site-secret.txt")]
    [InlineData("_framework/missing.js")]
    public async Task NothingOutsideTheSite_OrMissing(string path) =>
        (await GetAsync(path)).Result.Should().BeOfType<NotFoundResult>();

    [Fact]
    public async Task WithoutAPublishedSite_EverythingIs404()
    {
        OperatorSiteFunctions.Root = Path.Combine(_root, "nope");

        (await GetAsync(null)).Result.Should().BeOfType<NotFoundResult>();
    }
}
