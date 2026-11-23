using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>EN | Taglish: switches the page in place (nothing typed is lost) and remembers the pick for the next visit.</summary>
public partial class LanguageToggle
{
    [Inject] private PageScript Script { get; set; } = default!;

    /// <summary>White outlines, for the purple header.</summary>
    [Parameter] public bool OnDark { get; set; }

    private Color ToggleColor => OnDark ? Color.Inherit : Color.Dark;

    private string ToggleClass => OnDark ? "fd-language fd-language-dark" : "fd-language";

    private async Task PickAsync(string language)
    {
        Language.Set(language);
        await Script.WriteAsync(PageScript.LanguageKey, Language.Current);
        await Script.SetHtmlLangAsync(Language.Current);
    }
}
