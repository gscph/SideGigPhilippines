using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>The Professional side's pages, so links and tabs never drift from the <c>@page</c> routes.</summary>
public static class ProRoutes
{
    public const string Home = "/professional";
    public const string Inbox = "/professional/inbox";

    /// <summary>What customers nearby posted for the pro's trade (the Requests tab).</summary>
    public const string Requests = JobPosts.JobPostRoutes.Feed;
    public const string Reviews = "/professional/reviews";
    public const string Publish = "/professional/publish";
    public const string Me = "/professional/me";

    /// <summary>Work photos, opened from the Me page; under <see cref="Me"/> so the Me tab stays lit.</summary>
    public const string Photos = Me + "/photos";

    /// <summary>The pro's private references, opened from the Me page and the setup checklist; under <see cref="Me"/>.</summary>
    public const string References = Me + "/references";

    /// <summary>The pro's badges (ID, TESDA, past projects), opened from the Me page; under <see cref="Me"/>.</summary>
    public const string Badges = Me + "/badges";

    /// <summary>Where the worker works from: their base point and how far they travel (<c>WorkAreaPage</c>).</summary>
    public const string WorkArea = Me + "/area";

    /// <summary>Sending the proof for one kind of badge; under <see cref="Badges"/>.</summary>
    public static string BadgeProof(BadgeKind kind) => $"{Badges}/{kind.ToString().ToLowerInvariant()}";

    /// <summary>The feedback board (suggest features, vote), opened from the Me page.</summary>
    public static readonly string Feedback = Components.Feedback.FeedbackRoutes.Board(Components.Feedback.FeedbackRoutes.ProSide);

    /// <summary>A chat with a customer; under <see cref="Inbox"/> so the Inbox tab stays lit.</summary>
    public static string Chat(string chatId) => $"{Inbox}/{Uri.EscapeDataString(chatId)}";

    /// <summary>Report or block a customer (from their chat or review); Back and Done return to <paramref name="back"/>.</summary>
    public static string Report(string personId, ReportSubject subject, string back, string? chatId = null) =>
        Safety.ReportRoutes.Report(Safety.ReportRoutes.ProSide, personId, subject, back, chatId);
}
