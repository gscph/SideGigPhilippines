using System.Globalization;
using System.Text;

namespace SideGigPh.Maui.Services.Search;

/// <summary>
/// Turns what people type ("Pa-linis ng AIRCON!", "tubero po") into comparable words:
/// lower case, accents dropped, punctuation split, and filler words (English and Tagalog)
/// removed, so only the words that say what the job is are left.
/// </summary>
public static class SearchText
{
    private static readonly HashSet<string> FillerWords =
    [
        // English
        "a", "an", "the", "and", "or", "of", "for", "to", "in", "on", "at", "my", "me", "i", "is", "it",
        "need", "needs", "want", "someone", "somebody", "please", "with", "near", "help", "new",
        // Tagalog / Taglish
        "ng", "sa", "ang", "mga", "na", "at", "o", "ko", "po", "pa", "paki", "mag", "magpa", "kailangan",
        "gusto", "may", "yung", "ung", "nang", "si", "ni", "para", "iba", "pang", "lang", "naman", "ba",
        "ito", "dito", "namin", "kami", "akin", "ako", "tao", "taong", "hanap", "naghahanap",
    ];

    /// <summary>The meaningful words in <paramref name="text"/>, in order, without repeats.</summary>
    public static IReadOnlyList<string> Words(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var builder = new StringBuilder(text.Length);
        foreach (var c in text.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue; // é → e, ñ → n
            }

            builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }

        return builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 2 && !FillerWords.Contains(word))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// How well a typed word matches a known word: 1 for the same word, 0.8 when the user typed
    /// the start of it ("airc" → aircon, "clean" → cleaning), 0.7 when they typed a longer form
    /// ("leaking" → leak), else 0.
    /// </summary>
    public static double Similarity(string typed, string known)
    {
        if (typed == known)
        {
            return 1;
        }

        if (typed.Length >= 3 && known.StartsWith(typed, StringComparison.Ordinal))
        {
            return 0.8;
        }

        return known.Length >= 4 && typed.StartsWith(known, StringComparison.Ordinal) ? 0.7 : 0;
    }

    /// <summary>Sum over the typed words of each one's best match among <paramref name="known"/>.</summary>
    public static double Score(IReadOnlyList<string> typed, IReadOnlyCollection<string> known) =>
        typed.Sum(word => known.Count == 0 ? 0 : known.Max(k => Similarity(word, k)));
}
