using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Api;

/// <summary>
/// One notification to all of a person's phones. <see cref="Alert"/> decides how loud it is: true shows a normal
/// notification (sound, pop-up); false only updates the app icon's badge (iOS) or adds a silent tray line on a quiet
/// channel, which is what puts the dot on the icon (Android).
/// </summary>
/// <param name="Tag">Replaces the earlier notification with the same tag (one per chat), and lets the app clear it once read.</param>
/// <param name="Badge">The number on the app icon (iOS) and the Android notification count.</param>
/// <param name="AndroidChannel">The Android notification channel the app created for this kind of message.</param>
public sealed record PushMessage(
    IReadOnlyList<string> Tokens,
    string Title,
    string Body,
    bool Alert,
    string Tag,
    int Badge,
    string AndroidChannel,
    IReadOnlyDictionary<string, string> Data);

/// <summary>Sends notifications to phones. Behind a port so the push service can change and tests can watch what's sent.</summary>
public interface IPushSender
{
    /// <summary>Sends <paramref name="message"/>. Returns the tokens that no longer reach a phone, to forget.</summary>
    Task<IReadOnlyList<string>> SendAsync(PushMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="IPushSender"/> on Firebase Cloud Messaging (it delivers to Android and, through Apple's service, iOS).
/// Signs in with the Firebase project's service account (the <c>FirebaseServiceAccount</c> setting).
/// </summary>
public sealed class FcmPushSender(string serviceAccountJson, string projectId) : IPushSender
{
    private readonly FirebaseMessaging _messaging = FirebaseMessaging.GetMessaging(FirebaseApp.Create(new AppOptions
    {
        Credential = CredentialFactory.FromJson<ServiceAccountCredential>(serviceAccountJson).ToGoogleCredential(),
        ProjectId = projectId,
    }, "push"));

    public async Task<IReadOnlyList<string>> SendAsync(PushMessage message, CancellationToken cancellationToken)
    {
        var answer = await _messaging.SendEachForMulticastAsync(ToFcm(message), cancellationToken);
        return answer.Responses
            .Select((response, i) => (response, token: message.Tokens[i]))
            .Where(r => r.response.Exception?.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.SenderIdMismatch)
            .Select(r => r.token)
            .ToList();
    }

    /// <summary>
    /// No top-level notification: on iOS that would always show an alert, and a quiet message must only set the badge.
    /// Android gets its notification inside <see cref="AndroidConfig"/> either way (the quiet channel keeps it silent).
    /// </summary>
    internal static MulticastMessage ToFcm(PushMessage message) => new()
    {
        // The phone's Plugin.Firebase.CloudMessaging hands out registration tokens, not the installation ids (Fids)
        // the SDK now prefers; switch when the phone side moves to them.
#pragma warning disable CS0618
        Tokens = message.Tokens.ToList(),
#pragma warning restore CS0618
        Data = message.Data.ToDictionary(),
        Android = new AndroidConfig
        {
            Priority = message.Alert ? Priority.High : Priority.Normal,
            CollapseKey = message.Tag,
            Notification = new AndroidNotification
            {
                Title = message.Title,
                Body = message.Body,
                ChannelId = message.AndroidChannel,
                Tag = message.Tag,
                NotificationCount = message.Badge,
            },
        },
        Apns = new ApnsConfig
        {
            Headers = new Dictionary<string, string>
            {
                ["apns-push-type"] = "alert",
                ["apns-priority"] = message.Alert ? "10" : "5",
                ["apns-collapse-id"] = message.Tag,
            },
            Aps = new Aps
            {
                Badge = message.Badge,
                Alert = message.Alert ? new ApsAlert { Title = message.Title, Body = message.Body } : null,
                Sound = message.Alert ? "default" : null,
                ThreadId = message.Tag,
            },
        },
    };
}

/// <summary>Used when the <c>FirebaseServiceAccount</c> setting is missing (a PC without it): messages still send, nobody is notified.</summary>
public sealed class NoPushSender(ILogger<NoPushSender> logger) : IPushSender
{
    public Task<IReadOnlyList<string>> SendAsync(PushMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning("Push not sent (no FirebaseServiceAccount setting): {Title}", message.Title);
        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}
