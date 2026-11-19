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
/// A photo could not be stored, and why (<see cref="Failure"/>), so the app can say what to do: wait for the internet,
/// try again later, or pick another photo. <see cref="Exception.InnerException"/> has the cause when there is one.
/// </summary>
public sealed class PhotoStoreException(string message, PhotoStoreFailure failure, Exception? inner = null) : Exception(message, inner)
{
    public PhotoStoreFailure Failure { get; } = failure;

    /// <summary>The store refused it because another account already has the same photo (duplicate check).</summary>
    public bool Taken => Failure == PhotoStoreFailure.Taken;
}

/// <summary>Why a photo could not be stored.</summary>
public enum PhotoStoreFailure
{
    /// <summary>The upload got no answer (no signal, no internet): the same photo can go again once connected.</summary>
    NoInternet,

    /// <summary>The store answered but couldn't take it now (busy, down, sign-in expired): the same photo can go again later.</summary>
    ServerError,

    /// <summary>The store refused this photo itself (too big, not a picture it can read): another photo is needed.</summary>
    Rejected,

    /// <summary>Another account already has the same photo (duplicate check).</summary>
    Taken,
}
