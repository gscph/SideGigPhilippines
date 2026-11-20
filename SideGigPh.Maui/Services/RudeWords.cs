using SideGigPh.Contracts;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The app's side of the blocked-words check (<see cref="BlockedWords"/>, the same lists the server refuses): a form
/// whose text has a rude word shows "Please take out the rude words" under the box and won't send, so the person
/// can fix it instead of seeing "didn't send". Used on the post form, offer notes, reviews, the household's stars,
/// the feedback board, calling a pick off and the name page (<see cref="RudeWordsCheck"/>).
/// </summary>
public static class RudeWords
{
    /// <summary>True when <paramref name="text"/> has a blocked word or symbol.</summary>
    public static bool In(string? text) => BlockedWords.Contains(text);
}

/// <summary>The skeleton's <c>ITextCheck</c> port (the name page) answered with <see cref="RudeWords"/>.</summary>
public sealed class RudeWordsCheck : MobileAppTheme.Services.ITextCheck
{
    public bool IsRude(string? text) => RudeWords.In(text);
}
