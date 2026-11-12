namespace MobileAppTheme.Services;

/// <summary>
/// Keeps the user's photos. A photo's id is the SHA-256 of its bytes, so the same
/// image always gets the same id and apps can reject duplicates. Files on the device
/// (<c>FilePhotoStore</c>) or a cloud store such as blob storage behind an API, where the
/// duplicate check runs across all users.
/// </summary>
public interface IPhotoStore
{
    /// <summary>
    /// Saves the photo and returns its id. Saving the same bytes again returns the same id.
    /// Throws <see cref="PhotoStoreException"/> when the photo can't be stored (e.g. no network).
    /// </summary>
    Task<string> SaveAsync(byte[] bytes);

    /// <summary>An image source for the photo (a data URL or an https URL), or null if it's gone.</summary>
    Task<string?> GetUrlAsync(string photoId);

    /// <summary>Removes the photo. A photo that is already gone is not an error.</summary>
    Task DeleteAsync(string photoId);
}

/// <summary>
/// A photo could not be stored. <see cref="Taken"/> means the store refused it because another
/// account already has the same photo (duplicate check); otherwise it was a network or server
/// problem worth retrying. <see cref="Exception.InnerException"/> has the cause when there is one.
/// </summary>
public sealed class PhotoStoreException(string message, Exception? inner = null, bool taken = false) : Exception(message, inner)
{
    public bool Taken { get; } = taken;
}
