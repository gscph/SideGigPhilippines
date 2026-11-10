namespace SideGigPh.Maui.Services;

/// <summary>
/// Keeps a Professional's photos. A photo's id is the SHA-256 of its bytes, so the same
/// image always gets the same id (anti-fraud rule 2). Today files on the device; Firebase
/// Storage later, where the duplicate check also runs across all pros.
/// </summary>
public interface IPhotoStore
{
    /// <summary>Saves the photo and returns its id. Saving the same bytes again returns the same id.</summary>
    Task<string> SaveAsync(byte[] bytes);

    /// <summary>An image source for the photo (a data URL today), or null if it's gone.</summary>
    Task<string?> GetUrlAsync(string photoId);

    Task DeleteAsync(string photoId);
}
