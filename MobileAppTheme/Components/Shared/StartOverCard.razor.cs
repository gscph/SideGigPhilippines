using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// "Start over" with a second tap to confirm, since it deletes everything on the phone.
/// Reports the confirmed tap; the parent does the reset.
/// </summary>
public partial class StartOverCard : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>True while the reset runs; the buttons are disabled.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>What gets deleted, shown when asking to confirm; defaults to a generic "everything on this phone".</summary>
    [Parameter] public string? Warning { get; set; }

    [Parameter] public EventCallback OnConfirm { get; set; }

    private bool _asking;
}
