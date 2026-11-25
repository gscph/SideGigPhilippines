using Microsoft.AspNetCore.Components;
using MobileAppTheme.Auth;
using MudBlazor;
using SideGigPh.Maui.Components.Layout;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The Professional's pages inside the shared <see cref="AppShell"/>: tabs Home, Requests (what customers nearby posted for their trade), Inbox (with an
/// unread badge), Reviews and Me (profile, work photos, switch to customer, language); the avatar opens
/// Me too, with a green dot once published; until then (and once live, after the server refused the own photo) a red <see cref="NotLiveBanner"/> under the top bar links to the next step. Sends anyone signed out to sign in (<see cref="SignInGate"/>), loads <see cref="ProWorkspace"/> and sends anyone who hasn't finished the
/// Professional interview back to <c>/</c>. When the setup checklist gets finished it opens the Publish page once.
/// </summary>
public partial class ProLayout : LayoutComponentBase, IDisposable
{
    [Inject] private ProWorkspace Workspace { get; set; } = default!;
    [Inject] private LocalAccount Account { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private SignInGate Gate { get; set; } = default!;
    [Inject] private ChatAlerts Alerts { get; set; } = default!;
    [Inject] private JobBoard Board { get; set; } = default!;
    [Inject] private ProBadges Badges { get; set; } = default!;

    private bool _ready;
    private IDisposable? _alerts;

    /// <summary>Under the top bar until the listing is published: customers can't see the worker yet, and the next step is one tap away.</summary>
    private RenderFragment? NotLive => ProfileProgress.Of(Workspace, Badges).NextStep is { } step
        ? builder =>
        {
            builder.OpenComponent<NotLiveBanner>(0);
            builder.AddComponentParameter(1, nameof(NotLiveBanner.Step), step);
            builder.CloseComponent();
        }
        : null;

    private IReadOnlyList<AppTab> Tabs =>
    [
        new(ProRoutes.Home, Icons.Material.Filled.Home, T.Get(Copy.NavHome)),
        new(ProRoutes.Requests, Icons.Material.Filled.Campaign, T.Get(Copy.NavRequests)),
        new(ProRoutes.Inbox, Icons.Material.Filled.ChatBubbleOutline, T.Get(Copy.NavInbox), Workspace.UnreadCount, MatchPrefix: true),       
        new(ProRoutes.Me, Icons.Material.Filled.Person, T.Get(Copy.NavMe), MatchPrefix: true),
    ];

    protected override async Task OnInitializedAsync()
    {
        Workspace.Changed += OnWorkspaceChanged;
        Account.Changed += OnWorkspaceChanged;
        if (Workspace.IsLoaded)
        {
            // Back from a full-screen page (a chat, a request, Publish): the tabs show on the first render, with no blank
            // frame, and the phone was registered when they first loaded. A listing that went live since is the reason
            // to ask for notifications (asked once; the system remembers). The sign-in check still runs.
            Start();
            if (Workspace.IsPublished)
            {
                _ = Alerts.AskAsync();
            }

            if (await Gate.RedirectAsync("/") is { } back)
            {
                Nav.NavigateTo(back, replace: true);
            }

            return;
        }

        // Signed out (e.g. "Sign in with this number", then Back): sign in first, as at launch. Without this the pages
        // loaded with nobody signed in, and the add-number link then opened the sign-in page instead.
        if (await Gate.RedirectAsync("/") is { } signIn)
        {
            Nav.NavigateTo(signIn, replace: true);
            return;
        }

        if (!await Workspace.EnsureLoadedAsync())
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        Start();

        // A live listing is the reason to want notifications: customers write in. Asks once (the system remembers).
        _ = Workspace.IsPublished ? Alerts.AskAsync() : Alerts.RegisterAsync();
    }

    /// <summary>
    /// On every page change (the layout's body changes): picks up a number just linked on the add-phone page, then
    /// opens Publish once when that finished the checklist.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (_ready)
        {
            await Workspace.RefreshAccountAsync();
            PromptPublish();
        }
    }

    public void Dispose()
    {
        Workspace.Changed -= OnWorkspaceChanged;
        Account.Changed -= OnWorkspaceChanged;
        _alerts?.Dispose();
    }

    private void OnWorkspaceChanged()
    {
        if (_ready)
        {
            Alerts.ShowUnread(Workspace.UnreadChatIds);
        }

        InvokeAsync(() =>
        {
            StateHasChanged();
            PromptPublish();
        });
    }

    /// <summary>Shows the tabs and listens for notifications.</summary>
    private void Start()
    {
        _ready = true;
        // A push is a new message or a new request near them: the Inbox and the Requests feed are read again.
        _alerts = Alerts.Attach(() => Task.WhenAll(Workspace.RefreshActivityAsync(), Board.LoadFeedAsync()), path => InvokeAsync(() => Nav.NavigateTo(path)));
        Alerts.ShowUnread(Workspace.UnreadChatIds);
    }

    /// <summary>The last setup step (photo, work photo, number) was just done: straight to Publish, once.</summary>
    private void PromptPublish()
    {
        if (_ready && Workspace.TakePublishPrompt())
        {
            Nav.NavigateTo(ProRoutes.Publish);
        }
    }
}
