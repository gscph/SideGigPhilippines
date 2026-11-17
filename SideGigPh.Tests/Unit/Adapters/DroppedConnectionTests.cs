using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// On Android a dropped or timed-out connection throws <see cref="WebException"/> ("Socket closed", "Canceled"), not
/// <see cref="HttpRequestException"/>. Reads must still fall back to empty and writes must fail the usual way, or a
/// weak 4G signal breaks the whole screen (seen on the Search page and on Publish).
/// </summary>
public class DroppedConnectionTests
{
    [Theory]
    [InlineData("socket")]
    [InlineData("io")]
    public async Task Read_WhenTheConnectionDrops_ComesBackEmpty(string kind)
    {
        var api = Client(kind == "socket" ? new WebException("Socket closed") : new IOException("Canceled"));

        var result = await api.ReadAsync(async () => await api.GetAsync("search/professionals", SideGigPh.Contracts.ApiJson.Default.SearchPageDto), null, "pros");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Write_WhenTheConnectionDrops_IsAnHttpRequestExceptionWithNoStatus()
    {
        var api = Client(new WebException("Socket closed"));

        var send = () => api.SendAsync(HttpMethod.Put, "professionals/me?x=1");

        var failure = (await send.Should().ThrowAsync<HttpRequestException>()).Which;
        failure.StatusCode.Should().BeNull("nobody answered, so the listing may have gone live");
        failure.Message.Should().NotContain("x=1", "query strings stay out of logs");
    }

    [Fact]
    public async Task Read_WhenTheFirstTryDrops_GoesOnceMoreAndGetsTheAnswer()
    {
        var handler = new FailsFirst(new WebException("Socket closed"));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        using var response = await api.SendAsync(HttpMethod.Get, "config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Calls.Should().Be(ApiClient.ReadTries);
    }

    [Fact]
    public async Task Write_WhenTheConnectionDrops_IsSentOnlyOnce()
    {
        var handler = new FailsFirst(new WebException("Socket closed"));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        var send = () => api.SendAsync(HttpMethod.Post, "job-posts");

        await send.Should().ThrowAsync<HttpRequestException>();
        handler.Calls.Should().Be(1, "a write that may have reached the server is never sent twice");
    }

    [Fact]
    public async Task Read_ThatNeverAnswers_FailsAsNoAnswerInsteadOfACancel()
    {
        var api = Client(new TaskCanceledException("timed out"));

        var send = () => api.SendAsync(HttpMethod.Get, "search/professionals");

        (await send.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().BeNull();
    }

    private static ApiClient Client(Exception failure) =>
        new(new ApiOptions("https://api.test/api/", "https://photos.test/", new Throwing(failure)), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

    private sealed class Throwing(Exception failure) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw failure;
    }

    private sealed class FailsFirst(Exception failure) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            ++Calls == 1 ? throw failure : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
