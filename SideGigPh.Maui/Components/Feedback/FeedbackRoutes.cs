using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;

namespace SideGigPh.Maui.Components.Feedback;

/// <summary>
/// The feedback board's routes, shared by both sides (<c>SeekerRoutes.Feedback</c>, <c>ProRoutes.Feedback</c> build
/// them): <c>/feedback/{side}</c> is the board, <c>/feedback/{side}/new</c> the suggest step. The side only says
/// where Back goes (that side's Me page).
/// </summary>
public static class FeedbackRoutes
{
    public const string SeekerSide = "seeker";
    public const string ProSide = "professional";

    /// <summary>The board: every suggestion, most voted first.</summary>
    public static string Board(string side) => $"/feedback/{Side(side)}";

    /// <summary>Suggest a feature (checks for one like it first).</summary>
    public static string Suggest(string side) => $"{Board(side)}/new";

    /// <summary>Where the board's Back goes: the Me page of that side.</summary>
    public static string Home(string side) => Side(side) == ProSide ? ProRoutes.Me : SeekerRoutes.Me;

    /// <summary>A side from a link is one of the two; anything else is the Seeker's.</summary>
    private static string Side(string side) => side == ProSide ? ProSide : SeekerSide;
}
