using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api.Insights;

/// <summary>
/// The operator site's "Others" groups (<c>GET ops/other-jobs/groups</c>): reads the newest notes, finds each distinct
/// cleaned text's embedding in the cache, asks the model (<see cref="ITextEmbedder"/>) only for the new ones and keeps
/// them, then groups (<see cref="OtherJobGrouping"/>). With no model, or one that doesn't answer, the groups are the
/// exact matches and <see cref="AdminOtherJobGroupsDto.UsedAi"/> is false.
/// </summary>
public sealed class OtherJobInsights(AdminStore notes, OtherJobVectorStore cache, ITextEmbedder embedder)
{
    public async Task<AdminOtherJobGroupsDto> GroupsAsync(string? filter, int? days, CancellationToken cancellationToken)
    {
        var read = await notes.OtherJobNotesToGroupAsync(filter, days, DateTimeOffset.UtcNow, cancellationToken);
        var keys = read.Notes.Select(n => OtherJobGrouping.Clean(n.Text)).Distinct(StringComparer.Ordinal).ToList();
        var vectors = await VectorsAsync(keys, cancellationToken);
        return new AdminOtherJobGroupsDto(
            OtherJobGrouping.Group(read.Notes, vectors, read.TradeLabel),
            read.Notes.Count,
            read.Capped,
            UsedAi: keys.Count > 0 && keys.All(vectors.ContainsKey));
    }

    /// <summary>Every key's vector that is cached or the model gave now (none cached under <see cref="NoTextEmbedder"/>'s model).</summary>
    private async Task<IReadOnlyDictionary<string, float[]>> VectorsAsync(List<string> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return new Dictionary<string, float[]>();
        }

        var vectors = await cache.FindAsync(keys, embedder.Model, cancellationToken);
        var missing = keys.Where(k => !vectors.ContainsKey(k)).ToList();
        if (missing.Count == 0 || await embedder.EmbedAsync(missing, cancellationToken) is not { } made)
        {
            return vectors;
        }

        var fresh = missing.Zip(made).ToDictionary(p => p.First, p => p.Second, StringComparer.Ordinal);
        await cache.SaveAsync(fresh, embedder.Model, cancellationToken);
        foreach (var (key, vector) in fresh)
        {
            vectors[key] = vector;
        }

        return vectors;
    }
}
