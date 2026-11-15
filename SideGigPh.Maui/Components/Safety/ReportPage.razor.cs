using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Safety;

/// <summary>
/// Report or block a person (App Store 1.2, Google Play user-generated content), from a profile, a review or a chat
/// on either side: a reason, an optional note, "also block them" (on by default), or block without reporting. Then a
/// thank-you; Done goes back, or to the Inbox / search when the person is now hidden.
/// </summary>
public partial class ReportPage : ComponentBase
{
    [Inject] private SeekerWorkspace Seeker { get; set; } = default!;
    [Inject] private ProWorkspace Pro { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private BusyIndicator Busy { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<ReportPage> Logger { get; set; } = default!;

    [Parameter] public string Side { get; set; } = string.Empty;

    [Parameter] public string SubjectName { get; set; } = string.Empty;

    [Parameter] public string PersonId { get; set; } = string.Empty;

    [SupplyParameterFromQuery(Name = "back")] public string? Back { get; set; }

    [SupplyParameterFromQuery(Name = "chat")] public string? ChatId { get; set; }

    private enum Outcome
    {
        Reported,
        Blocked,
    }

    private static readonly (ReportReason Reason, LocalizedText Label, string Icon)[] Choices =
    [
        (ReportReason.Scam, Copy.ReportScam, AppImages.Scam),
        (ReportReason.Harassment, Copy.ReportHarassment, AppImages.Harassment),
        (ReportReason.Fake, Copy.ReportFake, AppImages.Fake),
        (ReportReason.Inappropriate, Copy.ReportInappropriate, AppImages.Blocked),
        (ReportReason.Employment, Copy.ReportEmployment, AppImages.Employment),
        (ReportReason.Other, Copy.ReportOther, AppImages.Quote),
    ];

    private string? _reason;
    private string? _note;
    private bool _alsoBlock = true;
    private bool _busy;
    private string? _error;
    private Outcome? _done;

    private bool IsPro => Side == ReportRoutes.ProSide;

    private IReadOnlyList<ChoiceItem> Reasons =>
        [.. Choices.Select(c => new ChoiceItem(c.Reason.ToString(), T.Get(c.Label), c.Icon))];

    private ReportSubject Subject => Enum.TryParse<ReportSubject>(SubjectName, ignoreCase: true, out var subject) ? subject : ReportSubject.User;

    /// <summary>Where Back goes, and Done when the person is still visible.</summary>
    private string BackPath => ReportRoutes.SafeBack(Back) ?? Home;

    /// <summary>Where Done goes once the person is hidden: their profile and chat are gone.</summary>
    private string Home => IsPro ? ProRoutes.Inbox : Subject == ReportSubject.Chat ? SeekerRoutes.Inbox : SeekerRoutes.Search;

    private void GoBack() => Nav.NavigateTo(BackPath);

    private async Task SendAsync()
    {
        if (_reason is null || !Enum.TryParse<ReportReason>(_reason, out var reason))
        {
            return;
        }

        var report = new UserReport(PersonId, Subject, reason, ChatId, _note?.Trim(), _alsoBlock);
        await RunAsync(() => IsPro ? Pro.ReportAsync(report) : Seeker.ReportAsync(report), Outcome.Reported);
    }

    private Task BlockAsync() =>
        RunAsync(() => IsPro ? Pro.BlockAsync(PersonId) : Seeker.BlockAsync(PersonId), Outcome.Blocked);

    private async Task RunAsync(Func<Task> send, Outcome outcome)
    {
        _busy = true;
        _error = null;
        try
        {
            await Busy.RunAsync(send);
            _done = outcome;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Sending a {Outcome} failed.", outcome);
            _error = T.Get(Copy.ReportFailed);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Finish() =>
        Nav.NavigateTo(_done == Outcome.Blocked || _alsoBlock ? Home : BackPath, replace: true);
}
