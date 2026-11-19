using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MobileAppTheme.Services.Local;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// "Offline" is not only "no network": with broken DNS or a captive Wi-Fi the phone says connected while nothing gets
/// through. A request that got no answer counts the phone as offline (the gray strip, "No internet + Try again" on empty
/// lists) until a request gets an answer again; a server's error answer still proves the connection works.
/// </summary>
public class ConnectionStatusTests
{
    [Fact]
    public void ARequestWithNoAnswer_CountsAsOffline_UntilOneIsAnswered()
    {
        var status = new DeviceNetworkStatus();
        var changes = 0;
        status.Changed += () => changes++;

        status.IsOnline.Should().BeTrue("no platform here: the phone counts as connected");

        status.NoAnswer();
        status.IsOnline.Should().BeFalse();
        status.NoAnswer();
        changes.Should().Be(1, "only a change is announced");

        status.Answered();
        status.IsOnline.Should().BeTrue();
        changes.Should().Be(2);
    }

    [Fact]
    public async Task ApiClient_ARequestWithNoAnswer_MarksOffline()
    {
        var status = new DeviceNetworkStatus();
        var server = new StubServer { Offline = true };
        var api = new ApiClient(server.Options, new FakeIdentity(null), NullLogger<ApiClient>.Instance, status);

        var read = () => api.SendAsync(HttpMethod.Get, "config");

        await read.Should().ThrowAsync<HttpRequestException>();
        status.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task ApiClient_ARequestThatOnlyRanOutOfTime_IsNotCountedAsOffline()
    {
        var status = new DeviceNetworkStatus();
        var options = new ApiOptions("https://api.test/api/", "https://photos.test/", new TimesOut());
        var api = new ApiClient(options, new FakeIdentity(null), NullLogger<ApiClient>.Instance, status);

        var read = () => api.SendAsync(HttpMethod.Get, "config");

        await read.Should().ThrowAsync<HttpRequestException>();
        status.IsOnline.Should().BeTrue("a cold server or paused database is slow, not a lost connection");
    }

    private sealed class TimesOut : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException("timed out", new TimeoutException());
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ApiClient_AnyAnswer_MarksOnlineAgain(HttpStatusCode answer)
    {
        var status = new DeviceNetworkStatus();
        status.NoAnswer();
        var server = new StubServer();
        server.Answer("GET config", answer, "{}");
        var api = new ApiClient(server.Options, new FakeIdentity(null), NullLogger<ApiClient>.Instance, status);

        using var response = await api.SendAsync(HttpMethod.Get, "config");

        status.IsOnline.Should().BeTrue("the server answered, so the internet works");
    }

    [Fact]
    public async Task ApiClient_ACancelledRequest_SaysNothingAboutTheConnection()
    {
        var status = new DeviceNetworkStatus();
        var api = new ApiClient(new StubServer().Options, new FakeIdentity(null), NullLogger<ApiClient>.Instance, status);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var read = () => api.SendAsync(HttpMethod.Get, "config", cancellationToken: cancelled.Token);

        await read.Should().ThrowAsync<OperationCanceledException>();
        status.IsOnline.Should().BeTrue("the person left the page; the connection may be fine");
    }
}
