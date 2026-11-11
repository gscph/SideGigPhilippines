namespace MobileAppTheme.Services;

/// <summary>
/// Keeps the user's photos. A photo's id is the SHA-256 of its bytes, so the same
/// image always gets the same id and apps can reject duplicates. Today files on the device; a
/// cloud store (e.g. Firebase Storage) later, where the duplicate check can run across all users.
/// </summary>
public interface IPhotoStore
{
    /// <summary>Saves the photo and returns its id. Saving the same bytes again returns the same id.</summary>
    Task<string> SaveAsync(byte[] bytes);

    /// <summary>An image source for the photo (a data URL today), or null if it's gone.</summary>
    Task<string?> GetUrlAsync(string photoId);

    Task DeleteAsync(string photoId);
}
