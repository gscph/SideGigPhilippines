namespace SideGigPh.Maui.Components.Layout;

/// <summary>
/// One bottom tab in <see cref="AppShell"/>. <see cref="Badge"/> shows as a red count (hidden at 0).
/// With <see cref="MatchPrefix"/>, sub-pages (e.g. <c>/inbox/5</c> under <c>/inbox</c>) keep the tab lit.
/// </summary>
public sealed record AppTab(string Href, string Icon, string Label, int Badge = 0, bool MatchPrefix = false);
