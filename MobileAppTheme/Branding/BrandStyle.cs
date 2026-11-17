namespace MobileAppTheme.Branding;

/// <summary>
/// A product's colors and corner radius, the only values an app sets to restyle the skeleton.
/// <see cref="MobileTheme.Create"/> turns them into a <c>MudTheme</c>. The defaults are a clean
/// blue on white with near-black ink and soft gray lines (Airbnb-style neutrals), so <c>new BrandStyle()</c> already looks finished.
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
    public string Ink { get; init; } = "#222222";

    /// <summary>Helper text, also <c>Color.Secondary</c>.</summary>
    public string Muted { get; init; } = "#6a6a6a";

    public string Success { get; init; } = "#107c10";

    public string Warning { get; init; } = "#c19c00";

    public string Error { get; init; } = "#d13438";

    /// <summary><c>Color.Info</c>: an app's extra accent for one kind of nudge. Null means <see cref="Primary"/>.</summary>
    public string? Info { get; init; }

    /// <summary><c>Color.Tertiary</c>: an app's extra accent for one kind of status. Null means <see cref="Primary"/>.</summary>
    public string? Tertiary { get; init; }

    /// <summary>Page and card background.</summary>
    public string Background { get; init; } = "#ffffff";

    /// <summary>Tinted background behind grouped content.</summary>
    public string BackgroundGray { get; init; } = "#f7f7f7";

    /// <summary>Borders, dividers and skeleton loaders.</summary>
    public string Lines { get; init; } = "#ebebeb";

    /// <summary>Text field outlines.</summary>
    public string LinesInputs { get; init; } = "#b0b0b0";

    public string CornerRadius { get; init; } = "8px";

    /// <summary>The font stack, first choice first. The default is the phone's own font; an app that ships a web
    /// font in its wwwroot (with an <c>@font-face</c> in its app.css) puts that font's name first.</summary>
    public string[] FontFamily { get; init; } =
        ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"];
}
