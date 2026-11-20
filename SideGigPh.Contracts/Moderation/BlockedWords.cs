using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SideGigPh.Contracts;

/// <summary>
/// The blocked-words check both sides run on text other people will read (names, job posts, offer notes, reviews,
/// feedback): English, Tagalog/Taglish and common Bisaya vulgarity, sexual terms, slurs and insults, including the
/// spellings people use to get around a filter (<c>put@</c>, <c>g@g0</c>, <c>vovo</c>, <c>p u t a</c>, <c>8==D</c>,
/// <c>../..</c>). The app checks a box before sending, so the person can fix it; the server refuses the same text
/// (<c>400 invalid:{field}</c>), so an old app or a script can't get past it.
/// <para>
/// The lists are <c>Moderation/blocked-words.txt</c> (<c>[whole]</c> entries match whole words or phrases,
/// <c>[part]</c> entries match inside a word) and <c>Moderation/blocked-patterns.txt</c> (regexes on the raw text:
/// symbol art, emoji, and words whose blocked and allowed forms differ by context, like <c>leche</c> and
/// <c>leche flan</c>). Text is normalized first (look-alike characters, accents, separators, repeated letters), in
/// the order the samples in <c>SideGigPh.Tests/Unit/Moderation</c> check. Words that are also everyday words here
/// (<c>puto</c>, <c>tae</c>, <c>bakla</c>, <c>atay</c>, <c>supot</c>, <c>poso negro</c>) are allowed on their own.
/// </para>
/// </summary>
public static class BlockedWords
{
    private static readonly Lazy<Lists> Loaded = new(Load);

    private static readonly Dictionary<char, char> Homoglyphs = new()
    {
        ['а'] = 'a', ['е'] = 'e', ['о'] = 'o', ['р'] = 'p', ['с'] = 'c', ['у'] = 'y', ['х'] = 'x', ['к'] = 'k',
        ['і'] = 'i', ['ј'] = 'j', ['ѕ'] = 's', ['ԁ'] = 'd', ['ո'] = 'n',
        ['α'] = 'a', ['ο'] = 'o', ['ι'] = 'i', ['κ'] = 'k', ['ν'] = 'v', ['τ'] = 't', ['ρ'] = 'p', ['ı'] = 'i',
    };

    // Look-alikes mapped anywhere in a word ('1' and '|' are handled apart: i in one view, l in another).
    private static readonly Dictionary<char, char> LookAlikes = new()
    {
        ['4'] = 'a', ['@'] = 'a', ['0'] = 'o', ['3'] = 'e', ['5'] = 's', ['$'] = 's', ['7'] = 't', ['8'] = 'b', ['9'] = 'g',
    };

    // Look-alikes mapped only between two letters or digits (so "gago!" and "(puta)" keep their punctuation).
    private static readonly Dictionary<char, char> InnerLookAlikes = new() { ['!'] = 'i', ['+'] = 't', ['('] = 'c' };

    private const string Separators = ".-_*'’`~^";

    private static readonly char[] TokenBreaks = [' ', '\t', '\r', '\n', ' ', '　', ':', '/'];

    // A number with an optional unit is a quantity, never letters: 1500, 10am, 5pcs, 90sqm, 2nd.
    private static readonly Regex Quantity = new(
        "^[0-9][0-9.,]*(?:st|nd|rd|th|am|pm|x|k|m|cm|mm|ft|kg|g|l|hp|kw|w|v|s|pc|pcs|hr|hrs|min|mins|sqm|yr|yrs|php|day|days)?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex Repeats = new("(.)\\1+", RegexOptions.CultureInvariant);

    /// <summary>True when <paramref name="text"/> has a blocked word or symbol. Empty text has none.</summary>
    public static bool Contains(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lists = Loaded.Value;
        var raw = text.Normalize(NormalizationForm.FormKC);
        foreach (var pattern in lists.Patterns)
        {
            try
            {
                if (pattern.IsMatch(raw))
                {
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // A pathological input: the word lists below still run.
            }
        }

        var (wholeViews, partViews) = Views(raw);
        foreach (var tokens in wholeViews)
        {
            foreach (var entry in lists.Whole)
            {
                if (HasSequence(tokens, entry))
                {
                    return true;
                }
            }
        }

        foreach (var tokens in partViews)
        {
            foreach (var token in tokens)
            {
                foreach (var entry in lists.Part)
                {
                    if (token.Contains(entry, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>True when any of <paramref name="texts"/> has a blocked word.</summary>
    public static bool AnyIn(params string?[] texts) => texts.Any(Contains);

    private static bool HasSequence(List<string> tokens, string[] entry)
    {
        for (var i = 0; i + entry.Length <= tokens.Count; i++)
        {
            var all = true;
            for (var j = 0; j < entry.Length && all; j++)
            {
                all = tokens[i + j] == entry[j];
            }

            if (all)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The normalized views of a text: for whole entries, the i view (1 and | read as i), the l view (read as l, only
    /// when the text had a 1 or |) and both with v read as b (vovo = bobo); for part entries, the i and l views only.
    /// </summary>
    private static (List<List<string>> Whole, List<List<string>> Part) Views(string text)
    {
        var plain = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.Format))
            {
                plain.Append(c);
            }
        }

        var lowered = plain.ToString().ToLowerInvariant();
        var mapped = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            mapped.Append(Homoglyphs.TryGetValue(c, out var latin) ? latin : c);
        }

        // ':' and '/' split too, so a number after them stays a number ("localhost:8080", "2/3"), not look-alike letters.
        var raw = JoinSingles(mapped.ToString().Split(TokenBreaks, StringSplitOptions.RemoveEmptyEntries), _ => true);
        var part = new List<List<string>>();
        foreach (var one in new[] { 'i', 'l' })
        {
            var usedOne = false;
            var tokens = new List<string>(raw.Count);
            foreach (var token in raw)
            {
                var (swapped, used) = MapLookAlikes(token, one);
                usedOne |= used;
                tokens.Add(DropSeparators(swapped));
            }

            if (one == 'l' && !usedOne)
            {
                break;
            }

            var letters = new string([.. string.Join(' ', tokens).Select(c => IsLetter(c) ? c : ' ')]);
            part.Add([.. JoinSingles(letters.Split(' ', StringSplitOptions.RemoveEmptyEntries), c => IsLetter(c[0]))
                .Select(token => Repeats.Replace(token, "$1"))]);
        }

        var whole = new List<List<string>>(part);
        whole.AddRange(part.Select(view => view.Select(token => token.Replace('v', 'b')).ToList()));
        return (whole, part);
    }

    private static bool IsLetter(char c) => c is >= 'a' and <= 'z';

    // A run of 3 or more one-character tokens is one word spelled out: "p u t a" → "puta". Runs of 2 stay apart.
    private static List<string> JoinSingles(IEnumerable<string> tokens, Func<string, bool> counts)
    {
        var result = new List<string>();
        var run = new List<string>();
        void Flush()
        {
            if (run.Count >= 3)
            {
                result.Add(string.Concat(run));
            }
            else
            {
                result.AddRange(run);
            }

            run.Clear();
        }

        foreach (var token in tokens)
        {
            if (token.Length == 1 && counts(token))
            {
                run.Add(token);
                continue;
            }

            Flush();
            result.Add(token);
        }

        Flush();
        return result;
    }

    private static (string Token, bool UsedOne) MapLookAlikes(string token, char one)
    {
        if (!token.Any(IsLetter) || Quantity.IsMatch(token))
        {
            return (token, false);
        }

        var result = new StringBuilder(token.Length);
        var usedOne = false;
        for (var i = 0; i < token.Length; i++)
        {
            var c = token[i];
            if (c is '1' or '|')
            {
                usedOne = true;
                result.Append(one);
            }
            else if (LookAlikes.TryGetValue(c, out var letter))
            {
                result.Append(letter);
            }
            else if (InnerLookAlikes.TryGetValue(c, out var inner) && i > 0 && i < token.Length - 1
                     && char.IsLetterOrDigit(token[i - 1]) && char.IsLetterOrDigit(token[i + 1]))
            {
                result.Append(inner);
            }
            else
            {
                result.Append(c);
            }
        }

        return (result.ToString(), usedOne);
    }

    // Separators between letters go: "p.u.t.a" → "puta", "tang-ina" → "tangina", "f*ck" → "fck".
    private static string DropSeparators(string token)
    {
        var result = new StringBuilder(token.Length);
        var i = 0;
        while (i < token.Length)
        {
            if (Separators.Contains(token[i]))
            {
                var j = i;
                while (j < token.Length && Separators.Contains(token[j]))
                {
                    j++;
                }

                if (!(result.Length > 0 && IsLetter(result[^1]) && j < token.Length && IsLetter(token[j])))
                {
                    result.Append(token, i, j - i);
                }

                i = j;
                continue;
            }

            result.Append(token[i]);
            i++;
        }

        return result.ToString();
    }

    private sealed record Lists(Regex[] Patterns, string[][] Whole, string[] Part);

    private static Lists Load()
    {
        var whole = new List<string[]>();
        var part = new List<string>();
        string? section = null;
        foreach (var line in Lines("SideGigPh.Contracts.blocked-words.txt"))
        {
            if (line is "[whole]" or "[part]")
            {
                section = line;
            }
            else if (section == "[whole]")
            {
                whole.Add(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
            else if (section == "[part]")
            {
                part.Add(line);
            }
        }

        var patterns = Lines("SideGigPh.Contracts.blocked-patterns.txt")
            .Select(line => new Regex(line, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
            .ToArray();
        return new Lists(patterns, [.. whole], [.. part]);
    }

    // The lines of an embedded list: trimmed, without blank lines and # comments.
    private static IEnumerable<string> Lines(string resource)
    {
        using var stream = typeof(BlockedWords).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing embedded list {resource}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0 && !trimmed.StartsWith('#'))
            {
                lines.Add(trimmed);
            }
        }

        return lines;
    }
}
