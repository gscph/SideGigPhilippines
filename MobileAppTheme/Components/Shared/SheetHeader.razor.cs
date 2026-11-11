using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// The top row of a full-screen sheet (sign-in, a step, a form): a close ✕ or back ← on the left,
/// a small bold title in the middle, an optional action on the right, and a hairline under it.
/// Without a title it is just the button row (interview questions, whose title is the big text below).
/// </summary>
public partial class SheetHeader : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter] public string? Title { get; set; }

    /// <summary>The title is the page's <c>h1</c> (sheets whose header is their only title).</summary>
    [Parameter] public bool TitleIsHeading { get; set; } = true;

    /// <summary>Custom middle content instead of <see cref="Title"/> (e.g. a progress bar).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Right-hand action, at most one icon button wide.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Shows ← when set (and <see cref="OnClose"/> isn't).</summary>
    [Parameter] public EventCallback OnBack { get; set; }

    /// <summary>Shows ✕ when set; wins over <see cref="OnBack"/>.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    [Parameter] public bool Divider { get; set; } = true;
}
