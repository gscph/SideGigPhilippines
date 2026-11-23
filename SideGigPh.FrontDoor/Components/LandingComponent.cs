using Microsoft.AspNetCore.Components;
using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Components;

/// <summary>Base for the page's parts: <see cref="T"/> picks the English or Taglish line, and a language switch re-renders.</summary>
public abstract class LandingComponent : ComponentBase, IDisposable
{
    [Inject] protected PageLanguage Language { get; set; } = default!;

    protected string T(Say say) => Language.Get(say);

    protected override void OnInitialized() => Language.Changed += OnLanguageChanged;

    private void OnLanguageChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        Language.Changed -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }
}
