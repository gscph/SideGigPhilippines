using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// The shell of one interview question, Airbnb style: a back arrow with a thin ink progress bar
/// beside it, the question as a big bold title, a short muted "why we ask" line, the answer UI
/// (child content) and an optional sticky ink Next button. Everything is centered: an optional
/// picture, the title and the why line, and a short step sits in the middle of the screen.
/// </summary>
public partial class InterviewStep : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>A picture for the question (an emoji), shown big in a soft circle above the title.</summary>
    [Parameter] public string? Illustration { get; set; }

    /// <summary>Why we ask, in one short line. Builds trust and sets up the payoff.</summary>
    [Parameter] public string? Why { get; set; }

    /// <summary>Small print under the answers.</summary>
    [Parameter] public string? Note { get; set; }

    /// <summary>0–1. Hidden at 0; callers start above 0 so the bar never looks empty.</summary>
    [Parameter] public double Progress { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Shown only when set.</summary>
    [Parameter] public EventCallback OnBack { get; set; }

    /// <summary>Shown only when set. Single-tap questions advance on their own and don't need it.</summary>
    [Parameter] public EventCallback OnNext { get; set; }

    [Parameter] public bool CanContinue { get; set; } = true;

    /// <summary>Defaults to <c>CommonCopy.Next</c>.</summary>
    [Parameter] public string? NextLabel { get; set; }

    private int Percent => (int)Math.Round(Math.Clamp(Progress, 0, 1) * 100);
}
