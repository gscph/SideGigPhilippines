using Microsoft.Extensions.Logging;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The operator's badge review: whether this account may see it, the requests waiting (oldest first), each proof
/// photo on demand, and the decision. A person always decides, comparing the worker's profile photo with the ID by
/// eye (no face matching); what the AI read is only a help. A rejection needs a short reason, which the worker sees.
/// Approving a clearance needs its "valid until" date, read off the paper (<see cref="ClearanceRules"/>): the badge
/// lapses after it.
/// </summary>
public sealed class BadgeReview(IBadgeReviewRepository repository, ILogger<BadgeReview> logger)
{
    /// <summary>How many requests a page loads.</summary>
    public const int PageSize = 20;

    /// <summary>The longest reason the worker is shown.</summary>
    public const int MaxReason = 200;

    private bool _isOperator;
    private List<PendingBadge> _pending = [];

    public IReadOnlyList<PendingBadge> Pending => _pending;

    /// <summary>
    /// Whether this account is an operator. A yes is kept for the rest of the launch; a no is asked again next time
    /// (it may have been a dropped call).
    /// </summary>
    public async Task<bool> IsOperatorAsync() => _isOperator || (_isOperator = await repository.IsOperatorAsync());

    public async Task LoadAsync() => _pending = [.. await repository.PendingAsync(PageSize)];

    /// <summary>The proof photo as an image source, or null when it is gone or didn't load.</summary>
    public async Task<string?> DocumentAsync(PendingBadge request) =>
        await repository.DocumentAsync(request.Id) is { Length: > 0 } bytes ? "data:image/jpeg;base64," + Convert.ToBase64String(bytes) : null;

    /// <summary>A reason the worker will read: trimmed, at most <see cref="MaxReason"/> characters, or null when empty.</summary>
    public static string? CleanReason(string? reason)
    {
        var text = reason?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length > MaxReason ? text[..MaxReason] : text;
    }

    /// <summary>A rejection needs a reason, so the worker knows what to send next time.</summary>
    public static bool CanReject(string? reason) => CleanReason(reason) is not null;

    /// <summary>True when approving <paramref name="request"/> needs a "valid until" date: a clearance.</summary>
    public static bool NeedsValidUntil(PendingBadge request) => request.Badge.Kind == BadgeKind.Clearance;

    /// <summary>
    /// True when <paramref name="request"/> may be approved with <paramref name="validUntil"/>: any badge but a
    /// clearance, or a clearance with a date from today (Philippine time) to a year ahead.
    /// </summary>
    public static bool CanApprove(PendingBadge request, DateOnly? validUntil, DateOnly today) =>
        !NeedsValidUntil(request) || (validUntil is { } until && ClearanceRules.IsValidUntilAllowed(until, today));

    /// <summary>
    /// Sends the decision and takes the request off the list. False when it didn't go through (it stays), or when a
    /// rejection has no reason or a clearance's approval no allowed "valid until".
    /// </summary>
    public async Task<bool> DecideAsync(PendingBadge request, bool approve, string? reason, DateOnly? validUntil = null)
    {
        if (approve ? !CanApprove(request, validUntil, JobPostRules.PhilippineToday(DateTimeOffset.UtcNow)) : !CanReject(reason))
        {
            return false;
        }

        try
        {
            await repository.DecideAsync(
                request.Id, approve, approve ? null : CleanReason(reason), approve && NeedsValidUntil(request) ? validUntil : null);
            _pending.RemoveAll(item => item.Id == request.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deciding a badge request failed.");
            return false;
        }
    }
}
