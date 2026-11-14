using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's work gallery (<see cref="WorkPhotosEditor"/>). The newest three show on their listing card.
/// </summary>
public partial class PhotosPage : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
}
