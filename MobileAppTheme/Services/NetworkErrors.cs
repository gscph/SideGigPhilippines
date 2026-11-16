using System.Net;

namespace MobileAppTheme.Services;

/// <summary>
/// Tells a lost or failed connection (no signal, no DNS, dropped socket, timeout) apart from a bug, so the app can say
/// "no connection, try again" instead of "something broke", and never close over it.
/// </summary>
public static class NetworkErrors
{
    /// <summary>
    /// True when the exception, or one inside it, is a connection failure: an <see cref="HttpRequestException"/>
    /// without a status code (the server never answered), Android's <see cref="WebException"/> /
    /// <see cref="IOException"/> (incl. sockets, "Unable to resolve host"), or a timeout. A server's error answer
    /// (it has a status code) is not one.
    /// </summary>
    public static bool IsNetwork(Exception? exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            switch (e)
            {
                case HttpRequestException { StatusCode: not null }:
                    return false;
                case HttpRequestException or WebException or IOException or TimeoutException or TaskCanceledException:
                    return true;
                case AggregateException aggregate when aggregate.InnerExceptions.Count == 1:
                    continue;
            }
        }

        return false;
    }
}
