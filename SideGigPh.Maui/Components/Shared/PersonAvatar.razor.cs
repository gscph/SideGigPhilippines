using Microsoft.AspNetCore.Components;
using MudBlazor;
using Size = MudBlazor.Size;

namespace SideGigPh.Maui.Components.Shared;

/// <summary>
/// A person's round avatar: their photo, else the initials of their name, else a person icon.
/// Used for pros (cards, profile, top bar), reviewers and the Seeker's own avatar.
/// </summary>
public partial class PersonAvatar : ComponentBase
{
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;

    [Parameter] public string? PhotoUrl { get; set; }

    [Parameter] public Size Size { get; set; } = Size.Medium;

    [Parameter] public string? Class { get; set; }

    private string Initials =>
        string.Concat(Name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0])));
}
