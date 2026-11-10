using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>
/// Opened from the avatar in the top bar: the Seeker's name (shown on their reviews) and the
/// app language. There's no sign-in yet, so it says plainly that everything stays on this phone.
/// </summary>
public partial class AccountPage : ComponentBase
{
    [Inject] private SeekerWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private string? _name;
    private bool _saved;

    private IReadOnlyList<(AppLanguage Language, string Label)> Languages =>
    [
        (AppLanguage.English, T.Get(Copy.LanguageEnglish)),
        (AppLanguage.Taglish, T.Get(Copy.LanguageTaglish)),
    ];

    private bool NameChanged => (_name?.Trim() ?? string.Empty) != (Workspace.Answers.ReviewerName ?? string.Empty);

    protected override void OnInitialized() => _name = Workspace.Answers.ReviewerName;

    private async Task SaveNameAsync()
    {
        await Workspace.SetNameAsync(_name);
        _saved = true;
    }
}
