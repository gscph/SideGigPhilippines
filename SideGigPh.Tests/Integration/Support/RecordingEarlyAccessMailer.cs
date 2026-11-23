using System.Collections.Concurrent;
using SideGigPh.Api;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>Azure Communication Services stand-in: keeps every sign-up the API emailed; <see cref="Refuse"/> makes the next ones fail.</summary>
public sealed class RecordingEarlyAccessMailer : IEarlyAccessMailer
{
    public ConcurrentQueue<EarlyAccessSignup> Sent { get; } = new();

    public bool Refuse { get; set; }

    public Task<bool> SendAsync(EarlyAccessSignup signup, CancellationToken cancellationToken)
    {
        if (Refuse)
        {
            return Task.FromResult(false);
        }

        Sent.Enqueue(signup);
        return Task.FromResult(true);
    }
}
