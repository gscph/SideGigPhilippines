namespace MobileAppTheme.Services;

/// <summary>Lets the user choose photos from their phone's gallery.</summary>
public interface IPhotoPicker
{
    /// <summary>
    /// The picked photos' bytes, resized for upload: empty if the user cancelled,
    /// null if the gallery couldn't be opened.
    /// </summary>
    Task<IReadOnlyList<byte[]>?> PickAsync(int maxCount);
}
