using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The operator's side of badges (<c>ops/…</c> routes; only the accounts in the server's <c>AdminUids</c> get
/// answers). Reads that fail come back empty; a decision that fails throws.
/// </summary>
public interface IBadgeReviewRepository
{
    /// <summary>Whether the signed-in account is an operator (false when it isn't, or the call failed).</summary>
    Task<bool> IsOperatorAsync();

    /// <summary>The requests waiting for a decision, oldest first.</summary>
    Task<IReadOnlyList<PendingBadge>> PendingAsync(int take);

    /// <summary>The proof photo (JPEG bytes), or null when it is gone or didn't load. Every look is logged on the server.</summary>
    Task<byte[]?> DocumentAsync(long id);

    /// <summary>Approves or rejects; the proof photo is deleted on the server either way.</summary>
    Task DecideAsync(long id, bool approve, string? reason);
}
