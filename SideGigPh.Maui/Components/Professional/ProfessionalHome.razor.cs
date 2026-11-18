using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// Pro home, built around the pro's daily work. Until the listing is published: the setup checklist, then recent
/// messages and reviews. Once live: two big tiles up top, Job Requests (new requests they haven't offered on) and Inbox
/// (unread chats), then the newest requests and recent messages, then the profile reminders (work photos, then badges)
/// and latest reviews. Redraws when the workspace or the board changes (a push re-read the Inbox, an offer was sent),
/// so the counts and "Recent messages" stay current.
/// </summary>
public partial class ProfessionalHome : ComponentBase, IDisposable
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private ProBadges Badges { get; set; } = default!;
    [Inject] private JobBoard Board { get; set; } = default!;

    private bool _requestsLoaded;

    /// <summary>Where the confirm-number step leads: the add-phone page, coming back here.</summary>
    private static string AddPhonePath => AuthRoutes.AddPhone(ProRoutes.Home, canSkip: true);

    /// <summary>Once the listing is live, the amber reminders show while work photos or a badge can still be added.</summary>
    private ProfileProgress Progress => ProfileProgress.Of(Workspace, Badges);

    /// <summary>Requests in the first page the pro hasn't offered on yet.</summary>
    private int NewRequestCount => Board.Feed.Count(post => post.MyOfferPesos is null);

    private string RequestsDetail => NewRequestCount > 0
        ? T.Get(Copy.HomeRequestsWaiting)
        : T.Get(Copy.HomeRequestsNone);

    private string InboxDetail => Workspace.UnreadCount > 0
        ? T.Get(Copy.HomeInboxWaiting)
        : T.Get(Copy.HomeInboxNone);

    protected override async Task OnInitializedAsync()
    {
        Workspace.Changed += OnChanged;
        Board.Changed += OnChanged;
        await Workspace.RefreshIfStaleAsync();
        if (Workspace.IsPublished)
        {
            await Task.WhenAll(Badges.RefreshIfStaleAsync(), Board.LoadFeedAsync());
            _requestsLoaded = true;
        }
    }

    public void Dispose()
    {
        Workspace.Changed -= OnChanged;
        Board.Changed -= OnChanged;
    }

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);
}
