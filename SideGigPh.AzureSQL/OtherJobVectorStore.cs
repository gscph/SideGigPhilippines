using Microsoft.EntityFrameworkCore;

namespace SideGigPh.AzureSQL;

/// <summary>
/// The embeddings of "Others" texts already made (<c>OtherJobTextVectors</c>), by cleaned text
/// (<see cref="OtherJobGrouping.Clean"/>) and model, so each distinct text is sent to Azure once.
/// </summary>
public sealed class OtherJobVectorStore(SideGigPhDb db)
{
    /// <summary>Texts looked up per query, well under SQL Server's 2,100 parameters.</summary>
    private const int LookupBatch = 500;

    /// <summary>The stored vectors of <paramref name="keys"/> made by <paramref name="model"/>; texts without one are left out.</summary>
    public async Task<Dictionary<string, float[]>> FindAsync(IReadOnlyCollection<string> keys, string model, CancellationToken cancellationToken)
    {
        var found = new Dictionary<string, float[]>(StringComparer.Ordinal);
        foreach (var batch in keys.Chunk(LookupBatch))
        {
            var rows = await db.OtherJobTextVectors.AsNoTracking()
                .Where(v => v.Model == model && batch.Contains(v.TextKey))
                .Select(v => new { v.TextKey, v.Vector })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                found[row.TextKey] = OtherJobGrouping.FromBytes(row.Vector);
            }
        }

        return found;
    }

    /// <summary>
    /// Keeps new vectors. Another operator's look at the same moment may have stored some already: those are skipped,
    /// and if one slips in between the check and the save the whole save is dropped (the next look stores them).
    /// </summary>
    public async Task SaveAsync(IReadOnlyDictionary<string, float[]> vectors, string model, CancellationToken cancellationToken)
    {
        var known = await FindAsync(vectors.Keys.ToList(), model, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, vector) in vectors.Where(v => !known.ContainsKey(v.Key)))
        {
            db.OtherJobTextVectors.Add(new OtherJobTextVectorRow
            {
                TextKey = key,
                Model = model,
                Vector = OtherJobGrouping.ToBytes(vector),
                CreatedAt = now,
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }
}
