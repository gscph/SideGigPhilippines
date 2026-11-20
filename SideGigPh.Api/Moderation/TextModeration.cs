using Microsoft.Extensions.Logging;

namespace SideGigPh.Api.Moderation;

/// <summary>
/// Checks what people write before it is saved or sent: chat messages, posts, offers, reviews, "Others" text, names,
/// listing text and suggestions. <see cref="RudeWords"/> first (English, Tagalog, Taglish, jejemon; free and instant),
/// then the outside service (<see cref="ITextModerator"/>, Azure AI Content Safety) for sexual and hateful content
/// the list doesn't know. A refused text is never saved: the function answers <c>422 text-not-allowed</c>
/// (<see cref="ApiResults.TextNotAllowed"/>) and the app asks the person to rewrite it. When the service can't be
/// reached the word list's answer stands (fail open), so a write never waits on Azure being up. Texts are never
/// logged: only what was checked (<c>what</c>) and why it was refused.
/// </summary>
public sealed class TextModeration(ITextModerator moderator, ILogger<TextModeration> logger)
{
    /// <summary>True when every text may be saved. Empty and null texts pass; all texts go to the service in one call.</summary>
    public async Task<bool> AllowsAsync(string what, IEnumerable<string?> texts, CancellationToken cancellationToken)
    {
        var written = texts.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()).ToList();
        if (written.Count == 0)
        {
            return true;
        }

        if (RudeWords.AnyIn(written))
        {
            logger.LogInformation("Moderation: refused {What} (word list).", what);
            return false;
        }

        if (await moderator.CheckAsync(string.Join("\n", written), cancellationToken) == ModeratorVerdict.Flagged)
        {
            logger.LogInformation("Moderation: refused {What} (Content Safety).", what);
            return false;
        }

        return true;
    }

    /// <summary>One text; see <see cref="AllowsAsync(string, IEnumerable{string?}, CancellationToken)"/>.</summary>
    public Task<bool> AllowsAsync(string what, string? text, CancellationToken cancellationToken) =>
        AllowsAsync(what, [text], cancellationToken);
}
