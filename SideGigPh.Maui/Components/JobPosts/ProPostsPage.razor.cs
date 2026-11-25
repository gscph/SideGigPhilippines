using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The pro's Requests tab, "New" side of the switch (<see cref="RequestsHeader"/>): what customers posted for their
/// trade within the distance they travel, a service they offer first, then nearest, then newest, a page at a time, one
/// short <see cref="RequestRow"/> each; a tap opens the request's own page, where the worker offers. Until the listing
/// is published it says to publish first (the server matches posts to the published listing).
/// </summary>
public partial class ProPostsPage : ComponentBase, IDisposable
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        Board.Changed += OnChanged;
        await Board.LoadFeedAsync();
        _loaded = true;
    }

    public void Dispose() => Board.Changed -= OnChanged;

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);
}
