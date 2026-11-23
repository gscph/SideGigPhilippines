using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>"Is it safe?" answered without overclaiming: what we check, chat before numbers, report and block. Never "verified" or "trusted".</summary>
public partial class TrustSection
{
    private sealed record Point(string Icon, Say Title, Say Text);

    private static readonly Point[] Points =
    [
        new("images/icons/profile-card.svg", Copy.Trust.BadgesTitle, Copy.Trust.BadgesText),
        new("images/icons/chat.svg", Copy.Trust.PrivateTitle, Copy.Trust.PrivateText),
        new("images/icons/flag.svg", Copy.Trust.ReportTitle, Copy.Trust.ReportText),
    ];
}
