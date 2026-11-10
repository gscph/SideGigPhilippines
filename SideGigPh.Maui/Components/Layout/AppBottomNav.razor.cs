using Microsoft.AspNetCore.Components;

namespace SideGigPh.Maui.Components.Layout;

/// <summary>Icon tabs fixed to the bottom of <see cref="AppShell"/>; the tab for the current page is lit.</summary>
public partial class AppBottomNav : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<AppTab> Tabs { get; set; } = [];

    /// <summary>The current route, e.g. <c>/inbox/5</c>.</summary>
    [Parameter, EditorRequired] public string CurrentPath { get; set; } = string.Empty;

    // The most specific match wins, so "/professional" doesn't light up on "/professional/reviews".
    private AppTab? ActiveTab => Tabs
        .Where(tab => CurrentPath == tab.Href || (tab.MatchPrefix && CurrentPath.StartsWith(tab.Href + "/", StringComparison.Ordinal)))
        .MaxBy(tab => tab.Href.Length);
}
