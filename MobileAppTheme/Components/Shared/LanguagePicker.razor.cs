using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Shared;

/// <summary>English / Taglish as two side-by-side buttons, the chosen one filled.</summary>
public partial class LanguagePicker : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public AppLanguage Value { get; set; }

    [Parameter] public EventCallback<AppLanguage> ValueChanged { get; set; }

    private IReadOnlyList<(AppLanguage Language, string Label)> Languages =>
    [
        (AppLanguage.English, T.Get(CommonCopy.LanguageEnglish)),
        (AppLanguage.Taglish, T.Get(CommonCopy.LanguageTaglish)),
    ];
}
