using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;

namespace SideGigPh.Maui.Services;

/// <summary>
/// New-message notifications on this phone, for both sides. The server decides how loud they are: a customer
/// writing to a Skilled Worker is a normal notification; a worker answering a Seeker only puts a red dot on the
/// app icon. Here the app registers the phone, asks to allow notifications when there's a reason to, refreshes the
/// Inbox when a message arrives while it's open, opens the chat when a notification is tapped, and keeps the icon
/// badge and tray in step with what's still unread. The side's layout attaches to it (<see cref="Attach"/>).
/// </summary>
public sealed class ChatAlerts
{
    private readonly IPushNotifications _push;
    private readonly IUserRepository _users;
    private readonly ILogger<ChatAlerts> _logger;
    private Func<Task>? _refresh;
    private Action<string>? _open;
    private string? _pendingPath;

    public ChatAlerts(IPushNotifications push, IUserRepository users, ILogger<ChatAlerts> logger)
    {
        _push = push;
        _users = users;
        _logger = logger;
        push.Received += _ => OnReceived();
        push.Tapped += OnTapped;
        push.TokenChanged += token => _ = SaveTokenAsync(token);
    }

    /// <summary>
    /// The open side's layout listens: <paramref name="refresh"/> re-reads its Inbox when a message arrives, and
    /// <paramref name="open"/> goes to a tapped notification's chat (straight away for the tap that opened the app).
    /// Dispose when the layout goes.
    /// </summary>
    public IDisposable Attach(Func<Task> refresh, Action<string> open)
    {
        _refresh = refresh;
        _open = open;
        if (_pendingPath is { } path)
        {
            _pendingPath = null;
            open(path);
        }

        return new Detach(this, refresh);
    }

    /// <summary>Sends this phone's notification address to the server, if notifications are allowed. Never asks.</summary>
    public async Task RegisterAsync()
    {
        if (_push.Platform is not null && await _push.GetTokenAsync() is { } token)
        {
            await SaveTokenAsync(token);
        }
    }

    /// <summary>
    /// Asks to allow notifications (the system dialog) when the person has a reason to want them: a worker whose
    /// listing is live, a Seeker who just wrote to a worker. Already allowed: only registers the phone.
    /// </summary>
    public async Task AskAsync()
    {
        if (_push.Platform is null)
        {
            return;
        }

        if (await _push.IsAllowedAsync() || await _push.RequestPermissionAsync())
        {
            await RegisterAsync();
        }
    }

    /// <summary>The chats still unread on the open side: the icon badge counts them and read ones leave the tray.</summary>
    public void ShowUnread(IReadOnlyCollection<string> unreadChatIds) => _push.ShowOnly(unreadChatIds);

    /// <summary>The page a notification opens: the chat on the receiver's side. Null for data it doesn't understand.</summary>
    public static string? PathFor(IReadOnlyDictionary<string, string> data) =>
        data.GetValueOrDefault(PushData.Side) switch
        {
            Wire.Professional when data.GetValueOrDefault(PushData.ChatId) is { Length: > 0 } chatId => ProRoutes.Chat(chatId),
            Wire.Seeker when data.GetValueOrDefault(PushData.ProfessionalId) is { Length: > 0 } proId => SeekerRoutes.Thread(proId),
            _ => null,
        };

    private void OnReceived()
    {
        if (_refresh is { } refresh)
        {
            _ = RefreshAsync(refresh);
        }
    }

    private void OnTapped(IReadOnlyDictionary<string, string> data)
    {
        if (PathFor(data) is not { } path)
        {
            return;
        }

        if (_open is { } open)
        {
            open(path);
        }
        else
        {
            _pendingPath = path; // the app is still starting: the layout opens it when it attaches
        }
    }

    private async Task RefreshAsync(Func<Task> refresh)
    {
        try
        {
            await refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't refresh the Inbox after a notification.");
        }
    }

    private async Task SaveTokenAsync(string token)
    {
        try
        {
            await _users.SaveDeviceAsync(token, _push.Platform!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't register this phone for notifications.");
        }
    }

    private sealed class Detach(ChatAlerts alerts, Func<Task> refresh) : IDisposable
    {
        public void Dispose()
        {
            if (alerts._refresh == refresh)
            {
                alerts._refresh = null;
                alerts._open = null;
            }
        }
    }
}
