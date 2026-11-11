namespace MobileAppTheme.Branding;

/// <summary>
/// A product's colors and corner radius, the only values an app sets to restyle the skeleton.
/// <see cref="MobileTheme.Create"/> turns them into a <c>MudTheme</c>. The defaults are a clean
/// blue / white palette (Microsoft style), so <c>new BrandStyle()</c> already looks finished.
/// </summary>
public sealed record BrandStyle
{
    /// <summary>The brand color: top bar, primary buttons, active tab, links.</summary>
    public string Primary { get; init; } = "#0078d4";

    public string PrimaryDarken { get; init; } = "#005a9e";

    public string PrimaryLighten { get; init; } = "#5ea8e6";

    /// <summary>Text on top of <see cref="Primary"/>.</summary>
    public string OnPrimary { get; init; } = "#ffffff";

    /// <summary>Main text color, also <c>Color.Dark</c>.</summary>
    public string Ink { get; init; } = "#0b1f33";

    /// <summary>Helper text, also <c>Color.Secondary</c>.</summary>
    public string Muted { get; init; } = "#5c6b7a";

    public string Success { get; init; } = "#107c10";

    public string Warning { get; init; } = "#c19c00";

    public string Error { get; init; } = "#d13438";

    /// <summary>Page and card background.</summary>
    public string Background { get; init; } = "#ffffff";

    /// <summary>Tinted background behind grouped content.</summary>
    public string BackgroundGray { get; init; } = "#f5f8fc";

    /// <summary>Borders, dividers and skeleton loaders.</summary>
    public string Lines { get; init; } = "#e1e8f0";

    /// <summary>Text field outlines.</summary>
    public string LinesInputs { get; init; } = "#c7d3e0";

    public string CornerRadius { get; init; } = "10px";
}
