using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Maui.Services.Api;

namespace SideGigPh.Tests.TestSupport;

/// <summary>
/// A pretend API for the app's HTTP adapters: answers each "METHOD path" with what a test set, and remembers every
/// request (so a test can check that a cached read made no request). Unknown paths answer 404.
/// </summary>
public sealed class StubServer : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _answers = [];

    public System.Collections.Concurrent.ConcurrentQueue<(string Route, string? Body, string? Token)> Requests { get; } = new();

    /// <summary>When set, every request fails like a phone with no signal.</summary>
    public bool Offline { get; set; }

    public void Answer(string route, HttpStatusCode status, string? json = null) =>
        _answers[route] = () => new HttpResponseMessage(status)
        {
            Content = new StringContent(json ?? string.Empty, Encoding.UTF8, "application/json"),
        };

    public int CountOf(string route) => Requests.Count(r => r.Route == route);

    /// <summary>An <see cref="ApiClient"/> that talks to this stub as the signed-in <paramref name="user"/>.</summary>
    public ApiClient Client(AuthUser? user = null) =>
        new(Options, new FakeIdentity(user ?? TestData.Worker()), NullLogger<ApiClient>.Instance);

    public ApiOptions Options => new("https://api.test/api/", FakePhotoStore.BaseUrl, this);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Offline)
        {
            throw new HttpRequestException("No internet");
        }

        var route = $"{request.Method} {request.RequestUri!.PathAndQuery.Replace("/api/", string.Empty)}";
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Enqueue((route, body, request.Headers.Authorization?.Parameter));
        return _answers.TryGetValue(route, out var answer) ? answer() : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
