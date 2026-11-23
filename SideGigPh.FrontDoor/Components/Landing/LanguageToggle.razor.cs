using Microsoft.AspNetCore.Components;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>EN | Taglish: switches the page in place (nothing typed is lost) and remembers the pick for the next visit.</summary>
public partial class LanguageToggle
{
    [Inject] private PageScript Script { get; set; } = default!;

    private async Task PickAsync(string language)
    {
        Language.Set(language);
        await Script.WriteAsync(PageScript.LanguageKey, Language.Current);
        await Script.SetHtmlLangAsync(Language.Current);
    }
}
