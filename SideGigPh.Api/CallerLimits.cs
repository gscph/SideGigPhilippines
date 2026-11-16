using System.Threading.RateLimiting;

namespace SideGigPh.Api;

/// <summary>The writes one account may only make so often (<see cref="CallerLimits"/>).</summary>
public enum Limited
{
    /// <summary>A chat message (text or a logged call).</summary>
    Message,

    /// <summary>A batch of error reports from the app (diagnostics).</summary>
    ErrorReport,

    /// <summary>A Seeker starting a chat with a new Skilled Worker (each one rings that worker's phone).</summary>
    NewChat,

    /// <summary>An "Others" note (analytics).</summary>
    OtherJobNote,

    /// <summary>A photo upload (each distinct photo is a new blob).</summary>
    PhotoUpload,

    /// <summary>A review written or changed.</summary>
    Review,

    /// <summary>A report about another user (each one waits for the operator).</summary>
    Report,

    /// <summary>A suggestion posted on the feedback board (everyone sees it).</summary>
    Feedback,

    /// <summary>A vote added or taken back on the feedback board.</summary>
    FeedbackVote,

    /// <summary>A listing published, changed or taken down (each one clears the shared search cache).</summary>
    Listing,

    /// <summary>The caller's own profile saved.</summary>
    Profile,

    /// <summary>A proof sent for a badge (each ID is a paid AI read and waits for the operator).</summary>
    Verification,
}

/// <summary>
/// Per-account limits on the writes a signed-in caller could repeat to flood other people or our storage. Each is far
/// above what a person does by hand; past it the API answers <c>429 too-many-requests</c> and the app shows its usual
/// "didn't send" message. Counted in memory per API instance (a sliding window), so they slow abuse down without a
/// database round trip; they are not exact across instances.
/// </summary>
public sealed class CallerLimits : IDisposable
{
    /// <summary>How many of each write one account may make in the window.</summary>
    public static readonly IReadOnlyDictionary<Limited, (int Permits, TimeSpan Window)> Rules = new Dictionary<Limited, (int, TimeSpan)>
    {
        [Limited.ErrorReport] = (12, TimeSpan.FromHours(1)),
        [Limited.Message] = (30, TimeSpan.FromMinutes(1)),
        [Limited.NewChat] = (20, TimeSpan.FromHours(1)),
        [Limited.OtherJobNote] = (20, TimeSpan.FromHours(1)),
        [Limited.PhotoUpload] = (10, TimeSpan.FromMinutes(15)),
        [Limited.Review] = (30, TimeSpan.FromHours(1)),
        [Limited.Report] = (10, TimeSpan.FromHours(1)),
        [Limited.Feedback] = (10, TimeSpan.FromHours(1)),
        [Limited.FeedbackVote] = (120, TimeSpan.FromHours(1)),
        [Limited.Listing] = (20, TimeSpan.FromHours(1)),
        [Limited.Profile] = (60, TimeSpan.FromHours(1)),
        [Limited.Verification] = (12, TimeSpan.FromHours(1)),
    };

    private readonly PartitionedRateLimiter<(Limited Kind, string Uid)> _limiter =
        PartitionedRateLimiter.Create<(Limited Kind, string Uid), (Limited, string)>(key =>
            RateLimitPartition.GetSlidingWindowLimiter(key, k => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = Rules[k.Item1].Permits,
                Window = Rules[k.Item1].Window,
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    /// <summary>True (and counted) when <paramref name="uid"/> may make one more <paramref name="kind"/> write now.</summary>
    public bool TryTake(Limited kind, string uid)
    {
        using var lease = _limiter.AttemptAcquire((kind, uid));
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}
