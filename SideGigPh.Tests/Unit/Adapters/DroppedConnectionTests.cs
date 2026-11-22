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

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task EveryWrite_WhenTheConnectionDrops_GoesOnceMore(string method)
    {
        // A chat send, a pick, Mark done after a minute in another app: the phone reuses a connection the network
        // closed, and the person saw "No internet" though the internet worked.
        var handler = new FailsFirst(new IOException("unexpected end of stream"));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        using var response = await api.SendAsync(new HttpMethod(method), "picked-jobs/1/done");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Calls.Should().Be(ApiClient.WriteTries);
    }

    [Fact]
    public async Task AWriteThatIsNotRepeatable_IsSentOnlyOnce()
    {
        var handler = new FailsFirst(new WebException("Socket closed"));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        var send = () => api.SendJsonAsync(HttpMethod.Post, "feedback", new SideGigPh.Contracts.PhotoDto("id", "url"), SideGigPh.Contracts.ApiJson.Default.PhotoDto, repeatable: false);

        await send.Should().ThrowAsync<HttpRequestException>();
        handler.Calls.Should().Be(1, "a repeat would post it twice");
    }

    [Fact]
    public async Task AWriteWithABody_WhenTheConnectionDrops_GoesOnceMoreWithTheSameBody()
    {
        // iOS sends a write on a kept-alive connection the server already closed (after the photo picker, a long form)
        // and doesn't try again itself.
        var handler = new FailsFirst(new HttpRequestException("The network connection was lost."));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        using var response = await api.SendAsync(HttpMethod.Post, "photos", [1, 2, 3], "image/jpeg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Calls.Should().Be(ApiClient.WriteTries);
        handler.Bodies.Should().HaveCount(2).And.AllSatisfy(body => body.Should().Equal(1, 2, 3));
        handler.ContentTypes.Should().AllBe("image/jpeg");
    }

    [Fact]
    public async Task AJsonWrite_WhenTheConnectionDrops_GoesOnceMore()
    {
        var handler = new FailsFirst(new WebException("Socket closed"));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        using var response = await api.SendJsonAsync(HttpMethod.Post, "job-posts", new SideGigPh.Contracts.PhotoDto("id", "url"), SideGigPh.Contracts.ApiJson.Default.PhotoDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Calls.Should().Be(2);
        handler.ContentTypes.Should().AllBe("application/json");
    }

    [Fact]
    public async Task AWrite_ThatDropsTwice_FailsAsNoAnswer()
    {
        var handler = new Counting(new HttpRequestException("The network connection was lost."));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        var send = () => api.SendAsync(HttpMethod.Post, "photos", [1], "image/jpeg");

        (await send.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().BeNull();
        handler.Calls.Should().Be(ApiClient.WriteTries);
    }

    [Fact]
    public async Task AWrite_ThatTimedOut_IsNotSentAgain()
    {
        var handler = new Counting(new TaskCanceledException("timed out", new TimeoutException()));
        var api = new ApiClient(new ApiOptions("https://api.test/api/", "https://photos.test/", handler), new FakeIdentity(null), NullLogger<ApiClient>.Instance);

        var send = () => api.SendAsync(HttpMethod.Post, "photos", [1], "image/jpeg");

        await send.Should().ThrowAsync<HttpRequestException>();
        handler.Calls.Should().Be(1, "a slow server may still be working on it");
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

        public List<byte[]> Bodies { get; } = [];

        public List<string?> ContentTypes { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                Bodies.Add(await request.Content.ReadAsByteArrayAsync(cancellationToken));
                ContentTypes.Add(request.Content.Headers.ContentType?.MediaType);
            }

            return ++Calls == 1 ? throw failure : new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class Counting(Exception failure) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            throw failure;
        }
    }
}
