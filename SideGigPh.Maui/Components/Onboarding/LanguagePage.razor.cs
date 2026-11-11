using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Shared;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Onboarding;

/// <summary>
/// First question of the app: English or Taglish. Many tradesmen aren't confident English
/// readers, so this comes before anything else. Tile ids are the <see cref="AppLanguage"/> names.
/// </summary>
public partial class LanguagePage : ComponentBase
{
    private static readonly IReadOnlyList<ChoiceItem> Options =
    [
        new(nameof(AppLanguage.English), "English", string.Empty, "I'll read in English"),
        new(nameof(AppLanguage.Taglish), "Taglish", string.Empty, "Tagalog na may halong English"),
    ];

    [Inject] private IOnboardingStore Store { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private async Task PickAsync(string id)
    {
        var language = Enum.Parse<AppLanguage>(id);
        T.Use(language);

        var state = await Store.LoadAsync();
        state.Language = language;
        await Store.SaveAsync(state);

        // The launch router sends them to sign in (D1: everyone signs in first), then on.
        Nav.NavigateTo("/");
    }
}
