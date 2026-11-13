using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Where the "Others" notes go (the server's <c>OtherJobNotes</c>, analytics only).</summary>
public interface IOtherJobNoteRepository
{
    /// <summary>Keeps what someone typed after picking "Others". Throws when it wasn't saved.</summary>
    Task SaveAsync(OtherJobNote note);
}
