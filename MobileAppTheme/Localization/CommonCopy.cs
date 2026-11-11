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

    // --- Sign-in (Components/Auth) ----------------------------------------
    public static readonly LocalizedText AuthTitle = new("Sign in to continue", "Mag-sign in para tumuloy");
    public static readonly LocalizedText AuthOr = new("or", "o");
    public static readonly LocalizedText AuthGoogle = new("Continue with Google", "Tuloy gamit ang Google");
    public static readonly LocalizedText AuthConnecting = new("Connecting…", "Kumokonekta…");
    public static readonly LocalizedText AuthPhoneLabel = new("Mobile number", "Mobile number");
    public static readonly LocalizedText AuthSendCode = new("Send code", "I-send ang code");
    public static readonly LocalizedText AuthSending = new("Sending…", "Sine-send…");
    public static readonly LocalizedText AuthCodeLabel = new("Enter the 6-digit code", "Ilagay ang 6-digit code");
    public static readonly LocalizedText AuthVerify = new("Verify and continue", "I-verify at tumuloy");
    public static readonly LocalizedText AuthVerifying = new("Verifying…", "Vine-verify…");
    public static readonly LocalizedText AuthOtherNumber = new("Use a different number", "Ibang number na lang");
    public static readonly LocalizedText AuthErrorNetwork = new("No connection. Check your internet and try again.", "Walang connection. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText AuthErrorInvalidCode = new("Wrong or expired code. Check the SMS and try again.", "Mali o expired ang code. I-check ang SMS at subukan ulit.");
    public static readonly LocalizedText AuthErrorInvalidPhone = new("Check the mobile number.", "I-check ang mobile number.");
    public static readonly LocalizedText AuthErrorPhoneInUse = new("Another account already uses this number. Sign in with your number instead.", "May ibang account na gumagamit ng number na 'to. Mag-sign in na lang gamit ang number mo.");
    public static readonly LocalizedText AuthNameTitle = new("What's your name?", "Ano'ng pangalan mo?");
    public static readonly LocalizedText AuthNameWhy = new("This is the name people you talk to will see.", "Ito ang pangalang makikita ng mga kausap mo.");
    public static readonly LocalizedText AuthFirstName = new("First name", "Pangalan");
    public static readonly LocalizedText AuthLastName = new("Last name", "Apelyido");
    public static readonly LocalizedText AuthPhoneTitle = new("Add your mobile number", "Ilagay ang mobile number mo");
    public static readonly LocalizedText AuthPhoneWhy = new("We'll text you a code to check it's yours. You only do this once.", "Ite-text ka namin ng code para masigurong sa'yo ito. Isang beses lang 'to.");
    public static readonly LocalizedText AuthErrorTooManyAttempts = new("Too many tries. Wait a while, then try again.", "Sobrang daming try. Maghintay muna bago ulitin.");
    public static readonly LocalizedText AuthErrorNotAvailable = new("Sign-in isn't working right now. Please try again later.", "Hindi gumagana ang sign-in ngayon. Subukan ulit mamaya.");
    public static readonly LocalizedText AuthErrorUnknown = new("Sign-in failed. Please try again.", "Hindi naka-sign in. Subukan ulit.");

    // --- Relative times. {0} = a number. ----------------------------------
    public static readonly LocalizedText JustNow = new("Just now", "Ngayon lang");
    public static readonly LocalizedText MinutesAgo = new("{0} min ago", "{0} min ago");
    public static readonly LocalizedText HoursAgo = new("{0} hr ago", "{0} hr ago");
    public static readonly LocalizedText Yesterday = new("Yesterday", "Kahapon");
    public static readonly LocalizedText DaysAgo = new("{0} days ago", "{0} araw na");
}
