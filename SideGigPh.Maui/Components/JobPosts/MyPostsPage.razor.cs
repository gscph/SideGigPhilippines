using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.JobPosts;

/// <summary>
/// The Seeker's Post tab: the button to post what they need done, then their own posts, newest first, each saying
/// whether workers can still see it, with "Edit" and "Take down" on the open ones.
/// </summary>
public partial class MyPostsPage : ComponentBase, IDisposable
{
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private bool _loaded;
    private bool _busy;
    private bool _failed;

    protected override async Task OnInitializedAsync()
    {
        Board.Changed += OnChanged;
        await Board.LoadMineAsync();
        _loaded = true;
    }

    public void Dispose() => Board.Changed -= OnChanged;

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task TakeDownAsync(JobPost post)
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered
        }

        _busy = true;
        try
        {
            _failed = !await Board.CloseAsync(post);
        }
        finally
        {
            _busy = false;
        }
    }
}
