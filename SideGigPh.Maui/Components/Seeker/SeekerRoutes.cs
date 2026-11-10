namespace SideGigPh.Maui.Components.Seeker;

/// <summary>The Seeker side's pages, so links and tabs never drift from the <c>@page</c> routes.</summary>
public static class SeekerRoutes
{
    public const string Home = "/seeker";
    public const string Search = "/search";
    public const string Inbox = "/inbox";
    public const string Reviews = "/reviews";
    public const string Account = "/me";

    /// <summary>A pro's public profile.</summary>
    public static string Pro(string professionalId) => $"/pro/{Uri.EscapeDataString(professionalId)}";

    /// <summary>The Inbox thread with a pro, where Contact lands.</summary>
    public static string Thread(string professionalId) => $"/inbox/{Uri.EscapeDataString(professionalId)}";

    public static string Rate(string professionalId) => $"/reviews/rate/{Uri.EscapeDataString(professionalId)}";
}
