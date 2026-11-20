using System.Globalization;
using System.Text;

namespace SideGigPh.Api.Moderation;

/// <summary>
/// Tarbaho's own list of vulgar, sexual and swear words in English, Tagalog, Taglish and jejemon spellings, checked
/// first, on the server, for free and in no time. Azure AI Content Safety (<see cref="ITextModerator"/>) checks the
/// rest; it is weak on Tagalog slang, which is why this list exists. Text is normalized before matching: lower case,
/// accents off, jejemon/leet letters back (<c>g@g0</c> → <c>gago</c>, <c>p0ta</c> → <c>pota</c>), masks dropped
/// (<c>f*ck</c> → <c>fck</c>), spaced-out letters joined (<c>p u t a</c> → <c>puta</c>) and long repeats cut
/// (<c>gagooo</c> → <c>gago</c>). Words match whole, so <c>putahe</c>, <c>potato</c> and <c>cockroach</c> pass;
/// only <see cref="Stems"/> match inside a longer word (<c>putanginamo</c>, <c>kantutan</c>).
/// Add a word here when people find a way around it; every word is checked by <c>RudeWordsTests</c>.
/// </summary>
public static class RudeWords
{
    /// <summary>Whole words (after normalizing). Kept short and unambiguous in a home-services app.</summary>
    public static readonly IReadOnlySet<string> Words = new HashSet<string>(StringComparer.Ordinal)
    {
        // English swear words and insults
        "fuck", "fucks", "fucked", "fucker", "fucking", "fuckin", "fck", "fcking", "fuk", "fvck", "phuck", "wtf", "stfu",
        "shit", "shits", "shitty", "sht", "bullshit", "bitch", "bitches", "btch", "biatch", "bastard", "asshole", "ass",
        "dick", "dickhead", "cock", "cunt", "motherfucker", "slut", "whore", "retard", "retarded",
        "nigger", "nigga", "faggot", "fag",
        // English sexual words
        "sex", "sexy", "sexual", "nude", "nudes", "porn", "porno", "horny", "boobs", "boob", "tits", "titties",
        "pussy", "pusy", "penis", "vagina", "dildo", "blowjob", "handjob", "milf", "orgasm", "jerkoff", "masturbate",
        // Tagalog / Taglish swear words and insults
        "puta", "pota", "potaena", "putragis", "puchang", "punyeta", "tarantado", "tarantada", "gago", "gaga", "gagu",
        "ulol", "ulul", "olol", "bobo", "boba", "tanga", "inutil", "pakyu", "pakyo", "pkyu", "pakshet", "pakingshet",
        "tangina", "tanginamo", "tngina", "taena", "tangena", "kupal", "yawa", "pisti", "leche",
        // Tagalog / Taglish sexual words
        "puki", "puke", "pekpek", "kepyas", "bilat", "burat", "titi", "tite", "etits", "otin", "bayag", "iyot",
        "kantot", "jakol", "salsal", "chupa", "tsupa", "libog", "malibog", "kalibugan", "hindot", "kiffy", "jabol",
    };

    /// <summary>Strings that are rude inside any word too (<c>putanginamo</c>, <c>kinantot</c>, <c>motherfucking</c>).</summary>
    public static readonly IReadOnlyList<string> Stems =
    [
        "fuck", "putangina", "tangina", "kantot", "kantut", "jakol", "hindot", "pakyu", "punyeta", "tarantad", "malibog",
        "nigger", "blowjob", "pussy",
    ];

    /// <summary>
    /// Phrases (after normalizing): rude only together. <c>hayop</c> alone is an animal (<c>may hayop sa kisame</c>),
    /// <c>hayop ka</c> is an insult.
    /// </summary>
    public static readonly IReadOnlyList<string[]> Phrases =
    [
        ["hayop", "ka"],
        ["animal", "ka"],
        ["anak", "ng", "puta"],
        ["putang", "ina"],
        ["tang", "ina"],
        ["puta", "ng", "ina"],
        ["son", "of", "a", "bitch"],
    ];

    /// <summary>Exceptions that look rude but aren't: food and everyday words.</summary>
    private static readonly IReadOnlyList<string[]> Innocent =
    [
        ["leche", "flan"],
    ];

    /// <summary>True when the text has a word from the list (any of <paramref name="texts"/>).</summary>
    public static bool AnyIn(IEnumerable<string?> texts) => texts.Any(Contains);

    /// <summary>True when <paramref name="text"/> has a word from the list.</summary>
    public static bool Contains(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Two readings: with jejemon letters (g@g0 → gago) and with digits and marks as spaces (gago!! → gago).
        return HasRudeWord(Tokens(text, leet: true)) || HasRudeWord(Tokens(text, leet: false));
    }

    private static bool HasRudeWord(List<string> tokens)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (IsInnocentAt(tokens, i))
            {
                i++; // skip the pair
                continue;
            }

            var token = tokens[i];
            var squeezed = Squeeze(token);
            if (Words.Contains(token) || Words.Contains(squeezed)
                || Stems.Any(stem => token.Contains(stem, StringComparison.Ordinal) || squeezed.Contains(stem, StringComparison.Ordinal))
                || Phrases.Any(phrase => StartsAt(tokens, i, phrase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The text as normalized words (see the class summary), in order. With <paramref name="leet"/>, digits and marks
    /// inside a word are read as letters (<c>p0ta</c>); without, they split words.
    /// </summary>
    public static List<string> Tokens(string text, bool leet = true)
    {
        var plain = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue; // accents off
            }

            plain.Append(char.ToLowerInvariant(c));
        }

        var raw = new List<string>();
        var word = new StringBuilder();
        var chars = plain.ToString();
        for (var at = 0; at < chars.Length; at++)
        {
            var c = chars[at];
            if (leet && !char.IsLetter(c) && Leet(c) is var letter && letter != c && InsideWord(chars, at))
            {
                c = letter;
            }

            if (char.IsLetter(c))
            {
                word.Append(c);
            }
            else if (c == '*' && word.Length > 0)
            {
                // a mask inside a word: f*ck → fck
            }
            else if (word.Length > 0)
            {
                raw.Add(word.ToString());
                word.Clear();
            }
        }

        if (word.Length > 0)
        {
            raw.Add(word.ToString());
        }

        // Join runs of single letters: "p u t a" → "puta".
        var tokens = new List<string>(raw.Count);
        var letters = new StringBuilder();
        foreach (var token in raw)
        {
            if (token.Length == 1)
            {
                letters.Append(token);
                continue;
            }

            if (letters.Length > 0)
            {
                tokens.Add(CutRepeats(letters.ToString()));
                letters.Clear();
            }

            tokens.Add(CutRepeats(token));
        }

        if (letters.Length > 0)
        {
            tokens.Add(CutRepeats(letters.ToString()));
        }

        return tokens;
    }

    /// <summary>
    /// True when the jejemon character at <paramref name="at"/> is part of a word: its run of letters and jejemon
    /// characters has at least one real letter (<c>p0ta</c> yes, <c>500</c> no), and a <c>!</c> or <c>|</c> isn't the
    /// run's last character (<c>gago!</c> keeps its <c>!</c>; the other reading catches it anyway).
    /// </summary>
    private static bool InsideWord(string chars, int at)
    {
        bool InRun(int i) => char.IsLetter(chars[i]) || Leet(chars[i]) != chars[i];
        var start = at;
        while (start > 0 && InRun(start - 1))
        {
            start--;
        }

        var end = at;
        while (end + 1 < chars.Length && InRun(end + 1))
        {
            end++;
        }

        var hasLetter = false;
        for (var i = start; i <= end; i++)
        {
            hasLetter |= char.IsLetter(chars[i]);
        }

        return hasLetter && !(at == end && chars[at] is '!' or '|');
    }

    /// <summary>Jejemon / leet spellings back to letters; other characters unchanged.</summary>
    private static char Leet(char c) => c switch
    {
        '0' => 'o',
        '1' or '!' or '|' => 'i',
        '3' => 'e',
        '4' or '@' => 'a',
        '5' or '$' => 's',
        '7' => 't',
        _ => c,
    };

    /// <summary>Three or more of the same letter become one: <c>gagooo</c> → <c>gago</c>.</summary>
    private static string CutRepeats(string token)
    {
        var cut = new StringBuilder(token.Length);
        for (var i = 0; i < token.Length; i++)
        {
            var run = 1;
            while (i + run < token.Length && token[i + run] == token[i])
            {
                run++;
            }

            cut.Append(token[i], run >= 3 ? 1 : run);
            i += run - 1;
        }

        return cut.ToString();
    }

    /// <summary>Every repeat becomes one letter (<c>fuuck</c> → <c>fuck</c>), for a second look.</summary>
    private static string Squeeze(string token)
    {
        var squeezed = new StringBuilder(token.Length);
        foreach (var c in token)
        {
            if (squeezed.Length == 0 || squeezed[^1] != c)
            {
                squeezed.Append(c);
            }
        }

        return squeezed.ToString();
    }

    private static bool IsInnocentAt(List<string> tokens, int i) => Innocent.Any(phrase => StartsAt(tokens, i, phrase));

    private static bool StartsAt(List<string> tokens, int i, string[] phrase)
    {
        if (i + phrase.Length > tokens.Count)
        {
            return false;
        }

        for (var j = 0; j < phrase.Length; j++)
        {
            if (tokens[i + j] != phrase[j])
            {
                return false;
            }
        }

        return true;
    }
}
