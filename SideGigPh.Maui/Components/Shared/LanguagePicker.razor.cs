using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>English / Taglish as two side-by-side buttons, the chosen one filled.</summary>
public partial class LanguagePicker : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public AppLanguage Value { get; set; }

    [Parameter] public EventCallback<AppLanguage> ValueChanged { get; set; }

    private IReadOnlyList<(AppLanguage Language, string Label)> Languages =>
    [
        (AppLanguage.English, T.Get(Copy.LanguageEnglish)),
        (AppLanguage.Taglish, T.Get(Copy.LanguageTaglish)),
    ];
}
