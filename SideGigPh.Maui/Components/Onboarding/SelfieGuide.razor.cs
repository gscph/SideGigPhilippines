using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// How to take the selfie, shown before the camera opens: a face in a dashed oval and three short tips (face in the
/// middle, good light, nothing covering the face). The same oval is on the live camera on both phones
/// (<c>Platforms/Android/SelfieCameraActivity</c>, <c>Platforms/iOS/IosSelfieCamera</c>), where it turns green once the
/// face is in it.
/// </summary>
public partial class SelfieGuide : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>Show the oval picture (left out when their photo already shows above the tips).</summary>
    [Parameter] public bool ShowPicture { get; set; } = true;
}
