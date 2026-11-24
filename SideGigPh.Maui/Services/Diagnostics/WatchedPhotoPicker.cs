using Microsoft.Extensions.Logging;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// The phone's <see cref="IPhotoPicker"/>, watched (<see cref="FlowWatch"/>): the camera and the photo picker leave the
/// app, so a camera that never comes back, or an app closed while it was open, is reported instead of looking like
/// nothing happened. How each one ended goes into the trail. The camera coming back with no photo
/// <see cref="CameraEmptyLimit"/> times in a row is reported too: a person who tries twice and gets nothing is almost
/// always hitting a problem (a camera app that can't save where it was asked), not changing their mind.
/// </summary>
public sealed class WatchedPhotoPicker(IPhotoPicker inner, FlowWatch watch, ILogger<WatchedPhotoPicker> logger) : IPhotoPicker
{
    public const string CameraFlow = "Camera";
    public const string PickerFlow = "Photo picker";

    /// <summary>How long the camera or picker may stay open before it is reported as stuck.</summary>
    public static readonly TimeSpan StuckAfter = TimeSpan.FromMinutes(3);

    /// <summary>How many selfies in a row may come back with no photo before it is reported.</summary>
    public const int CameraEmptyLimit = 2;

    private int _camerasEmpty;

    public Task<PhotoPick> PickAsync(int maxCount) => WatchAsync(PickerFlow, () => inner.PickAsync(maxCount));

    /// <summary>
    /// The document camera is watched like the selfie's, but backing out of it isn't counted: a worker often opens it
    /// to look, then fetches the ID or better light. Only an empty selfie camera twice in a row points at a problem.
    /// </summary>
    public Task<PhotoPick> TakeDocumentPhotoAsync() => WatchAsync(CameraFlow, inner.TakeDocumentPhotoAsync);

    public async Task<PhotoPick> TakeSelfieAsync()
    {
        var pick = await WatchAsync(CameraFlow, inner.TakeSelfieAsync);
        if (pick.Status == PhotoPickStatus.Picked)
        {
            _camerasEmpty = 0;
        }
        else if (pick.Status == PhotoPickStatus.Cancelled && ++_camerasEmpty == CameraEmptyLimit)
        {
            logger.LogError("The camera came back with no photo {Times} times in a row.", CameraEmptyLimit);
        }

        return pick;
    }

    public void OpenAppSettings() => inner.OpenAppSettings();

    public async Task<PhotoPick?> RecoverSelfieAsync()
    {
        var pick = await inner.RecoverSelfieAsync();
        if (pick is not null)
        {
            logger.LogInformation("{Flow}: a selfie taken while the app was closed, {Status}.", CameraFlow, pick.Status);
        }

        return pick;
    }

    private async Task<PhotoPick> WatchAsync(string flow, Func<Task<PhotoPick>> open)
    {
        using (watch.Start(flow, StuckAfter))
        {
            var pick = await open();
            logger.LogInformation("{Flow}: {Status}, {Count} photo(s).", flow, pick.Status, pick.Photos.Count);
            return pick;
        }
    }
}
