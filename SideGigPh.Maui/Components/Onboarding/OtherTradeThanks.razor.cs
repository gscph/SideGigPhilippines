using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// Shown after someone typed a kind of work we don't offer (the "Others" tile after the trades): thanks, it was noted
/// for analytics, and a button back to the trades. Both interviews use it, each with its own <see cref="Message"/>.
/// </summary>
public partial class OtherTradeThanks : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>What happens now, for this side ("We don't have skilled workers for this yet…").</summary>
    [Parameter, EditorRequired] public string Message { get; set; } = string.Empty;

    [Parameter] public double Progress { get; set; }

    /// <summary>Back to the trade tiles (both Back and the main button).</summary>
    [Parameter] public EventCallback OnPickAnother { get; set; }
}
