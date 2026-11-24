using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The gentle "take it again?" after a proof was sent and the server's photo check found it blurry, dark, with glare
/// or too small. Only advice: the photo is already sent, so keeping it is always one tap (a worker told "not
/// readable" again and again gives up on the badge), and a retake replaces it before the checker sees it.
/// </summary>
public partial class PhotoRetakePrompt : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>What the check found.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ProofProblem> Problems { get; set; } = [];

    /// <summary>True while a retake is being taken or sent.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnRetake { get; set; }

    [Parameter] public EventCallback OnKeep { get; set; }

    private static LocalizedText Say(ProofProblem problem) => problem switch
    {
        ProofProblem.Blurry => Copy.BadgeRetakeBlurry,
        ProofProblem.Glare => Copy.BadgeRetakeGlare,
        ProofProblem.Dark => Copy.BadgeRetakeDark,
        _ => Copy.BadgeRetakeSmall,
    };
}
