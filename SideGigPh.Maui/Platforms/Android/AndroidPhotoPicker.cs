using Android.App;
using Android.Content;
using Microsoft.Extensions.Logging;
using MobileAppTheme.Localization;
using MobileAppTheme.Services.Local;
using SideGigPh.Maui.Services.Localization;
using Application = Android.App.Application;
using File = Java.IO.File;

namespace SideGigPh.Maui.Platforms.Android;

/// <summary>
/// The skeleton's <see cref="DevicePhotoPicker"/> with a selfie taken in Tarbaho's own camera screen,
/// <see cref="SelfieCameraActivity"/>: the front lens only (no switching to the back lens), a dashed oval and a hint over
/// the live camera. The phone's camera app could not force the front lens or show the oval. The photo comes back
/// upright, at most 1280 pixels, JPEG quality 82 with no metadata: the server checks it and stores it as sent
/// (<c>POST photos/selfie</c>), never resized. Needs <c>MainActivity.OnActivityResult</c> to call
/// <see cref="HandleActivityResult"/> and the CAMERA permission in the manifest, asked here when "Take a selfie" is
/// tapped (the reason is on screen then). The app stays on screen while the person poses, so a one-time ("Only this
/// time") grant is not taken back mid-pose, which lost selfies with the camera app (22 Nov 2026).
/// <para>
/// A phone short of memory can still close the app while the camera is open. The photo's path is kept in
/// <see cref="Preferences"/> before the camera opens, so the selfie question picks the photo up when the app starts
/// again (<see cref="RecoverSelfieAsync"/>).
/// </para>
/// </summary>
public sealed class AndroidPhotoPicker(DevicePhotoPicker device, AppText text, ILogger<AndroidPhotoPicker> logger) : IPhotoPicker
{
    /// <summary>The request code the camera's answer comes back with.</summary>
    public const int SelfieRequestCode = 4711;

    private static TaskCompletionSource<Result>? _pending;

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

            var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No activity to open the camera from.");
            var file = new File(Application.Context.CacheDir, $"selfie-{Guid.NewGuid():N}.jpg");
            var intent = new Intent(activity, typeof(SelfieCameraActivity));
            intent.PutExtra(SelfieCameraActivity.ExtraOutput, file.AbsolutePath);
            intent.PutExtra(SelfieCameraActivity.ExtraHint, text.Get(Copy.SelfieCameraHint));
            intent.PutExtra(SelfieCameraActivity.ExtraShutter, text.Get(Copy.SelfieCameraShutter));
            intent.PutExtra(SelfieCameraActivity.ExtraCancel, text.Get(CommonCopy.Cancel));

            var taken = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = taken;
            Preferences.Default.Set(PendingSelfieKey, file.AbsolutePath);
            await MainThread.InvokeOnMainThreadAsync(() => activity.StartActivityForResult(intent, SelfieRequestCode));
            var result = await taken.Task;
            Preferences.Default.Remove(PendingSelfieKey);
            try
            {
                if (result == Result.Canceled)
                {
                    logger.LogInformation("The camera closed without a photo.");
                    return PhotoPick.Cancelled;
                }

                if (result != Result.Ok)
                {
                    logger.LogError("The selfie camera could not take a photo.");
                    return PhotoPick.Failed;
                }

                if (!file.Exists() || file.Length() == 0)
                {
                    logger.LogError("The selfie camera said OK but saved no photo (file {State}).", file.Exists() ? "empty" : "missing");
                    return PhotoPick.Failed;
                }

                return new PhotoPick(PhotoPickStatus.Picked, [await System.IO.File.ReadAllBytesAsync(file.AbsolutePath)]);
            }
            finally
            {
                file.Delete();
            }
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
                return new PhotoPick(PhotoPickStatus.Picked, [await System.IO.File.ReadAllBytesAsync(path)]);
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

    /// <summary>Called from <c>MainActivity.OnActivityResult</c>: the camera closed, with a photo, without, or failed.</summary>
    public static void HandleActivityResult(int requestCode, Result resultCode)
    {
        if (requestCode == SelfieRequestCode)
        {
            Interlocked.Exchange(ref _pending, null)?.TrySetResult(resultCode);
        }
    }
}
