namespace SideGigPh.FrontDoor.Localization;

/// <summary>
/// The language the page shows: English or Taglish. It comes from <c>?lang=tl</c> (a Taglish post's link), else the
/// visitor's last pick, else the browser's language; the header toggle changes it in place (typed text is kept).
/// One per visitor (scoped); components read <see cref="Get"/> and re-render on <see cref="Changed"/>.
/// </summary>
public sealed class PageLanguage
{
    public const string English = "en";
    public const string Taglish = "tl";

    public string Current { get; private set; } = English;

    public bool IsTaglish => Current == Taglish;

    public event Action? Changed;

    public string Get(Say say) => IsTaglish ? say.Tl : say.En;

    public string Format(Say say, params object[] args) => string.Format(Get(say), args);

    public void Set(string? language)
    {
        var next = Clean(language) ?? English;
        if (next == Current) return;
        Current = next;
        Changed?.Invoke();
    }

    /// <summary>"tl", "fil" or "fil-PH" → Taglish; "en…" → English; anything else → null (no opinion).</summary>
    public static string? Clean(string? language)
    {
        var l = (language ?? "").Trim().ToLowerInvariant();
        if (l is "tl" or "fil" || l.StartsWith("tl-") || l.StartsWith("fil-")) return Taglish;
        if (l == "en" || l.StartsWith("en-")) return English;
        return null;
    }
}
