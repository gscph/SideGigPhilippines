namespace SideGigPh.Maui.Models;

/// <summary>What is being reported.</summary>
public enum ReportSubject
{
    Listing,
    Review,
    Chat,
    User,
}

/// <summary>Why it is reported; the report sheet shows one tile per reason.</summary>
public enum ReportReason
{
    Scam,
    Harassment,
    Fake,
    Inappropriate,
    Employment,
    Other,
}

/// <summary>
/// A report about another person: the worker of a listing, a review's writer, or the other person in a chat
/// (<paramref name="PersonId"/>, their uid). <paramref name="ChatId"/> for a chat report. With
/// <paramref name="AlsoBlock"/> they are blocked too and disappear for the reporter at once.
/// </summary>
public sealed record UserReport(string PersonId, ReportSubject Subject, ReportReason Reason, string? ChatId, string? Note, bool AlsoBlock);
