namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// The phone's connection to the API, as bad as a phone's gets after a pause (the camera, the photo picker, a minute
/// in another app): a kept-alive connection the server or the mobile network closed. Android's handler then throws an
/// <see cref="IOException"/> ("unexpected end of stream"), iOS "The network connection was lost".
/// <para>
/// <b>By default each write loses the answer to its first try</b> (<see cref="LoseFirstAnswers"/>), the first time the
/// phone makes it (the same write again, a second chat message say, goes through, so a test's own limits aren't hit
/// twice as fast): the server does the work, the phone hears nothing, and the app's one more try
/// (<c>ApiClient.WriteTries</c>) must get the right answer without doing it twice. So every integration test also checks that each write it makes survives a dropped
/// connection and is safe to repeat; a new write that isn't fails here, not on a phone. Only the writes a repeat
/// would double (<see cref="NotRepeatable"/>) are left alone; the app sends those once.
/// </para>
/// </summary>
public sealed class DroppingConnection(HttpMessageHandler server) : DelegatingHandler(server)
{
    /// <summary>
    /// The writes the app sends only once (<c>repeatable: false</c>), as "METHOD path": a second feedback post or error
    /// report would be a duplicate. Keep it in step with the app's <c>repeatable: false</c> calls.
    /// </summary>
    public static readonly IReadOnlySet<string> NotRepeatable = new HashSet<string> { "POST feedback", "POST error-reports" };

    private readonly HashSet<string> _seen = [];
    private int _dropWrites;

    /// <summary>Each write loses the answer to its first try (see the class). On unless a test turns it off.</summary>
    public bool LoseFirstAnswers { get; set; } = true;

    /// <summary>How many of the next writes (POST / PUT / DELETE) drop before reaching the server.</summary>
    public int DropWrites
    {
        get => Volatile.Read(ref _dropWrites);
        set => Volatile.Write(ref _dropWrites, value);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        if (Interlocked.Decrement(ref _dropWrites) >= 0)
        {
            throw new IOException("unexpected end of stream on https://api.tarbaho.com/...");
        }

        Interlocked.Exchange(ref _dropWrites, 0);
        var response = await base.SendAsync(request, cancellationToken);
        if (LoseFirstAnswers && FirstTry(request))
        {
            response.Dispose();
            throw new IOException("unexpected end of stream on https://api.tarbaho.com/... (the answer was lost)");
        }

        return response;
    }

    // The first try of a write the phone hasn't made before; the app's retry is a new request, the same "METHOD path".
    private bool FirstTry(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;
        var key = $"{request.Method} {path[(path.IndexOf("/api/", StringComparison.Ordinal) + 5)..]}";
        if (NotRepeatable.Contains(key))
        {
            return false;
        }

        lock (_seen)
        {
            return _seen.Add(key);
        }
    }
}
