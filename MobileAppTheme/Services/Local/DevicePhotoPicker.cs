using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// The phone's own photo picker via MAUI <see cref="MediaPicker"/>. On Android 13+ that is the
/// system photo picker, which needs no permission. Photos are scaled down and recompressed so
/// they stay small enough to show and, later, to upload.
/// </summary>
public sealed class DevicePhotoPicker(ILogger<DevicePhotoPicker> logger) : IPhotoPicker
{
    private const int MaxSide = 1280;
    private const int JpegQuality = 80;

    public async Task<IReadOnlyList<byte[]>?> PickAsync(int maxCount)
    {
        try
        {
            var options = new MediaPickerOptions
            {
                SelectionLimit = maxCount,
                MaximumWidth = MaxSide,
                MaximumHeight = MaxSide,
                CompressionQuality = JpegQuality,
                RotateImage = true,
            };
            var files = await MainThread.InvokeOnMainThreadAsync(() => MediaPicker.Default.PickPhotosAsync(options));

            var photos = new List<byte[]>();
            foreach (var file in (files ?? []).Take(maxCount))
            {
                await using var stream = await file.OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                photos.Add(buffer.ToArray());
            }

            return photos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not pick photos.");
            return null;
        }
    }
}
