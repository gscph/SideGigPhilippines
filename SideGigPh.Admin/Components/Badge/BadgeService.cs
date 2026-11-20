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
        _pending = await api.GetAsync($"admin/verifications?take={PageSize}", ApiJson.Default.ListAdminVerificationDto);

    /// <summary>The proof photo as an image source (the API logs every look), or null when it is gone.</summary>
    public Task<string?> DocumentAsync(AdminVerificationDto request) => api.GetImageAsync($"admin/verifications/{request.Id}/document");

    /// <summary>A reason the worker will read: trimmed, at most <see cref="MaxReason"/> characters, or null when empty.</summary>
    public static string? CleanReason(string? reason)
    {
        var text = reason?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length > MaxReason ? text[..MaxReason] : text;
    }

    public static bool CanReject(string? reason) => CleanReason(reason) is not null;

    /// <summary>Sends the decision and takes the request off the list. Throws <see cref="AdminApiException"/> when it didn't go through.</summary>
    public async Task DecideAsync(AdminVerificationDto request, bool approve, string? reason)
    {
        if (!approve && !CanReject(reason))
        {
            throw new AdminApiException("Write a reason first: the worker reads it.");
        }

        await api.PostAsync($"admin/verifications/{request.Id}/decision",
            new VerificationDecisionRequest(approve, approve ? null : CleanReason(reason)), ApiJson.Default.VerificationDecisionRequest);
        _pending.RemoveAll(item => item.Id == request.Id);
    }
}
