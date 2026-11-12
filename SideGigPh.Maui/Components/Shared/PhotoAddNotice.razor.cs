using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Tells the person why photos weren't added (no permission, with a Settings link; upload failed;
/// photo used by another account; gallery didn't open), or that duplicates were skipped. Silent otherwise.
/// </summary>
public partial class PhotoAddNotice : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Inject] private IPhotoPicker Picker { get; set; } = default!;

    [Parameter] public PhotoAddResult? Result { get; set; }

    private LocalizedText Message => Result?.Problem switch
    {
        PhotoProblem.UploadFailed => Copy.PhotosUploadFailed,
        PhotoProblem.Taken => Copy.PhotosTaken,
        _ => Copy.PhotosFailed,
    };
}
