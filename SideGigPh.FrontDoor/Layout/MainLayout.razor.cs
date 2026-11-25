using Microsoft.AspNetCore.Components;
using SideGigPh.FrontDoor.EarlyAccess;
using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Layout;

/// <summary>
/// The page frame: theme, header, body, footer. On the first render it picks the language (<c>?lang=tl</c> from a Taglish
/// post's link, else the visitor's last pick, else the browser's language) and recalls a sign-up from this browser.
/// </summary>
public partial class MainLayout
{
    [Inject] private PageLanguage Language { get; set; } = default!;
    [Inject] private SignupState Signup { get; set; } = default!;
    [Inject] private PageScript Script { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var language = PageLanguage.Clean(AskedLanguage(Navigation.Uri))
            ?? PageLanguage.Clean(await Script.ReadAsync(PageScript.LanguageKey))
            ?? await Script.BrowserLanguageAsync();
        Language.Set(language); // Set cleans it too: anything else is English
        await Script.SetHtmlLangAsync(Language.Current);

        Signup.SetJoined(await Script.ReadAsync(PageScript.JoinedKey));

        // The hero's worker link tapped while the page was still prerendered: apply its side now.
        if (await Script.EarlyRoleAsync() is { } role)
        {
            Signup.SuggestRole(role);
        }
    }

    /// <summary>The <c>lang</c> in the page's address (<c>tarbaho.com/?lang=tl</c>), or null.</summary>
    private static string? AskedLanguage(string uri) =>
        new Uri(uri).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(pair => pair.Length == 2 && pair[0] == "lang")
            .Select(pair => Uri.UnescapeDataString(pair[1]))
            .FirstOrDefault();
}
