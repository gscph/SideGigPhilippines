namespace MobileAppTheme.Localization;

/// <summary>
/// The skeleton's own UI strings (shell, generic components, relative times), English and Taglish
/// side by side. App strings live in the app's own <c>Copy</c> class, never here. Keep both versions
/// short and plain; Taglish is everyday conversational Taglish.
/// </summary>
public static class CommonCopy
{
    // --- Navigation -------------------------------------------------------
    public static readonly LocalizedText Next = new("Next", "Next");
    public static readonly LocalizedText Back = new("Back", "Back");
    public static readonly LocalizedText NavProfile = new("Profile", "Profile");
    public static readonly LocalizedText ShowSection = new("Show or hide", "Ipakita o itago");
    public static readonly LocalizedText Cancel = new("Cancel", "Huwag na lang");

    // --- Not found page ---------------------------------------------------
    public static readonly LocalizedText NotFoundTitle = new("Not Found", "Wala dito");
    public static readonly LocalizedText NotFoundBody = new(
        "Sorry, the content you are looking for does not exist.",
        "Sorry, wala ang page na hinahanap mo.");

    // --- Me page ----------------------------------------------------------
    public static readonly LocalizedText AccountLanguage = new("Language", "Language");
    public static readonly LocalizedText LanguageEnglish = new("English", "English");
    public static readonly LocalizedText LanguageTaglish = new("Taglish", "Taglish");
    public static readonly LocalizedText AccountOnPhone = new(
        "No sign-in yet: everything you add is saved on this phone.",
        "Wala pang sign-in: naka-save sa phone na ito lahat ng nilagay mo.");
    public static readonly LocalizedText StartOver = new("Start over", "Magsimula ulit");
    public static readonly LocalizedText StartOverWhy = new(
        "Delete everything on this phone and answer the first questions again.",
        "Burahin lahat sa phone na ito at sagutin ulit ang mga unang tanong.");
    public static readonly LocalizedText StartOverAsk = new("Delete everything and start over?", "Burahin lahat at magsimula ulit?");
    public static readonly LocalizedText StartOverWarning = new(
        "Everything you added on this phone will be deleted. You can't undo this.",
        "Mabubura lahat ng nilagay mo sa phone na ito. Hindi na ito maibabalik.");
    public static readonly LocalizedText StartOverYes = new("Yes, delete and start over", "Oo, burahin at magsimula ulit");

    // --- Relative times. {0} = a number. ----------------------------------
    public static readonly LocalizedText JustNow = new("Just now", "Ngayon lang");
    public static readonly LocalizedText MinutesAgo = new("{0} min ago", "{0} min ago");
    public static readonly LocalizedText HoursAgo = new("{0} hr ago", "{0} hr ago");
    public static readonly LocalizedText Yesterday = new("Yesterday", "Kahapon");
    public static readonly LocalizedText DaysAgo = new("{0} days ago", "{0} araw na");
}
