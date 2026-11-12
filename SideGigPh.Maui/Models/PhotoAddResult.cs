namespace SideGigPh.Maui.Models;

/// <summary>
/// What happened when someone picked photos (work photos or a profile photo). <see cref="Duplicates"/> counts photos
/// skipped because they were already added (anti-fraud rule 2: SHA-256 duplicate check); <see cref="Problem"/> says
/// why nothing (or not everything) was added, so the notice can say what to do.
/// </summary>
public sealed record PhotoAddResult(int Added, int Duplicates, PhotoProblem Problem = PhotoProblem.None)
{
    public bool Failed => Problem != PhotoProblem.None;

    public static PhotoAddResult Nothing { get; } = new(0, 0);

    /// <summary>A pick that brought no photos back: cancelled (no problem), no permission, or the gallery failed.</summary>
    public static PhotoAddResult From(PhotoPickStatus status) => new(0, 0, status switch
    {
        PhotoPickStatus.Picked or PhotoPickStatus.Cancelled => PhotoProblem.None,
        PhotoPickStatus.PermissionDenied => PhotoProblem.NoPermission,
        _ => PhotoProblem.CouldNotOpen,
    });

    /// <summary>An upload the photo store refused: taken by another account, or a network/server failure.</summary>
    public static PhotoAddResult From(PhotoStoreException ex) =>
        new(0, 0, ex.Taken ? PhotoProblem.Taken : PhotoProblem.UploadFailed);
}

/// <summary>Why adding photos stopped.</summary>
public enum PhotoProblem
{
    None,

    /// <summary>The gallery didn't open or a photo couldn't be read.</summary>
    CouldNotOpen,

    /// <summary>The phone denied access to the photos.</summary>
    NoPermission,

    /// <summary>The upload failed (no internet, server error): worth trying again.</summary>
    UploadFailed,

    /// <summary>Another account already uses this photo (anti-fraud rule 2).</summary>
    Taken,
}
