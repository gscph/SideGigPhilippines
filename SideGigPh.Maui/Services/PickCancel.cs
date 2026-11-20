using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>What became of calling a picked job off (<see cref="JobBoard.CancelPickAsync"/>).</summary>
public enum CancelPickResult
{
    Cancelled,

    /// <summary>It didn't send (no internet, server trouble): nothing changed.</summary>
    Failed,

    /// <summary>Both sides had already marked it done, so it stays (the card re-reads it).</summary>
    Completed,
}

/// <summary>
/// Calling off a picked job from the chat's job card (QA: a worker who can't come, or one who never shows up, used to
/// leave the post closed for good). Either side may, until both marked it done; the post opens again for its other
/// offers, the numbers stop showing, and the person's app sends the other a chat message with the reason.
/// </summary>
public static class PickCancel
{
    /// <summary>The longest note the server keeps.</summary>
    public const int MaxNote = PickCancelRules.MaxNote;

    /// <summary>The reasons each side picks from, most likely first ("Other" last).</summary>
    public static IReadOnlyList<string> ReasonsFor(bool household) => household
        ? [PickCancelReasons.NoShow, PickCancelReasons.NoReply, PickCancelReasons.NotNeeded, PickCancelReasons.Price, PickCancelReasons.Other]
        : [PickCancelReasons.CantMakeIt, PickCancelReasons.NoReply, PickCancelReasons.Price, PickCancelReasons.Other];

    /// <summary>A completed job (both marked done) stays; anything before that can be called off.</summary>
    public static bool CanCancel(PickedJob job) => !job.IsCompleted;

    /// <summary>A reason this side may give, and a note that fits (or none).</summary>
    public static bool IsReady(bool household, string? reason, string? note) =>
        reason is not null && ReasonsFor(household).Contains(reason) && PickCancelRules.IsNote(note);

    /// <summary>The note as it is sent: trimmed, null when empty.</summary>
    public static string? CleanNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
