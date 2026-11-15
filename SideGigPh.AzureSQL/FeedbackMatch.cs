namespace SideGigPh.AzureSQL;

/// <summary>
/// Finds suggestions that probably ask for the same thing, so a person can add their vote instead of posting it
/// again. Two titles are compared by the words that carry meaning: small words (English and Tagalog: "the", "ng",
/// "po", "sana"…) and words everyone uses on a wish list ("add", "feature", "app") are dropped, a plural "s" is
/// ignored, and the score is the share of the shorter title's words found in the other. No AI and no index: the
/// board is small, and the person confirms the match themselves.
/// </summary>
public static class FeedbackMatch
{
    /// <summary>At least this share of the shorter title's words must be in the other one.</summary>
    public const double MinScore = 0.5;

    private static readonly HashSet<string> SmallWords = new(StringComparer.Ordinal)
    {
        // English
        "the", "and", "for", "you", "can", "could", "would", "should", "please", "want", "need", "have", "has", "with", "from",
        "that", "this", "there", "when", "what", "more", "able", "let", "also", "into", "our", "your", "are", "not", "but",
        // What every wish says
        "add", "feature", "option", "app", "make", "allow", "show", "put", "give",
        // Tagalog / Taglish
        "ang", "mga", "ng", "sa", "na", "po", "ko", "mo", "ako", "kami", "tayo", "sana", "may", "meron", "para", "yung", "lang",
        "din", "rin", "naman", "pwede", "puwede", "paki", "pa", "ba", "at", "ay", "kung", "nang", "dapat", "gusto",
    };

    /// <summary>The words of <paramref name="text"/> that carry meaning: lower case, no small words, no plural "s".</summary>
    public static IReadOnlySet<string> Words(string? text)
    {
        var words = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in (text ?? string.Empty).ToLowerInvariant().Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var word = new string([.. raw.Where(char.IsLetterOrDigit)]);
            if (SmallWords.Contains(word))
            {
                continue;
            }

            if (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal))
            {
                word = word[..^1];
            }

            if (word.Length >= 2 && !SmallWords.Contains(word))
            {
                words.Add(word);
            }
        }

        return words;
    }

    /// <summary>0 to 1: the share of the shorter set's words that are in the other. 0 when either has none.</summary>
    public static double Score(IReadOnlySet<string> a, IReadOnlySet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        return small.Count(large.Contains) / (double)small.Count;
    }

    /// <summary>True when the two texts probably ask for the same thing.</summary>
    public static bool IsSimilar(string? a, string? b) => Score(Words(a), Words(b)) >= MinScore;

    private static readonly char[] Separators = [' ', '\t', '\r', '\n', '/', '-', '_', ',', '.', ';', ':', '!', '?', '(', ')', '"', '\'', '+', '&'];
}
