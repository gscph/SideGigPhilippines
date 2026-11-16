namespace SideGigPh.Maui.Models;

/// <summary>What a worker can show proof of. Each is checked by a person at Tarbaho before it shows.</summary>
public enum BadgeKind
{
    /// <summary>A government ID.</summary>
    Id,

    /// <summary>A TESDA National Certificate or training certificate.</summary>
    Tesda,

    /// <summary>A past project with a developer or any other company.</summary>
    Project,
}

/// <summary>
/// A badge: on a listing once approved, or what a worker asks for. For a <see cref="BadgeKind.Project"/> badge,
/// <paramref name="DeveloperId"/> is one of <c>BadgeCatalog.Developers</c> or <c>BadgeCatalog.OtherCompany</c> with
/// the company typed in <paramref name="CompanyName"/>; <paramref name="ProjectName"/> is the building, when given.
/// </summary>
public sealed record Badge(BadgeKind Kind, string? DeveloperId = null, string? CompanyName = null, string? ProjectName = null);

/// <summary>Where a worker's request for a badge stands.</summary>
public enum BadgeState
{
    /// <summary>Nothing sent yet.</summary>
    None,

    /// <summary>Sent, waiting for a person at Tarbaho to check it.</summary>
    Pending,

    /// <summary>Checked: the badge shows on the listing.</summary>
    Approved,

    /// <summary>Not accepted; the worker can send a better photo.</summary>
    Rejected,
}

/// <summary>One proof the worker sent and how it stands. <paramref name="RejectReason"/> is what the checker wrote, when rejected.</summary>
public sealed record BadgeRequest(long Id, Badge Badge, BadgeState State, string? RejectReason, DateTimeOffset SentAt);

/// <summary>What sending a proof ended with.</summary>
public enum BadgeSubmitResult
{
    Sent,

    /// <summary>The consent box wasn't ticked: nothing was sent.</summary>
    NoConsent,

    /// <summary>The company's name is missing or too short.</summary>
    NoCompany,

    /// <summary>The worker closed the photo picker without choosing.</summary>
    NoPhoto,

    /// <summary>The phone said no to reading photos.</summary>
    PhotoDenied,

    /// <summary>This badge is already waiting to be checked, or already approved.</summary>
    AlreadySent,

    /// <summary>It didn't send (no internet, or the server refused the photo).</summary>
    Failed,
}
