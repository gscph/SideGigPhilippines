using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// How to take the selfie, shown before the camera opens (on Android the phone's own camera app takes it, and Tarbaho
/// can't draw over it): a face in a dashed oval and three short tips (face in the middle, good light, nothing covering
/// the face). On iPhone the same oval is also drawn over the live camera (<c>Platforms/iOS/IosPhotoPicker</c>).
/// </summary>
public partial class SelfieGuide : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>Show the oval picture (left out when their photo already shows above the tips).</summary>
    [Parameter] public bool ShowPicture { get; set; } = true;
}
