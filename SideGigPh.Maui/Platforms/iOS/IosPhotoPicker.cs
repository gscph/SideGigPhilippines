using CoreGraphics;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services.Localization;
using AppTheme = SideGigPh.Maui.Components.Layout.AppTheme;
using UIKit;

namespace SideGigPh.Maui.Platforms.iOS;

/// <summary>
/// The skeleton's <see cref="DevicePhotoPicker"/> with a selfie taken in Tarbaho's own front camera,
/// <see cref="IosSelfieCamera"/>, after the camera permission (<c>NSCameraUsageDescription</c>): no lens switch, and the
/// shutter works only while a face fills the green oval. The photo is made small here, like on Android: drawn upright (the camera's orientation applied), in sRGB, at most
/// <see cref="MaxSide"/> pixels on the long side, JPEG quality 0.82, with no metadata (a redrawn image carries no EXIF
/// or location). The server checks it and stores it as sent (<c>POST photos/selfie</c>). The gallery stays the
/// skeleton's.
/// </summary>
public sealed class IosPhotoPicker(DevicePhotoPicker device, AppText text, ILogger<IosPhotoPicker> logger) : IPhotoPicker
{
    private const double MaxSide = 1280;
    private const float JpegQuality = 0.82f;

    public Task<PhotoPick> PickAsync(int maxCount) => device.PickAsync(maxCount);

    public void OpenAppSettings() => device.OpenAppSettings();

    /// <summary>A badge proof (ID, clearance): the skeleton's back camera, at document size.</summary>
    public Task<PhotoPick> TakeDocumentPhotoAsync() => device.TakeDocumentPhotoAsync();

    public async Task<PhotoPick> TakeSelfieAsync()
    {
        try
        {
            var status = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var current = await Permissions.CheckStatusAsync<Permissions.Camera>();
                return current == PermissionStatus.Granted ? current : await Permissions.RequestAsync<Permissions.Camera>();
            });
            if (status != PermissionStatus.Granted)
            {
                logger.LogWarning("Camera permission denied.");
                return PhotoPick.CameraDenied;
            }

            var photo = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                using var image = await OpenFrontCameraAsync();
                return image is null ? null : Shrink(image);
            });
            return photo is null ? PhotoPick.Cancelled : new PhotoPick(PhotoPickStatus.Picked, [photo]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not take a selfie.");
            return PhotoPick.Failed;
        }
    }

    // Shows Tarbaho's own front camera over the app; null when the person closed it without a photo.
    private Task<UIImage?> OpenFrontCameraAsync()
    {
        var parent = Platform.GetCurrentUIViewController() ?? throw new InvalidOperationException("No screen to open the camera from.");
        var camera = new IosSelfieCamera(
            Copy.SelfieCameraHints.Select(text.Get).ToArray(),
            AppTheme.Style.Success,
            AppTheme.Style.Error,
            text.Get(CommonCopy.Cancel),
            text.Get(Copy.SelfieCameraShutter));
        parent.PresentViewController(camera, true, null);
        return camera.Result;
    }

    // Redrawn at most MaxSide pixels, upright and in sRGB, then saved as a JPEG with no metadata.
    private static byte[] Shrink(UIImage image)
    {
        double width = image.Size.Width * image.CurrentScale;
        double height = image.Size.Height * image.CurrentScale;
        var scale = Math.Min(1, MaxSide / Math.Max(width, height));
        var size = new CGSize(Math.Max(1, Math.Round(width * scale)), Math.Max(1, Math.Round(height * scale)));

        // A new image format, not PreferredFormat: in the .NET bindings that one is typed as the base
        // UIGraphicsRendererFormat, which has no Opaque or PreferredRange, and the renderer needs the image format.
        using var format = new UIGraphicsImageRendererFormat
        {
            Scale = 1,
            Opaque = true,
            PreferredRange = UIGraphicsImageRendererFormatRange.Standard,
        };

        using var renderer = new UIGraphicsImageRenderer(size, format);
        using var jpeg = renderer.CreateJpeg(JpegQuality, _ => image.Draw(new CGRect(CGPoint.Empty, size)));
        return jpeg.ToArray();
    }
}
