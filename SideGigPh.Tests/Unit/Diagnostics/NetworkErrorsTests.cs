using System.Net;
using System.Net.Sockets;
using MobileAppTheme.Services;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// A lost connection (seen on a Samsung A15: "Unable to resolve host", "Socket closed", "Canceled") is told apart from
/// a bug, so the screen says "no internet, try again"; a server's error answer, a plain cancel and a file error (a full
/// phone) are not, so they are never hidden behind "check your Wi-Fi".
/// </summary>
public class NetworkErrorsTests
{
    public static TheoryData<Exception> Network => new()
    {
        new HttpRequestException("API: couldn't reach GET chats: Connection failure", new IOException("Unable to resolve host")),
        new WebException("Socket closed"),
        new SocketException((int)SocketError.HostNotFound),
        new TaskCanceledException("The request timed out", new TimeoutException()),
        new AggregateException(new WebException("Unable to resolve host")),
        new InvalidOperationException("wrapped", new WebException("Canceled")),
    };

    public static TheoryData<Exception> NotNetwork => new()
    {
        new HttpRequestException("500 Internal Server Error", null, HttpStatusCode.InternalServerError),
        new IOException("No space left on device"),
        new FileNotFoundException("diagnostics.json"),
        new TaskCanceledException("The person left the page"),
        new ObjectDisposedException("CancellationTokenSource"),
        new NullReferenceException(),
    };

    [Theory]
    [MemberData(nameof(Network))]
    public void ConnectionFailures_AreNetwork(Exception exception) => NetworkErrors.IsNetwork(exception).Should().BeTrue();

    [Theory]
    [MemberData(nameof(NotNetwork))]
    public void BugsServerErrorsAndFileErrors_AreNot(Exception exception) => NetworkErrors.IsNetwork(exception).Should().BeFalse();
}
