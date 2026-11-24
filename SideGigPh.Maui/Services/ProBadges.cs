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

    /// <summary>The newest request of each badge of this kind that still counts: approved, waiting, or rejected with nothing newer.</summary>
    public IReadOnlyList<BadgeRequest> Of(BadgeKind kind) =>
        [.. Requests
            .Where(r => r.Badge.Kind == kind)
            .GroupBy(r => (r.Badge.DeveloperId, Company: r.Badge.CompanyName?.Trim().ToUpperInvariant()))
            .Select(group => group.OrderBy(r => Rank(r.State)).ThenByDescending(r => r.SentAt).First())
            .OrderBy(r => Rank(r.State))
            .ThenByDescending(r => r.SentAt)];

    /// <summary>How a kind of badge stands overall: approved beats waiting beats rejected.</summary>
    public BadgeState StateOf(BadgeKind kind) => Of(kind).FirstOrDefault()?.State ?? BadgeState.None;

    /// <summary>How many kinds of badge the worker holds (0 to <see cref="BadgeCatalog.Kinds"/>).</summary>
    public int ApprovedKinds => Enum.GetValues<BadgeKind>().Count(kind => StateOf(kind) == BadgeState.Approved);

    /// <summary>
    /// True when a new proof can be sent for this kind: always for a past project (each company is its own badge),
    /// else only when nothing is waiting or approved.
    /// </summary>
    public bool CanSend(BadgeKind kind) => kind == BadgeKind.Project || StateOf(kind) is BadgeState.None or BadgeState.Rejected;

    /// <summary>
    /// True for the badges whose proof is photographed now with the camera, never picked from the gallery (an old
    /// photo, or a screenshot of someone else's ID), with the photo guide shown first.
    /// </summary>
    public static bool UsesCamera(BadgeKind kind) => kind is BadgeKind.Id or BadgeKind.Clearance;

    /// <summary>
    /// What the photo check found on the proof just sent for <paramref name="badge"/>, while it waits for the checker;
    /// empty when nothing (then there is nothing to ask).
    /// </summary>
    public IReadOnlyList<ProofProblem> ProblemsOf(Badge badge) =>
        Of(badge.Kind).FirstOrDefault(r => r.State == BadgeState.Pending && Same(r.Badge, badge))?.PhotoProblems ?? [];

    private static bool Same(Badge a, Badge b) =>
        a.Kind == b.Kind && (a.Kind != BadgeKind.Project
            || (a.DeveloperId == b.DeveloperId && string.Equals(a.CompanyName?.Trim(), b.CompanyName?.Trim(), StringComparison.OrdinalIgnoreCase)));

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

        if (!BadgeCatalog.HasCompany(badge))
        {
            return BadgeSubmitResult.NoCompany;
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
            var result = await repository.SubmitAsync(Clean(badge), pick.Photos[0], retake);
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

    /// <summary>Only what the kind uses: a developer's own name is never typed, and only a project has a company or building.</summary>
    private static Badge Clean(Badge badge) => badge.Kind != BadgeKind.Project
        ? new Badge(badge.Kind)
        : new Badge(
            BadgeKind.Project,
            badge.DeveloperId,
            badge.DeveloperId == BadgeCatalog.OtherCompany ? badge.CompanyName?.Trim() : null,
            string.IsNullOrWhiteSpace(badge.ProjectName) ? null : badge.ProjectName.Trim());

    private static int Rank(BadgeState state) => state switch
    {
        BadgeState.Approved => 0,
        BadgeState.Pending => 1,
        BadgeState.Rejected => 2,
        _ => 3,
    };
}
