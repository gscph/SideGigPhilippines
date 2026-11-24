using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// Tells the person why photos weren't added (no permission, with a Settings link; no internet; server trouble; a photo the server can't use;
/// gallery didn't open). Silent otherwise.
/// </summary>
public partial class PhotoAddNotice : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Inject] private IPhotoPicker Picker { get; set; } = default!;

    [Parameter] public PhotoAddResult? Result { get; set; }

    /// <summary>The photo was a selfie: one the server refused is taken again, not swapped for another photo.</summary>
    [Parameter] public bool Selfie { get; set; }

    private LocalizedText Message => Result?.Problem switch
    {
        PhotoProblem.NoInternet => Copy.PhotosNoInternet,
        PhotoProblem.ServerError => Copy.PhotosServerError,
        PhotoProblem.Rejected => Selfie ? Copy.SelfieRejected : Copy.PhotosRejected,
        _ => Copy.PhotosFailed,
    };
}
