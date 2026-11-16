using System.Net;
using MobileAppTheme.Services;

namespace SideGigPh.Tests.Unit.Diagnostics;

/// <summary>
/// A lost connection (seen on a Samsung A15: "Unable to resolve host", "Socket closed", "Canceled") is told apart from
/// a bug, so the screen says "no internet, try again"; a server's error answer is still a bug.
/// </summary>
public class NetworkErrorsTests
{
    public static TheoryData<Exception> Network => new()
    {
        new HttpRequestException("Connection failure", new IOException("Unable to resolve host")),
        new WebException("Socket closed"),
        new IOException("Canceled"),
        new TaskCanceledException("timed out"),
        new AggregateException(new WebException("Unable to resolve host")),
        new InvalidOperationException("wrapped", new WebException("Canceled")),
    };

    public static TheoryData<Exception> NotNetwork => new()
    {
        new HttpRequestException("500 Internal Server Error", null, HttpStatusCode.InternalServerError),
        new ObjectDisposedException("CancellationTokenSource"),
        new NullReferenceException(),
    };

    [Theory]
    [MemberData(nameof(Network))]
    public void ConnectionFailures_AreNetwork(Exception exception) => NetworkErrors.IsNetwork(exception).Should().BeTrue();

    [Theory]
    [MemberData(nameof(NotNetwork))]
    public void BugsAndServerErrors_AreNot(Exception exception) => NetworkErrors.IsNetwork(exception).Should().BeFalse();
}
