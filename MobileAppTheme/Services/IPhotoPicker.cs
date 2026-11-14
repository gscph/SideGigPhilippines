namespace MobileAppTheme.Services;

/// <summary>Lets the user choose photos from their phone's gallery, or take a selfie.</summary>
public interface IPhotoPicker
{
    /// <summary>
    /// Asks for the gallery permission when the phone needs one, then opens the picker.
    /// The photos come back resized for upload.
    /// </summary>
    Task<PhotoPick> PickAsync(int maxCount);

    /// <summary>
    /// Asks for the camera permission, then opens the camera for one photo of the user, on the front lens where the
    /// phone lets the app choose. The photo comes back resized for upload, like a picked one.
    /// </summary>
    Task<PhotoPick> TakeSelfieAsync();

    /// <summary>Opens this app's page in the phone's Settings, where a denied permission can be turned on.</summary>
    void OpenAppSettings();
}

/// <summary>What a <see cref="IPhotoPicker.PickAsync"/> call ended with.</summary>
public enum PhotoPickStatus
{
    /// <summary>The user picked one or more photos.</summary>
    Picked,

    /// <summary>The user closed the picker without choosing.</summary>
    Cancelled,

    /// <summary>The phone said no to reading photos (the user denied it, or it's off in Settings).</summary>
    PermissionDenied,

    /// <summary>The picker couldn't open or a photo couldn't be read; the adapter logged why.</summary>
    Failed,

    /// <summary>The phone said no to the camera (the user denied it, or it's off in Settings).</summary>
    CameraDenied,
}

/// <summary>The outcome of a pick and, when <see cref="PhotoPickStatus.Picked"/>, the photos' bytes.</summary>
public sealed record PhotoPick(PhotoPickStatus Status, IReadOnlyList<byte[]> Photos)
{
    public static PhotoPick Cancelled { get; } = new(PhotoPickStatus.Cancelled, []);

    public static PhotoPick PermissionDenied { get; } = new(PhotoPickStatus.PermissionDenied, []);

    public static PhotoPick Failed { get; } = new(PhotoPickStatus.Failed, []);

    public static PhotoPick CameraDenied { get; } = new(PhotoPickStatus.CameraDenied, []);
}
