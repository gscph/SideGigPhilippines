using SideGigPh.Api.Moderation;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// Azure AI Content Safety stand-in: flags any text containing <see cref="Marker"/> (something the word list doesn't
/// know), so tests can see the service's "no" refuse a write. Counts its calls; <see cref="Down"/> answers Unchecked.
/// </summary>
public sealed class FakeTextModerator : ITextModerator
{
    /// <summary>Put this in a text to have the stand-in flag it.</summary>
    public const string Marker = "flag-me-azure";

    private int _calls;

    public int Calls => _calls;

    /// <summary>When true the stand-in behaves like a service that can't be reached.</summary>
    public bool Down { get; set; }

    public Task<ModeratorVerdict> CheckAsync(string text, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Task.FromResult(Down ? ModeratorVerdict.Unchecked
            : text.Contains(Marker, StringComparison.OrdinalIgnoreCase) ? ModeratorVerdict.Flagged : ModeratorVerdict.Clean);
    }
}
