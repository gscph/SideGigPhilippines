using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Seeker;

/// <summary>The Seeker side's pages, so links and tabs never drift from the <c>@page</c> routes.</summary>
public static class SeekerRoutes
{
    public const string Home = "/seeker";
    /// <summary>Results, opened from the home search box or a trade icon; under <see cref="Home"/> so Home stays lit.</summary>
    public const string Search = Home + "/search";
    public const string Inbox = "/inbox";

    /// <summary>The Seeker's own job posts (the Post tab).</summary>
    public const string Posts = JobPosts.JobPostRoutes.Mine;
    /// <summary>The Seeker's reviews, opened from the Me page; under <see cref="Me"/> so Me stays lit.</summary>
    public const string Reviews = Me + "/reviews";
    public const string Me = "/me";

    /// <summary>The feedback board (suggest features, vote), opened from the Me page.</summary>
    public static readonly string Feedback = Components.Feedback.FeedbackRoutes.Board(Components.Feedback.FeedbackRoutes.SeekerSide);

    /// <summary>A pro's public profile.</summary>
    public static string Pro(string professionalId) => $"/pro/{Uri.EscapeDataString(professionalId)}";

    /// <summary>A pro's public profile whose Back returns to <paramref name="back"/> (a page of this app) instead of the results.</summary>
    public static string Pro(string professionalId, string back) => $"{Pro(professionalId)}?back={Uri.EscapeDataString(back)}";

    /// <summary>All of a pro's reviews ("Show all" on the profile); Back returns to the profile.</summary>
    public static string ProReviews(string professionalId) => $"{Pro(professionalId)}/reviews";

    /// <summary><paramref name="back"/> when it is a page of this app (starts with one slash), else the results.</summary>
    public static string BackOrSearch(string? back) =>
        back is { Length: > 1 } path && path[0] == '/' && path[1] != '/' && path[1] != '\\' ? path : Search;

    /// <summary>The Inbox thread with a pro, where Contact lands.</summary>
    public static string Thread(string professionalId) => $"/inbox/{Uri.EscapeDataString(professionalId)}";

    public static string Rate(string professionalId) => $"{Reviews}/rate/{Uri.EscapeDataString(professionalId)}";

    /// <summary>The review page after a completed job: it says the review stays hidden until the worker reviews too (blind reviews).</summary>
    // job=true, not 1: Blazor reads a bool query value only as true/false, and anything else breaks the page.
    public static string RateAfterJob(string professionalId) => $"{Rate(professionalId)}?job=true";

    /// <summary>Report or block a person (a worker, or a review's writer); Back and Done return to <paramref name="back"/>.</summary>
    public static string Report(string personId, ReportSubject subject, string back, string? chatId = null) =>
        Safety.ReportRoutes.Report(Safety.ReportRoutes.SeekerSide, personId, subject, back, chatId);
}
