using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>Tells the pro when picking photos failed, or when duplicates were skipped. Silent otherwise.</summary>
public partial class PhotoAddNotice : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public PhotoAddResult? Result { get; set; }
}
