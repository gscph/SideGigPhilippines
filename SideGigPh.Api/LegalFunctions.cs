using System.Collections.Frozen;
using System.Net;
using Markdig;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace SideGigPh.Api;

/// <summary>
/// The public legal pages: <c>GET legal/privacy</c>, <c>legal/terms</c>, <c>legal/data</c>. Web pages anyone can open
/// without signing in, as Google Play and the App Store require (the store listings, the sign-in screen and the Me page
/// link here). The text is <c>docs/legal/*.md</c>, embedded at build and turned into HTML once per process.
/// </summary>
public sealed class LegalFunctions
{
    /// <summary>Each page: its address under <c>legal/</c>, its Markdown file and its title.</summary>
    internal static readonly IReadOnlyList<(string Page, string File, string Title)> Pages =
    [
        ("privacy", "privacy-policy.md", "Privacy Policy"),
        ("terms", "terms-of-service.md", "Terms of Service"),
        ("data", "data-handling.md", "Your Data"),
    ];

    private static readonly Lazy<FrozenDictionary<string, string>> Html = new(() =>
        Pages.ToFrozenDictionary(p => p.Page, p => Render(p.File, p.Title)));

    [Function("GetLegalPage")]
    public Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "legal/{page}")] HttpRequest request,
        string page) =>
        Task.FromResult<IActionResult>(Html.Value.TryGetValue(page, out var html)
            ? new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = StatusCodes.Status200OK }
            : new NotFoundResult());

    private static string Render(string file, string title)
    {
        using var stream = typeof(LegalFunctions).Assembly.GetManifestResourceStream($"legal/{file}")
            ?? throw new InvalidOperationException($"The legal page legal/{file} isn't embedded (SideGigPh.Api.csproj).");
        using var reader = new StreamReader(stream);
        var markdown = reader.ReadToEnd();

        // Links between the pages point at the Markdown files in docs/legal/; on the web they are the sibling addresses.
        foreach (var other in Pages)
        {
            markdown = markdown.Replace($"]({other.File}", $"]({other.Page}", StringComparison.Ordinal);
        }

        var body = Markdown.ToHtml(markdown, new MarkdownPipelineBuilder().UsePipeTables().UseAutoLinks().Build());
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Tarbaho · {{WebUtility.HtmlEncode(title)}}</title>
            <style>
              body { margin: 0; background: #fff; color: #222; font: 16px/1.6 system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; }
              main { max-width: 760px; margin: 0 auto; padding: 24px 16px 64px; }
              h1 { font-size: 1.8rem; line-height: 1.25; } h2 { margin-top: 2rem; font-size: 1.3rem; } h3 { font-size: 1.1rem; }
              a { color: #5B2A86; }
              table { border-collapse: collapse; width: 100%; display: block; overflow-x: auto; font-size: 0.95rem; }
              th, td { border: 1px solid #ebebeb; padding: 8px; text-align: left; vertical-align: top; }
              th { background: #f7f7f7; }
              blockquote { margin: 1rem 0; padding: 8px 16px; border-left: 4px solid #5B2A86; background: #f7f7f7; }
              hr { border: 0; border-top: 1px solid #ebebeb; margin: 2rem 0; }
            </style>
            </head>
            <body><main>
            {{body}}
            </main></body>
            </html>
            """;
    }
}
