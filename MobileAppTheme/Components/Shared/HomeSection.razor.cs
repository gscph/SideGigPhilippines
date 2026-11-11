using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A collapsible home-page section, Slack style: the title links to the full page ("Recent
/// inquiries ›") and the chevron on the right shows or hides the content.
/// </summary>
public partial class HomeSection : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>The full page for this section; the title links there when set.</summary>
    [Parameter] public string? Href { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _expanded = true;

    private void Toggle() => _expanded = !_expanded;
}
