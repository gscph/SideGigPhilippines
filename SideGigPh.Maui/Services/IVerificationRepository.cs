using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The proofs a worker sends for badges (a government ID, a TESDA certificate, a past project). The photo is
/// sensitive: it goes to private storage, only a checker at Tarbaho sees it, and it is deleted once checked.
/// </summary>
public interface IVerificationRepository
{
    /// <summary>The caller's own requests and how each stands, newest first. Empty when it can't be read.</summary>
    Task<IReadOnlyList<BadgeRequest>> GetMineAsync();

    /// <summary>
    /// Sends the photo for <paramref name="badge"/>. Call it only after the worker ticked the consent box: the call
    /// tells the server they agreed. Answers <see cref="BadgeSubmitResult.Sent"/> or
    /// <see cref="BadgeSubmitResult.AlreadySent"/>; throws when it didn't send.
    /// </summary>
    Task<BadgeSubmitResult> SubmitAsync(Badge badge, byte[] photo);
}
