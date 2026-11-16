using Azure.Data.Tables;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api.Telemetry;

/// <summary>
/// What people type after picking "Others" (Table Storage <c>OtherJobNotes</c>): demand for jobs and trades we don't
/// list yet. Analytics only: a note never becomes a job, is never shown and is stored without the account (the uid only
/// counts the caller's limit), so deleting an account has nothing to clear here. One partition per trade
/// (<c>other</c> for a trade we don't offer), newest first. Here and not in SQL so a note never wakes the database.
/// </summary>
public sealed class OtherJobNoteTable(TelemetryTables tables, TimeProvider clock)
{
    /// <summary>Longest note kept; longer text is cut (the app cuts it first, <c>OtherJobNotes.MaxText</c>).</summary>
    public const int MaxNote = 100;

    /// <summary>Keeps what the caller typed under "Others". False for an empty note, a bad id or side.</summary>
    public async Task<bool> SaveAsync(OtherJobRequest request, CancellationToken cancellationToken)
    {
        var text = StoreText.Limit(request.Text, MaxNote);
        if (text is null || !StoreText.IsCode(request.CategoryId) || request.Side is not (Wire.Seeker or Wire.Professional))
        {
            return false;
        }

        var now = clock.GetUtcNow();
        await tables.OtherJobNotes.AddAsync(new TableEntity(request.CategoryId, NewestFirst.Key(now))
        {
            ["Side"] = request.Side,
            ["Text"] = text,
            ["CreatedAt"] = now,
        }, cancellationToken);
        return true;
    }
}
