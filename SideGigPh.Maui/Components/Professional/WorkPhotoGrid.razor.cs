using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The worker's work photos as a 2 × 3 grid of square slots: their photos first (each with its remove button), then an
/// "Add" slot for every place still free, up to <see cref="Slots"/>. Any "Add" slot opens the gallery. Only on the
/// worker's own Photos page; Seekers see the photos as the profile's cover (<c>ProfileCover</c>).
/// </summary>
public partial class WorkPhotoGrid : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The photos to show, newest first.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<WorkPhoto> Photos { get; set; } = [];

    /// <summary>How many slots the grid has (two across).</summary>
    [Parameter] public int Slots { get; set; } = 6;

    /// <summary>What the photos are, for screen readers.</summary>
    [Parameter] public string? Label { get; set; }

    [Parameter] public bool CanRemove { get; set; } = true;

    /// <summary>The "Add" slots are off while a pick or upload is running.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>An empty slot was tapped.</summary>
    [Parameter] public EventCallback OnAdd { get; set; }

    /// <summary>A photo's remove button was tapped, with its id.</summary>
    [Parameter] public EventCallback<string> OnRemove { get; set; }

    private int EmptySlots => Math.Max(0, Slots - Photos.Count);
}
