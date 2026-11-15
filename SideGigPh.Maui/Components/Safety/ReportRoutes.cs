using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Components.Safety;

/// <summary>
/// The report page's route, shared by both sides (<c>SeekerRoutes.Report</c>, <c>ProRoutes.Report</c> build it):
/// <c>/report/{side}/{subject}/{personId}?chat=…&amp;back=…</c>.
/// </summary>
public static class ReportRoutes
{
    public const string SeekerSide = "seeker";
    public const string ProSide = "professional";

    public static string Report(string side, string personId, ReportSubject subject, string back, string? chatId) =>
        $"/report/{side}/{subject.ToString().ToLowerInvariant()}/{Uri.EscapeDataString(personId)}"
        + $"?back={Uri.EscapeDataString(back)}"
        + (chatId is null ? string.Empty : $"&chat={Uri.EscapeDataString(chatId)}");

    /// <summary>A Back/Done target inside the app, or null (a link from outside could name another site).</summary>
    public static string? SafeBack(string? back) =>
        back is { Length: > 1 } && back[0] == '/' && back[1] != '/' && back[1] != '\\' ? back : null;
}
