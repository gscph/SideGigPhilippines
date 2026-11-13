using System.Collections.Concurrent;
using SideGigPh.Api;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// Stands in for Firebase Cloud Messaging: keeps every notification the API sends, so a test can check who was told
/// what. Tokens in <see cref="Dead"/> answer "no longer reaches a phone", like Google does after an app is removed.
/// </summary>
public sealed class RecordingPushSender : IPushSender
{
    public ConcurrentQueue<PushMessage> Sent { get; } = new();

    public ConcurrentDictionary<string, bool> Dead { get; } = new();

    /// <summary>The notifications sent to <paramref name="token"/>, oldest first.</summary>
    public IReadOnlyList<PushMessage> To(string token) => Sent.Where(m => m.Tokens.Contains(token)).ToList();

    public Task<IReadOnlyList<string>> SendAsync(PushMessage message, CancellationToken cancellationToken)
    {
        Sent.Enqueue(message);
        return Task.FromResult<IReadOnlyList<string>>(message.Tokens.Where(Dead.ContainsKey).ToList());
    }
}
