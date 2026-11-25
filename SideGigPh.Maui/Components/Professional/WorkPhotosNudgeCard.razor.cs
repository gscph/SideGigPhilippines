using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The amber reminder on Pro home to add work photos once the listing is live: optional (publishing never needs them),
/// but customers pick workers with photos first and they rank the worker higher in search
/// (<c>SearchRanker.PhotosFeature</c>). Shown while there are fewer than <c>ProfessionalDraft.GoodWorkPhotos</c>
/// (<see cref="ProfileProgress.ShowWorkPhotosNudge"/>); opens the photos page.
/// </summary>
public partial class WorkPhotosNudgeCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
}
