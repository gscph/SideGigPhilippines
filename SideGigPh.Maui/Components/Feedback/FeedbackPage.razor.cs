using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
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
    [Inject] private ILogger<FeedbackPage> Logger { get; set; } = default!;

    [Parameter] public string Side { get; set; } = string.Empty;

    private LocalizedText? _notice;
    private bool _noticeIsError;
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        Board.Changed += OnChanged;
        await Board.LoadAsync();
    }

    public void Dispose() => Board.Changed -= OnChanged;

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);

    private void GoBack() => Nav.NavigateTo(FeedbackRoutes.Home(Side));

    private Task VoteAsync(FeedbackPost post) =>
        OnceAsync(async () => Say(await Board.ToggleVoteAsync(post) ? null : Copy.FeedbackFailed, isError: true));

    private Task RemoveAsync(FeedbackPost post) =>
        OnceAsync(async () => Say(await Board.RemoveAsync(post) ? null : Copy.FeedbackFailed, isError: true));

    private Task ReportAsync(FeedbackPost post) =>
        OnceAsync(async () =>
        {
            var reported = await Board.ReportAsync(post);
            Say(reported ? Copy.FeedbackReported : Copy.FeedbackFailed, isError: !reported);
        });

    /// <summary>
    /// One tap, one call: a quick double tap on a vote would otherwise send a vote and take it straight back
    /// (the row flips at once). A failure the board doesn't turn into false still ends as a message, never a broken screen.
    /// </summary>
    private async Task OnceAsync(Func<Task> send)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "A feedback board action failed.");
            Say(Copy.FeedbackFailed, isError: true);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Say(LocalizedText? notice, bool isError)
    {
        _notice = notice;
        _noticeIsError = isError;
    }
}
