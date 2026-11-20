using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// Groups what people typed under "Others" so the most requested missing work shows first (the operator site's
/// Groups tab). Two steps, no I/O: notes whose cleaned text (<see cref="Clean"/>) is the same form one group; then,
/// when the texts have embeddings, groups that mean the same ("linis ng pool" and "pool cleaning") are merged
/// (<see cref="SameMeaning"/>). Without embeddings (Azure not set up or down) the exact groups are the answer.
/// </summary>
public static class OtherJobGrouping
{
    /// <summary>
    /// Two texts mean the same from this cosine similarity of their embeddings. High on purpose: with
    /// text-embedding-3-small, a job and its translation or rewording ("linis ng pool" / "pool cleaning") score above
    /// it, while two different jobs on the same thing ("pool cleaning" / "pool repair") score below. A missed merge
    /// only leaves two rows the operator reads side by side; a wrong merge hides a request, so err on missing.
    /// </summary>
    public const double SameMeaning = 0.82;

    /// <summary>The most examples a group shows.</summary>
    public const int MaxExamples = 5;

    /// <summary>The most trades a group lists.</summary>
    public const int MaxTrades = 3;

    /// <summary>The longest cleaned text (<c>OtherJobTextVectors.TextKey</c>): a note is at most this long, but taking accents apart can lengthen it.</summary>
    public const int MaxKey = 100;

    /// <summary>One note as the grouping needs it: what was typed, the side and the trade it was typed under.</summary>
    public sealed record Note(string Text, string Side, string TradeId);

    /// <summary>
    /// <c>"  Linis ng PÓOL!! "</c> → <c>"linis ng pool"</c>: lower case, accents off (ñ → n), anything but letters and
    /// digits made a space, spaces collapsed. A text with no letter or digit keeps its trimmed lower-case self.
    /// Cut to <see cref="MaxKey"/>.
    /// </summary>
    public static string Clean(string text)
    {
        var plain = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            plain.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        var cleaned = string.Join(' ', plain.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var key = cleaned.Length > 0 ? cleaned : text.Trim().ToLowerInvariant();
        return key.Length > MaxKey ? key[..MaxKey].TrimEnd() : key;
    }

    /// <summary>
    /// The groups, biggest first (then by label). <paramref name="vectors"/> holds embeddings by cleaned text; a text
    /// without one is never merged. Merging is greedy from the biggest group: each group that is still alone takes in
    /// every smaller lone group at least <see cref="SameMeaning"/> close to it (compared with the biggest group only,
    /// so a chain of near texts can't drift into something else). <paramref name="tradeLabel"/> names a trade id.
    /// </summary>
    public static List<AdminOtherJobGroupDto> Group(
        IReadOnlyList<Note> notes,
        IReadOnlyDictionary<string, float[]> vectors,
        Func<string, string> tradeLabel)
    {
        var exact = notes
            .GroupBy(n => Clean(n.Text), StringComparer.Ordinal)
            .Select(g => (Key: g.Key, Notes: g.ToList()))
            .OrderByDescending(g => g.Notes.Count).ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        var taken = new bool[exact.Count];
        var groups = new List<AdminOtherJobGroupDto>();
        for (var i = 0; i < exact.Count; i++)
        {
            if (taken[i])
            {
                continue;
            }

            var members = new List<(string Key, List<Note> Notes)> { exact[i] };
            if (vectors.TryGetValue(exact[i].Key, out var seed))
            {
                for (var j = i + 1; j < exact.Count; j++)
                {
                    if (!taken[j] && vectors.TryGetValue(exact[j].Key, out var other) && Cosine(seed, other) >= SameMeaning)
                    {
                        taken[j] = true;
                        members.Add(exact[j]);
                    }
                }
            }

            groups.Add(ToGroup(members, tradeLabel));
        }

        return groups
            .OrderByDescending(g => g.Count).ThenBy(g => g.Label, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The cosine similarity of two vectors, −1 to 1; 0 when they differ in size or either is all zeros.</summary>
    public static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }

        double dot = 0, aa = 0, bb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            aa += a[i] * a[i];
            bb += b[i] * b[i];
        }

        return aa == 0 || bb == 0 ? 0 : dot / Math.Sqrt(aa * bb);
    }

    /// <summary>A vector as float32 little-endian bytes (<c>OtherJobTextVectors.Vector</c>).</summary>
    public static byte[] ToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        for (var i = 0; i < vector.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), vector[i]);
        }

        return bytes;
    }

    /// <summary>The vector stored by <see cref="ToBytes"/>.</summary>
    public static float[] FromBytes(byte[] bytes)
    {
        var vector = new float[bytes.Length / sizeof(float)];
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * sizeof(float)));
        }

        return vector;
    }

    /// <summary>
    /// One group from its exact groups (biggest first): the label is the text typed most often, ties to the first in
    /// text order; the examples are each exact group's own most typed text.
    /// </summary>
    private static AdminOtherJobGroupDto ToGroup(List<(string Key, List<Note> Notes)> members, Func<string, string> tradeLabel)
    {
        var all = members.SelectMany(m => m.Notes).ToList();
        return new AdminOtherJobGroupDto(
            Label: MostTyped(all),
            Count: all.Count,
            Examples: members.Take(MaxExamples).Select(m => MostTyped(m.Notes)).ToList(),
            Households: all.Count(n => n.Side == Wire.Seeker),
            Workers: all.Count(n => n.Side == Wire.Professional),
            Trades: all
                .GroupBy(n => n.TradeId, StringComparer.Ordinal)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Take(MaxTrades)
                .Select(g => new AdminCountDto(g.Key, tradeLabel(g.Key), g.Count()))
                .ToList());
    }

    private static string MostTyped(IEnumerable<Note> notes) => notes
        .GroupBy(n => n.Text.Trim(), StringComparer.Ordinal)
        .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
        .First().Key;
}
