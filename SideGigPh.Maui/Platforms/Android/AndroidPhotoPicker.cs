using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using AndroidX.ExifInterface.Media;
using Microsoft.Extensions.Logging;
using MobileAppTheme.Services.Local;
using Application = Android.App.Application;
using File = Java.IO.File;
using Path = System.IO.Path;

namespace SideGigPh.Maui.Platforms.Android;

/// <summary>
/// The skeleton's <see cref="DevicePhotoPicker"/> with a selfie that opens on the front camera. MAUI's camera call can't
/// choose the lens, so this starts the phone's camera app itself with the front-camera hints most camera apps (Samsung's
/// included) read; one that ignores them opens its last-used lens and the person flips it. The photo comes back upright,
/// at most <see cref="MaxSide"/> pixels, JPEG quality 82 with no metadata (a compressed bitmap carries no EXIF or
/// location): the server checks it and stores it as sent (<c>POST photos/selfie</c>), never resized. Needs <c>MainActivity.OnActivityResult</c> to call
/// <see cref="HandleActivityResult"/> and the IMAGE_CAPTURE query in the manifest; the file goes through MAUI's own file
/// provider (its cache path).
/// <para>
/// The camera app takes the photo, so this app asks for no camera permission (the manifest has none: with CAMERA
/// declared, Android would refuse the camera app until it was granted). That also matters because Android closes an app
/// holding an "Only this time" permission about a minute after it goes to the background, which is while the person
/// poses: the selfie came back to a fresh app and was lost (22 Nov 2026).
/// </para>
/// <para>
/// A phone short of memory can still close the app while the camera is open. The photo's path is kept in
/// <see cref="Preferences"/> before the camera opens, so the selfie question picks the photo up when the app starts
/// again (<see cref="RecoverSelfieAsync"/>).
/// </para>
/// </summary>
public sealed class AndroidPhotoPicker(DevicePhotoPicker device, ILogger<AndroidPhotoPicker> logger) : IPhotoPicker
{
    /// <summary>The request code the camera's answer comes back with.</summary>
    public const int SelfieRequestCode = 4711;

    private const int MaxSide = 1280;
    private const int JpegQuality = 82;

    private static TaskCompletionSource<bool>? _pending;

    // Where the camera was asked to save the selfie, kept until it came back (see RecoverSelfieAsync).
    private const string PendingSelfieKey = "selfie.pendingPath";

    /// <summary>A selfie older than this when the app comes back is not picked up (the person moved on).</summary>
    private static readonly TimeSpan RecoverFor = TimeSpan.FromHours(1);

    public Task<PhotoPick> PickAsync(int maxCount) => device.PickAsync(maxCount);

    public void OpenAppSettings() => device.OpenAppSettings();

    public async Task<PhotoPick> TakeSelfieAsync()
    {
        try
        {
            var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No activity to open the camera from.");
            var context = Application.Context;
            var file = new File(context.CacheDir, $"selfie-{Guid.NewGuid():N}.jpg");
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, $"{context.PackageName}.fileProvider", file)!;

            var intent = new Intent(MediaStore.ActionImageCapture);
            intent.PutExtra(MediaStore.ExtraOutput, uri);
            intent.AddFlags(ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
            intent.PutExtra("android.intent.extras.CAMERA_FACING", 1);
            intent.PutExtra("android.intent.extras.LENS_FACING_FRONT", 1);
            intent.PutExtra("android.intent.extra.USE_FRONT_CAMERA", true);

            var taken = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = taken;
            Preferences.Default.Set(PendingSelfieKey, file.AbsolutePath);
            await MainThread.InvokeOnMainThreadAsync(() => activity.StartActivityForResult(intent, SelfieRequestCode));
            var ok = await taken.Task;
            Preferences.Default.Remove(PendingSelfieKey);
            if (!ok)
            {
                logger.LogInformation("The camera closed without a photo.");
                return PhotoPick.Cancelled;
            }

            if (!file.Exists() || file.Length() == 0)
            {
                // The camera app said it took the photo but didn't save it where it was asked: looks like nothing
                // happened to the person, so it must reach the error reports.
                logger.LogError("The camera said OK but saved no photo (file {State}).", file.Exists() ? "empty" : "missing");
                return PhotoPick.Failed;
            }

            try
            {
                return new PhotoPick(PhotoPickStatus.Picked, [await Task.Run(() => Shrink(file.AbsolutePath))]);
            }
            finally
            {
                file.Delete();
            }
        }
        catch (ActivityNotFoundException ex)
        {
            logger.LogError(ex, "No camera app to take a selfie with.");
            return PhotoPick.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not take a selfie.");
            return PhotoPick.Failed;
        }
    }

    public async Task<PhotoPick?> RecoverSelfieAsync()
    {
        try
        {
            var path = Preferences.Default.Get<string?>(PendingSelfieKey, null);
            if (path is null || _pending is not null)
            {
                return null; // none waiting, or the camera is open now and its answer will come the usual way
            }

            Preferences.Default.Remove(PendingSelfieKey);
            var file = new File(path);
            try
            {
                var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(file.LastModified());
                if (!file.Exists() || file.Length() == 0 || age > RecoverFor)
                {
                    return null;
                }

                logger.LogWarning("The app was closed while the camera was open; picking up the selfie it took.");
                return new PhotoPick(PhotoPickStatus.Picked, [await Task.Run(() => Shrink(path))]);
            }
            finally
            {
                file.Delete();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not pick up the selfie taken while the app was closed.");
            return null;
        }
    }

    /// <summary>Called from <c>MainActivity.OnActivityResult</c>: the camera closed, with a photo or without.</summary>
    public static void HandleActivityResult(int requestCode, Result resultCode)
    {
        if (requestCode == SelfieRequestCode)
        {
            Interlocked.Exchange(ref _pending, null)?.TrySetResult(resultCode == Result.Ok);
        }
    }

    // Decoded at a fraction of full size first (a 50 MP photo would not fit in memory), then turned upright
    // from the EXIF orientation, scaled to MaxSide and saved as a JPEG with no metadata.
    private static byte[] Shrink(string path)
    {
        var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeFile(path, bounds);
        var sample = 1;
        while (Math.Max(bounds.OutWidth, bounds.OutHeight) / (sample * 2) >= MaxSide)
        {
            sample *= 2;
        }

        using var decoded = BitmapFactory.DecodeFile(path, new BitmapFactory.Options { InSampleSize = sample })
            ?? throw new InvalidOperationException("The camera's photo couldn't be read.");

        var matrix = new Matrix();
        matrix.PostRotate(new ExifInterface(path).GetAttributeInt(ExifInterface.TagOrientation, ExifInterface.OrientationNormal) switch
        {
            ExifInterface.OrientationRotate90 => 90,
            ExifInterface.OrientationRotate180 => 180,
            ExifInterface.OrientationRotate270 => 270,
            _ => 0,
        });
        var scale = Math.Min(1f, MaxSide / (float)Math.Max(decoded.Width, decoded.Height));
        matrix.PostScale(scale, scale);

        using var upright = Bitmap.CreateBitmap(decoded, 0, 0, decoded.Width, decoded.Height, matrix, true);
        using var output = new MemoryStream();
        upright.Compress(Bitmap.CompressFormat.Jpeg!, JpegQuality, output);
        return output.ToArray();
    }
}
