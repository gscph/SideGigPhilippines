using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// "Start over" with a second tap to confirm, since it deletes everything on the phone.
/// Reports the confirmed tap; the parent does the reset.
/// </summary>
public partial class StartOverCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>True while the reset runs; the buttons are disabled.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnConfirm { get; set; }

    private bool _asking;
}
