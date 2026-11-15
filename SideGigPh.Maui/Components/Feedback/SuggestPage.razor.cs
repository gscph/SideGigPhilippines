using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Feedback;

/// <summary>
/// Suggest a feature, in up to three screens: write it (a title, optional details); when the board already has
/// suggestions that look the same, "Is it one of these?" lets the person add their vote to one instead, or say theirs
/// is different and post it; then a thank-you that leads back to the board. With no look-alike it posts right away.
/// </summary>
public partial class SuggestPage : ComponentBase
{
    [Inject] private FeedbackBoard Board { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<SuggestPage> Logger { get; set; } = default!;

    [Parameter] public string Side { get; set; } = string.Empty;

    private enum Step
    {
        Write,
        Similar,
        Posted,
        Voted,
    }

    private Step _step = Step.Write;
    private string? _title;
    private string? _details;
    private IReadOnlyList<FeedbackPost> _similar = [];
    private bool _busy;
    private bool _failed;

    private bool CanSend => FeedbackBoard.CanPost(_title);

    private void GoBack() => Nav.NavigateTo(FeedbackRoutes.Board(Side));

    private void Finish() => Nav.NavigateTo(FeedbackRoutes.Board(Side), replace: true);

    /// <summary>Next on the first screen: look for suggestions like this one; none means it is new, so post it.</summary>
    private async Task CheckAsync()
    {
        if (!CanSend)
        {
            return;
        }

        await RunAsync(async () =>
        {
            _similar = await Board.FindSimilarAsync(_title!);
            if (_similar.Count > 0)
            {
                _step = Step.Similar;
                return;
            }

            await Board.PostAsync(_title!, _details);
            _step = Step.Posted;
        });
    }

    /// <summary>"No, mine is different": post it.</summary>
    private Task PostAsync() => RunAsync(async () =>
    {
        await Board.PostAsync(_title!, _details);
        _step = Step.Posted;
    });

    /// <summary>"Yes, same idea": their vote goes to the one already there (nothing is posted).</summary>
    private Task SameAsync(FeedbackPost post) => RunAsync(async () =>
    {
        if (!await Board.VoteAsync(post, on: true))
        {
            throw new HttpRequestException("The vote didn't go through.");
        }

        _step = Step.Voted;
    });

    private async Task RunAsync(Func<Task> work)
    {
        if (_busy)
        {
            return; // a second tap before the button re-rendered as disabled
        }

        _busy = true;
        _failed = false;
        try
        {
            await Busy.RunAsync(work);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Sending a suggestion failed.");
            _failed = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
