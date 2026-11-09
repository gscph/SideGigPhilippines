namespace SideGigPh.Maui.Components.Search;

/// <summary>One of the Seeker's answers shown above the results; tapping it reopens <see cref="Step"/>.</summary>
public sealed record AnswerChip(string Label, int Step);
