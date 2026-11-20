namespace SideGigPh.Maui.Models;

/// <summary>
/// What happened when someone picked photos (work photos or a profile photo): how many were added, and
/// <see cref="Problem"/> says why nothing (or not everything) was added, so the notice can say what to do.
/// </summary>
public sealed record PhotoAddResult(int Added, PhotoProblem Problem = PhotoProblem.None)
{
    public bool Failed => Problem != PhotoProblem.None;

    /// <summary>The upload failed in a way the same photo may get through on another go (no internet, server trouble).</summary>
    public bool CanRetry => Problem is PhotoProblem.NoInternet or PhotoProblem.ServerError;

    public static PhotoAddResult Nothing { get; } = new(0);

    /// <summary>A pick that brought no photos back: cancelled (no problem), no permission, or the gallery failed.</summary>
    public static PhotoAddResult From(PhotoPickStatus status) => new(0, status switch
    {
        PhotoPickStatus.Picked or PhotoPickStatus.Cancelled => PhotoProblem.None,
        PhotoPickStatus.PermissionDenied => PhotoProblem.NoPermission,
        PhotoPickStatus.CameraDenied => PhotoProblem.NoCamera,
        _ => PhotoProblem.CouldNotOpen,
    });

    /// <summary>An upload that failed: no internet, server trouble, or a photo the server can't use.</summary>
    public static PhotoAddResult From(PhotoStoreException ex) => new(0, ex.Failure switch
    {
        PhotoStoreFailure.NoInternet => PhotoProblem.NoInternet,
        PhotoStoreFailure.Rejected => PhotoProblem.Rejected,
        _ => PhotoProblem.ServerError,
    });
}

/// <summary>Why adding photos stopped.</summary>
public enum PhotoProblem
{
    None,

    /// <summary>The gallery didn't open or a photo couldn't be read.</summary>
    CouldNotOpen,

    /// <summary>The phone denied access to the photos.</summary>
    NoPermission,

    /// <summary>The phone denied the camera (for a selfie).</summary>
    NoCamera,

    /// <summary>The upload got no answer (no internet): the same photo can go again once connected.</summary>
    NoInternet,

    /// <summary>The server couldn't take it right now (busy, down): the same photo can go again in a moment.</summary>
    ServerError,

    /// <summary>The server can't use this photo (too big, not a readable picture): another photo is needed.</summary>
    Rejected,
}
