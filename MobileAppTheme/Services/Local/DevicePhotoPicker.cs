using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// The phone's own photo picker via MAUI <see cref="MediaPicker"/>. Android 13+ and iOS use the
/// system photo picker, which needs no permission (the user chooses each photo). Android 12 and
/// older need READ_EXTERNAL_STORAGE, so it is checked and asked for first. Photos are scaled down
/// and recompressed so they stay small enough to show and upload (at most 1280px, JPEG 82, upright, no metadata). <see cref="TakeSelfieAsync"/> asks for the camera and
/// opens it with MAUI's <see cref="MediaPicker.CapturePhotoAsync"/>, which can't choose the lens (an app may wrap this
/// adapter to ask for the front one, as Tarbaho does on Android).
/// </summary>
public sealed class DevicePhotoPicker(ILogger<DevicePhotoPicker> logger) : IPhotoPicker
{
    private const int MaxSide = 1280;
    private const int JpegQuality = 82;

    // Android 13 (API 33) brought the permission-free system photo picker.
    private const int FirstAndroidWithPhotoPicker = 33;

    public async Task<PhotoPick> PickAsync(int maxCount)
    {
        try
        {
            if (!await EnsurePermissionAsync())
            {
                logger.LogWarning("Photo permission denied.");
                return PhotoPick.PermissionDenied;
            }

            var options = new MediaPickerOptions
            {
                SelectionLimit = maxCount,
                MaximumWidth = MaxSide,
                MaximumHeight = MaxSide,
                CompressionQuality = JpegQuality,
                RotateImage = true,
                PreserveMetaData = false,
            };
            var files = await MainThread.InvokeOnMainThreadAsync(() => MediaPicker.Default.PickPhotosAsync(options));
            if (files is null || files.Count == 0)
            {
                return PhotoPick.Cancelled;
            }

            var photos = new List<byte[]>();
            foreach (var file in files.Take(maxCount))
            {
                photos.Add(await ReadAsync(file));
            }

            return new PhotoPick(PhotoPickStatus.Picked, photos);
        }
        catch (PermissionException ex)
        {
            logger.LogWarning(ex, "Photo permission denied by the picker.");
            return PhotoPick.PermissionDenied;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not pick photos.");
            return PhotoPick.Failed;
        }
    }

    public Task<PhotoPick> TakeSelfieAsync() => CaptureAsync(MaxSide, JpegQuality, "a selfie");

    /// <summary>
    /// MAUI's camera: on iOS it opens on the back lens, the one a document is photographed with. It asks for the
    /// camera permission first, like the selfie.
    /// </summary>
    public Task<PhotoPick> TakeDocumentPhotoAsync() =>
        CaptureAsync(IPhotoPicker.DocumentMaxSide, IPhotoPicker.DocumentJpegQuality, "a document photo");

    private async Task<PhotoPick> CaptureAsync(int maxSide, int quality, string what)
    {
        try
        {
            var status = await DevicePermission.AskAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                logger.LogWarning("Camera permission denied.");
                return PhotoPick.CameraDenied;
            }

            var options = new MediaPickerOptions
            {
                MaximumWidth = maxSide,
                MaximumHeight = maxSide,
                CompressionQuality = quality,
                RotateImage = true,
                PreserveMetaData = false, // no EXIF or location: a selfie may be stored as sent
            };
            var file = await MainThread.InvokeOnMainThreadAsync(() => MediaPicker.Default.CapturePhotoAsync(options));
            return file is null ? PhotoPick.Cancelled : new PhotoPick(PhotoPickStatus.Picked, [await ReadAsync(file)]);
        }
        catch (PermissionException ex)
        {
            logger.LogWarning(ex, "Camera permission denied by the camera.");
            return PhotoPick.CameraDenied;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not take {What}.", what);
            return PhotoPick.Failed;
        }
    }

    public void OpenAppSettings() => AppInfo.Current.ShowSettingsUI();

    private static async Task<byte[]> ReadAsync(FileResult file)
    {
        await using var stream = await file.OpenReadAsync();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private static async Task<bool> EnsurePermissionAsync()
    {
        if (!OperatingSystem.IsAndroid() || OperatingSystem.IsAndroidVersionAtLeast(FirstAndroidWithPhotoPicker))
        {
            return true;
        }

        return await DevicePermission.AskAsync<Permissions.StorageRead>() == PermissionStatus.Granted;
    }
}
