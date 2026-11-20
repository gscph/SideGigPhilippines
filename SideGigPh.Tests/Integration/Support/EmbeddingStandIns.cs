using System.Collections.Concurrent;
using SideGigPh.Api.Insights;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// Azure OpenAI embeddings stand-in: a text in <see cref="Vectors"/> (by cleaned text) gets that vector, any other a
/// fixed one of its own made from its letters, so unrelated texts are far apart. Records every text it was asked for.
/// </summary>
public sealed class FakeTextEmbedder : ITextEmbedder
{
    private const int Size = 64;

    /// <summary>Vectors tests choose, by cleaned text (<c>OtherJobGrouping.Clean</c>).</summary>
    public ConcurrentDictionary<string, float[]> Vectors { get; } = new(StringComparer.Ordinal);

    /// <summary>Every text the API asked to embed, in order.</summary>
    public ConcurrentQueue<string> Asked { get; } = new();

    public string Model => $"fake/{Size}";

    public Task<IReadOnlyList<float[]>?> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        foreach (var text in texts)
        {
            Asked.Enqueue(text);
        }

        return Task.FromResult<IReadOnlyList<float[]>?>(texts.Select(t => Vectors.TryGetValue(t, out var chosen) ? chosen : Own(t)).ToList());
    }

    /// <summary>A vector that depends only on the text (string.GetHashCode changes per run, so a stable hash seeds it).</summary>
    private static float[] Own(string text)
    {
        var seed = text.Aggregate(17, (hash, c) => unchecked(hash * 31 + c));
        var random = new Random(seed);
        return Enumerable.Range(0, Size).Select(_ => (float)(random.NextDouble() * 2 - 1)).ToArray();
    }
}
