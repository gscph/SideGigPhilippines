using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Feedback;

/// <summary>
/// The feedback board (both sides, opened from the Me page): every suggestion, most voted first, each with a vote
/// button; the button at the bottom opens <see cref="SuggestPage"/>. A person can take back their own suggestion
/// while nobody else voted for it, and report someone else's. Full screen, outside the tab shell.
/// </summary>
public partial class FeedbackPage : ComponentBase, IDisposable
{
    [Inject] private FeedbackBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public string Side { get; set; } = string.Empty;

    private LocalizedText? _notice;
    private bool _noticeIsError;

    protected override async Task OnInitializedAsync()
    {
        Board.Changed += OnChanged;
        await Board.LoadAsync();
    }

    public void Dispose() => Board.Changed -= OnChanged;

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);

    private void GoBack() => Nav.NavigateTo(FeedbackRoutes.Home(Side));

    private async Task VoteAsync(FeedbackPost post) =>
        Say(await Board.ToggleVoteAsync(post) ? null : Copy.FeedbackFailed, isError: true);

    private async Task RemoveAsync(FeedbackPost post) =>
        Say(await Board.RemoveAsync(post) ? null : Copy.FeedbackFailed, isError: true);

    private async Task ReportAsync(FeedbackPost post)
    {
        var reported = await Board.ReportAsync(post);
        Say(reported ? Copy.FeedbackReported : Copy.FeedbackFailed, isError: !reported);
    }

    private void Say(LocalizedText? notice, bool isError)
    {
        _notice = notice;
        _noticeIsError = isError;
    }
}
