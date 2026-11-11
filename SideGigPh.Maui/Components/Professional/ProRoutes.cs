namespace SideGigPh.Maui.Components.Professional;

/// <summary>The Professional side's pages, so links and tabs never drift from the <c>@page</c> routes.</summary>
public static class ProRoutes
{
    public const string Home = "/professional";
    public const string Inbox = "/professional/inbox";
    public const string Reviews = "/professional/reviews";
    public const string Publish = "/professional/publish";
    public const string Me = "/professional/me";

    /// <summary>Work photos, opened from the Me page; under <see cref="Me"/> so the Me tab stays lit.</summary>
    public const string Photos = Me + "/photos";

    /// <summary>A chat with a customer; under <see cref="Inbox"/> so the Inbox tab stays lit.</summary>
    public static string Chat(string chatId) => $"{Inbox}/{Uri.EscapeDataString(chatId)}";
}
