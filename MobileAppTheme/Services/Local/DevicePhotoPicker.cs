using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// The phone's own photo picker via MAUI <see cref="MediaPicker"/>. Android 13+ and iOS use the
/// system photo picker, which needs no permission (the user chooses each photo). Android 12 and
/// older need READ_EXTERNAL_STORAGE, so it is checked and asked for first. Photos are scaled down
/// and recompressed so they stay small enough to show and upload.
/// </summary>
public sealed class DevicePhotoPicker(ILogger<DevicePhotoPicker> logger) : IPhotoPicker
{
    private const int MaxSide = 1280;
    private const int JpegQuality = 80;

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
            };
            var files = await MainThread.InvokeOnMainThreadAsync(() => MediaPicker.Default.PickPhotosAsync(options));
            if (files is null || files.Count == 0)
            {
                return PhotoPick.Cancelled;
            }

            var photos = new List<byte[]>();
            foreach (var file in files.Take(maxCount))
            {
                await using var stream = await file.OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                photos.Add(buffer.ToArray());
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

    public void OpenAppSettings() => AppInfo.Current.ShowSettingsUI();

    private static async Task<bool> EnsurePermissionAsync()
    {
        if (!OperatingSystem.IsAndroid() || OperatingSystem.IsAndroidVersionAtLeast(FirstAndroidWithPhotoPicker))
        {
            return true;
        }

        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.StorageRead>();
            }

            return status == PermissionStatus.Granted;
        });
    }
}
