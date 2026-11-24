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

    /// <summary>
    /// Opens the back camera for one photo of a document or a card (never the gallery: a photo taken now, not an old
    /// one or a screenshot). It comes back bigger than a picked photo, at most <see cref="DocumentMaxSide"/> pixels, so
    /// small print stays readable. Where an adapter has no such camera it answers <see cref="PhotoPick.Failed"/>.
    /// </summary>
    Task<PhotoPick> TakeDocumentPhotoAsync() => Task.FromResult(PhotoPick.Failed);

    /// <summary>The long side of a document photo, in pixels (<see cref="TakeDocumentPhotoAsync"/>).</summary>
    public const int DocumentMaxSide = 2000;

    /// <summary>The JPEG quality of a document photo: a little higher than a picked one, for small print.</summary>
    public const int DocumentJpegQuality = 85;

    /// <summary>Opens this app's page in the phone's Settings, where a denied permission can be turned on.</summary>
    void OpenAppSettings();

    /// <summary>
    /// The selfie the camera took while the phone closed this app (a phone short of memory closes an app in the
    /// background, and the camera is another app): saved where it was asked, but nobody was waiting for it. Picked once,
    /// resized like a taken one; null when there is none, or where the camera never runs as another app.
    /// </summary>
    Task<PhotoPick?> RecoverSelfieAsync() => Task.FromResult<PhotoPick?>(null);
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
