using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The worker's badges: what they sent, how each stands, and sending a new proof. A proof is sent only with the
/// consent box ticked (the photo is of an ID or a document), and never twice for the same badge.
/// </summary>
public sealed class ProBadges(IVerificationRepository repository, ILogger<ProBadges> logger)
{
    /// <summary>Everything the worker sent, newest first.</summary>
    public IReadOnlyList<BadgeRequest> Requests { get; private set; } = [];

    public event Action? Changed;

    /// <summary>How many kinds of badge there are (ID, NBI / police clearance).</summary>
    public static int Kinds => Enum.GetValues<BadgeKind>().Length;

    /// <summary>How long Pro home trusts the last read (an operator may approve one at any time).</summary>
    public static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(5);

    private DateTimeOffset? _loadedAt;

    /// <summary>Reads the worker's requests again (opening the badges page or Me; after sending one).</summary>
    public async Task LoadAsync()
    {
        Requests = await repository.GetMineAsync();
        _loadedAt = DateTimeOffset.UtcNow;
        Changed?.Invoke();
    }

    /// <summary>Reads them only when the last read is older than <see cref="FreshFor"/> (Pro home, opened often).</summary>
    public Task RefreshIfStaleAsync() =>
        _loadedAt is { } at && DateTimeOffset.UtcNow - at < FreshFor ? Task.CompletedTask : LoadAsync();

    /// <summary>
    /// The request of this kind that counts, or null when none was sent: approved beats waiting beats expired beats
    /// rejected, the newest first among equals.
    /// </summary>
    public BadgeRequest? Of(BadgeKind kind) =>
        Requests
            .Where(r => r.Badge.Kind == kind)
            .OrderBy(r => Rank(r.State))
            .ThenByDescending(r => r.SentAt)
            .FirstOrDefault();

    /// <summary>How a kind of badge stands overall: approved beats waiting beats rejected.</summary>
    public BadgeState StateOf(BadgeKind kind) => Of(kind)?.State ?? BadgeState.None;

    /// <summary>How many kinds of badge the worker holds (0 to <see cref="Kinds"/>).</summary>
    public int ApprovedKinds => Enum.GetValues<BadgeKind>().Count(kind => StateOf(kind) == BadgeState.Approved);

    /// <summary>
    /// True when a new proof can be sent for this kind: only when nothing is waiting or approved (a clearance whose
    /// "valid until" passed can be sent again).
    /// </summary>
    public bool CanSend(BadgeKind kind) => StateOf(kind) is BadgeState.None or BadgeState.Rejected or BadgeState.Expired;

    /// <summary>
    /// What the photo check found on the proof just sent for <paramref name="badge"/>, while it waits for the checker;
    /// empty when nothing (then there is nothing to ask).
    /// </summary>
    public IReadOnlyList<ProofProblem> ProblemsOf(Badge badge) =>
        Of(badge.Kind) is { State: BadgeState.Pending } request ? request.PhotoProblems ?? [] : [];

    /// <summary>
    /// Sends the picked photo as proof for <paramref name="badge"/>. Nothing leaves the phone without
    /// <paramref name="consent"/>. With <paramref name="retake"/> it replaces the photo still waiting for this badge.
    /// Never throws: a failure is logged and answered as <see cref="BadgeSubmitResult.Failed"/>.
    /// </summary>
    public async Task<BadgeSubmitResult> SubmitAsync(Badge badge, bool consent, PhotoPick pick, bool retake = false)
    {
        if (!consent)
        {
            return BadgeSubmitResult.NoConsent;
        }

        switch (pick.Status)
        {
            case PhotoPickStatus.Picked when pick.Photos.Count > 0:
                break;
            case PhotoPickStatus.PermissionDenied or PhotoPickStatus.CameraDenied:
                return BadgeSubmitResult.PhotoDenied;
            case PhotoPickStatus.Failed:
                return BadgeSubmitResult.Failed;
            default:
                return BadgeSubmitResult.NoPhoto;
        }

        try
        {
            var result = await repository.SubmitAsync(badge, pick.Photos[0], retake);
            await LoadAsync();
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Only that it failed: nothing about the document goes into the logs.
            logger.LogError("A badge proof didn't send ({Kind}): {Failure}.", badge.Kind, ex.GetType().Name);
            return BadgeSubmitResult.Failed;
        }
    }

    private static int Rank(BadgeState state) => state switch
    {
        BadgeState.Approved => 0,
        BadgeState.Pending => 1,
        BadgeState.Expired => 2,
        BadgeState.Rejected => 3,
        _ => 4,
    };
}
