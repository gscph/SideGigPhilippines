using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// Sends what people type after picking a trade's "Others" job, for analytics. It never holds up the interview: empty
/// text sends nothing and a failure is only logged.
/// </summary>
public sealed class OtherJobNotes(IOtherJobNoteRepository repository, ILogger<OtherJobNotes> logger)
{
    /// <summary>Longest text sent (the server's <c>OtherJobNoteTable.MaxNote</c>).</summary>
    public const int MaxText = 100;

    public async Task NoteAsync(string? categoryId, UserRole side, string? text)
    {
        var typed = text?.Trim();
        if (string.IsNullOrEmpty(categoryId) || string.IsNullOrEmpty(typed))
        {
            return;
        }

        try
        {
            await repository.SaveAsync(new OtherJobNote(categoryId, side, typed.Length > MaxText ? typed[..MaxText] : typed));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't send the Others note for {CategoryId}.", categoryId);
        }
    }
}
