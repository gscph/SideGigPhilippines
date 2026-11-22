namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// The phone's connection to the API, which can drop the next writes before any answer, like a kept-alive connection
/// the server closed while the person was in the camera or the photo picker: Android's handler then throws an
/// <see cref="IOException"/> ("unexpected end of stream"), iOS "The network connection was lost".
/// </summary>
public sealed class DroppingConnection(HttpMessageHandler server) : DelegatingHandler(server)
{
    private int _dropWrites;

    /// <summary>How many of the next writes (POST / PUT / DELETE) drop before reaching the server.</summary>
    public int DropWrites
    {
        get => Volatile.Read(ref _dropWrites);
        set => Volatile.Write(ref _dropWrites, value);
    }

    /// <summary>Writes that reached the server.</summary>
    public int WritesSent { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get)
        {
            if (Interlocked.Decrement(ref _dropWrites) >= 0)
            {
                throw new IOException("unexpected end of stream on https://api.tarbaho.com/...");
            }

            Interlocked.Exchange(ref _dropWrites, 0);
            WritesSent++;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
