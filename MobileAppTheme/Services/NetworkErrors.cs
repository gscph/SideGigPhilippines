using System.Net;
using System.Net.Sockets;

namespace MobileAppTheme.Services;

/// <summary>
/// Tells a lost or failed connection (no signal, no DNS, dropped socket, timeout) apart from a bug, so the app can say
/// "no internet, try again" instead of "something broke", and never close over it.
/// </summary>
public static class NetworkErrors
{
    /// <summary>
    /// True only for a web request that got no answer: an <see cref="HttpRequestException"/> without a status code
    /// (the app's API client turns Android's socket errors into one), a <see cref="WebException"/> or
    /// <see cref="SocketException"/>, or a request timeout (<see cref="TaskCanceledException"/> caused by a
    /// <see cref="TimeoutException"/>). A server's error answer (it has a status code), a plain cancel, and file errors
    /// such as "No space left on device" are not: those are bugs or other problems the person can't fix with Wi-Fi.
    /// </summary>
    public static bool IsNetwork(Exception? exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            switch (e)
            {
                case HttpRequestException { StatusCode: not null }:
                    return false;
                case HttpRequestException or WebException or SocketException or TimeoutException:
                    return true;
                case TaskCanceledException { InnerException: TimeoutException }:
                    return true;
                case AggregateException aggregate when aggregate.InnerExceptions.Count == 1:
                    continue;
            }
        }

        return false;
    }
}
