using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// What people type after picking a trade's "Others" job (<c>OtherJobNotes</c>). Analytics only: a note never becomes
/// a job and is never shown. Deleting an account keeps its notes without the uid (<c>UserStore.DeleteAsync</c>).
/// </summary>
public sealed class OtherJobNoteStore(SideGigPhDb db)
{
    /// <summary>Longest note kept (the column's length); longer text is cut.</summary>
    public const int MaxNote = 100;

    /// <summary>Keeps what the caller typed under "Others" in a trade. False for an empty note, a bad id or side.</summary>
    public async Task<bool> SaveAsync(string uid, OtherJobRequest request, CancellationToken cancellationToken)
    {
        var text = StoreText.Limit(request.Text, MaxNote);
        if (text is null || !StoreText.IsCode(request.CategoryId) || request.Side is not (Wire.Seeker or Wire.Professional))
        {
            return false;
        }

        db.OtherJobNotes.Add(new OtherJobNoteRow
        {
            Uid = uid,
            Side = request.Side,
            CategoryId = request.CategoryId,
            Text = text,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
