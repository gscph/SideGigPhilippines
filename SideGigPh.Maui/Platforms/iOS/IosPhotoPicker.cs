using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Extensions.Logging;
using MobileAppTheme.Services.Local;
using UIKit;

namespace SideGigPh.Maui.Platforms.iOS;

/// <summary>
/// The skeleton's <see cref="DevicePhotoPicker"/> with a selfie that opens on the front camera. MAUI's camera call can't
/// choose the lens, so this opens iOS's own camera screen (<see cref="UIImagePickerController"/>) on
/// <see cref="UIImagePickerControllerCameraDevice.Front"/> after the camera permission (<c>NSCameraUsageDescription</c>).
/// A dashed oval over the live camera shows where the face goes (<c>FaceGuide</c>). The photo is made small here, like on Android: drawn upright (the camera's orientation applied), in sRGB, at most
/// <see cref="MaxSide"/> pixels on the long side, JPEG quality 0.82, with no metadata (a redrawn image carries no EXIF
/// or location). The server checks it and stores it as sent (<c>POST photos/selfie</c>). The gallery stays the
/// skeleton's.
/// </summary>
public sealed class IosPhotoPicker(DevicePhotoPicker device, ILogger<IosPhotoPicker> logger) : IPhotoPicker
{
    private const double MaxSide = 1280;
    private const float JpegQuality = 0.82f;

    public Task<PhotoPick> PickAsync(int maxCount) => device.PickAsync(maxCount);

    public void OpenAppSettings() => device.OpenAppSettings();

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

    // Shows the camera over the app on the front lens; null when the person closed it without a photo.
    private static Task<UIImage?> OpenFrontCameraAsync()
    {
        if (!UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera))
        {
            throw new InvalidOperationException("This phone has no camera.");
        }

        var parent = Platform.GetCurrentUIViewController() ?? throw new InvalidOperationException("No screen to open the camera from.");
        var done = new TaskCompletionSource<UIImage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var camera = new UIImagePickerController
        {
            SourceType = UIImagePickerControllerSourceType.Camera,
            CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo,
            AllowsEditing = false,
            // Full screen, so it can't be swiped away without Cancel (which would leave the selfie button waiting).
            ModalPresentationStyle = UIModalPresentationStyle.FullScreen,
        };
        if (UIImagePickerController.IsCameraDeviceAvailable(UIImagePickerControllerCameraDevice.Front))
        {
            camera.CameraDevice = UIImagePickerControllerCameraDevice.Front;
        }

        camera.CameraOverlayView = FaceGuide(parent.View?.Window?.Bounds ?? UIScreen.MainScreen.Bounds);

        camera.FinishedPickingMedia += (_, e) =>
        {
            var image = e.OriginalImage;
            camera.DismissViewController(true, () => done.TrySetResult(image));
        };
        camera.Canceled += (_, _) => camera.DismissViewController(true, () => done.TrySetResult(null));
        parent.PresentViewController(camera, true, null);
        return done.Task;
    }

    // The selfie guide over the live camera: a dashed white oval where the face goes (the same oval as the app's own
    // guide picture, SelfieGuide). It only draws; taps go through to the camera's buttons.
    private static UIView FaceGuide(CGRect bounds)
    {
        double screenWidth = bounds.Width;
        double screenHeight = bounds.Height;
        var width = screenWidth * 0.62;
        var height = width * 1.3;
        var oval = new CGRect((screenWidth - width) / 2, screenHeight * 0.42 - height / 2, width, height);

        var view = new UIView(bounds) { UserInteractionEnabled = false, BackgroundColor = UIColor.Clear };
        view.Layer.AddSublayer(new CAShapeLayer
        {
            Path = UIBezierPath.FromOval(oval).CGPath,
            StrokeColor = UIColor.White.CGColor,
            FillColor = UIColor.Clear.CGColor,
            LineWidth = 3,
            LineDashPattern = [new NSNumber(10), new NSNumber(8)],
        });
        return view;
    }

    // Redrawn at most MaxSide pixels, upright and in sRGB, then saved as a JPEG with no metadata.
    private static byte[] Shrink(UIImage image)
    {
        double width = image.Size.Width * image.CurrentScale;
        double height = image.Size.Height * image.CurrentScale;
        var scale = Math.Min(1, MaxSide / Math.Max(width, height));
        var size = new CGSize(Math.Max(1, Math.Round(width * scale)), Math.Max(1, Math.Round(height * scale)));

        var format = UIGraphicsImageRendererFormat.PreferredFormat;
        format.Scale = 1;
        format.Opaque = true;
        format.PreferredRange = UIGraphicsImageRendererFormatRange.Standard;

        using var renderer = new UIGraphicsImageRenderer(size, format);
        using var jpeg = renderer.CreateJpeg(JpegQuality, _ => image.Draw(new CGRect(CGPoint.Empty, size)));
        return jpeg.ToArray();
    }
}
