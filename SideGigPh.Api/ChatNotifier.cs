using Microsoft.Extensions.Logging;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Tells the other person in a chat about a new message. A customer writing to a Skilled Worker gets a normal
/// notification: a new customer is work, and a slow answer loses it. A Skilled Worker answering a Seeker only
/// puts a red dot on the app icon: Seekers didn't sign up for a noisy app. A failed notification never fails
/// the message.
/// </summary>
public sealed class ChatNotifier(PushDeviceStore devices, IPushSender sender, ILogger<ChatNotifier> logger)
{
    /// <summary>The longest message preview in a notification.</summary>
    public const int MaxPreview = 120;

    public async Task NotifyAsync(ChatDto chat, string senderUid, CancellationToken cancellationToken)
    {
        try
        {
            var toWorker = senderUid == chat.SeekerId;
            var receiver = toWorker ? chat.ProfessionalId : chat.SeekerId;
            if (await devices.TargetAsync(receiver, asSeeker: !toWorker, cancellationToken) is not { } target)
            {
                return;
            }

            var dead = await sender.SendAsync(Message(chat, toWorker, target), cancellationToken);
            await devices.RemoveAsync(dead, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't notify about a message in chat {ChatId}", chat.Id);
        }
    }

    internal static PushMessage Message(ChatDto chat, bool toWorker, PushTarget target)
    {
        var taglish = target.Language == Wire.Taglish;
        var last = chat.LastMessage;
        var from = toWorker ? chat.SeekerName : chat.ProfessionalName;
        var title = toWorker && last.At == chat.CreatedAt
            ? (taglish ? $"Bagong customer: {from}" : $"New customer: {from}")
            : from;
        var body = last.Kind switch
        {
            Wire.Call => taglish ? "Tumawag sa'yo" : "Called you",
            Wire.Pin => taglish ? "Na-send ang location niya. I-tap para sa directions." : "Sent you their location. Tap for directions.",
            _ => Preview(last.Text),
        };

        return new PushMessage(
            target.Tokens,
            title,
            body,
            Alert: toWorker,
            Tag: chat.Id,
            Badge: target.UnreadChats,
            AndroidChannel: toWorker ? PushData.CustomersChannel : PushData.RepliesChannel,
            new Dictionary<string, string>
            {
                [PushData.ChatId] = chat.Id,
                [PushData.ProfessionalId] = chat.ProfessionalId,
                [PushData.Side] = toWorker ? Wire.Professional : Wire.Seeker,
                // Plugin.Firebase: while the app is open, don't add a tray notification; the app refreshes its Inbox instead.
                ["is_silent_in_foreground"] = "true",
            });
    }

    internal static string Preview(string? text)
    {
        var line = (text ?? string.Empty).ReplaceLineEndings(" ").Trim();
        return line.Length > MaxPreview ? $"{line[..(MaxPreview - 1)]}…" : line;
    }
}
