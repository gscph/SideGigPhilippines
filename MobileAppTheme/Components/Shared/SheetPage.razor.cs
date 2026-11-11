using Microsoft.AspNetCore.Components;

namespace MobileAppTheme.Components.Shared;

/// <summary>
/// A full-screen sheet page (Airbnb's "Log in or sign up" / "Finish signing up" look): a
/// <see cref="SheetHeader"/> with the centered title, then the content on white with roomy gutters.
/// The main button goes inline at the end of the content, not pinned.
/// </summary>
public partial class SheetPage : ComponentBase
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public EventCallback OnBack { get; set; }

    [Parameter] public EventCallback OnClose { get; set; }
}
