using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Badge;

/// <summary>
/// The badge requests waiting for the operator (the same rules as the phone's <c>BadgeReview</c>): oldest first, the
/// proof photo only on demand, and a rejection needs a short reason, which the worker sees. A person always decides,
/// comparing the profile photo with the ID by eye; what the AI read is only a help.
/// </summary>
public sealed class BadgeService(AdminApi api)
{
    /// <summary>How many requests a load brings.</summary>
    public const int PageSize = 20;

    /// <summary>The longest reason the worker is shown.</summary>
    public const int MaxReason = 200;

    private List<AdminVerificationDto> _pending = [];

    public IReadOnlyList<AdminVerificationDto> Pending => _pending;

    public async Task LoadAsync() =>
        _pending = await api.GetAsync($"ops/verifications?take={PageSize}", ApiJson.Default.ListAdminVerificationDto);

    /// <summary>The proof photo as an image source (the API logs every look), or null when it is gone.</summary>
    public Task<string?> DocumentAsync(AdminVerificationDto request) => api.GetImageAsync($"ops/verifications/{request.Id}/document");

    /// <summary>A reason the worker will read: trimmed, at most <see cref="MaxReason"/> characters, or null when empty.</summary>
    public static string? CleanReason(string? reason)
    {
        var text = reason?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length > MaxReason ? text[..MaxReason] : text;
    }

    public static bool CanReject(string? reason) => CleanReason(reason) is not null;

    /// <summary>True when approving needs the "valid until" printed on the paper: a clearance (the badge lapses after it).</summary>
    public static bool NeedsValidUntil(AdminVerificationDto request) => request.Kind == VerificationKinds.Clearance;

    /// <summary>Any badge but a clearance, or a clearance with a "valid until" from today (Philippine time) to a year ahead.</summary>
    public static bool CanApprove(AdminVerificationDto request, DateOnly? validUntil) =>
        !NeedsValidUntil(request)
        || (validUntil is { } until && ClearanceRules.IsValidUntilAllowed(until, JobPostRules.PhilippineToday(DateTimeOffset.UtcNow)));

    /// <summary>Sends the decision and takes the request off the list. Throws <see cref="AdminApiException"/> when it didn't go through.</summary>
    public async Task DecideAsync(AdminVerificationDto request, bool approve, string? reason, DateOnly? validUntil = null)
    {
        if (!approve && !CanReject(reason))
        {
            throw new AdminApiException("Write a reason first: the worker reads it.");
        }

        if (approve && !CanApprove(request, validUntil))
        {
            throw new AdminApiException("Type the clearance's \"valid until\" date first: today or later, at most a year ahead. An expired one is rejected.");
        }

        await api.PostAsync($"ops/verifications/{request.Id}/decision",
            new VerificationDecisionRequest(approve, approve ? null : CleanReason(reason), approve && NeedsValidUntil(request) ? validUntil : null),
            ApiJson.Default.VerificationDecisionRequest);
        _pending.RemoveAll(item => item.Id == request.Id);
    }
}
