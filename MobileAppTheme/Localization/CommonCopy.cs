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
    public static readonly LocalizedText Close = new("Close", "Isara");
    public static readonly LocalizedText Progress = new("Progress", "Progress");

    // --- Busy overlay (rotates while the app waits on something) ----------
    public static readonly LocalizedText BusyWorking = new("Working on it…", "Ginagawa na…");
    public static readonly LocalizedText BusyThinking = new("Thinking…", "Nag-iisip…");
    public static readonly LocalizedText BusyProcessing = new("Processing…", "Pinoproseso…");
    public static readonly LocalizedText BusyConnecting = new("Connecting…", "Kumokonekta…");
    public static readonly LocalizedText BusySending = new("Sending…", "Pinapadala…");
    public static readonly LocalizedText BusyChecking = new("Checking…", "Chine-check…");
    public static readonly LocalizedText BusySaving = new("Saving…", "Sine-save…");
    public static readonly LocalizedText BusyTidying = new("Tidying up…", "Inaayos…");
    public static readonly LocalizedText BusyAlmost = new("Almost there…", "Malapit na…");
    public static readonly LocalizedText BusyHangTight = new("Hang tight…", "Sandali lang…");

    // --- Not found page ---------------------------------------------------
    public static readonly LocalizedText NotFoundTitle = new("Not Found", "Wala dito");
    public static readonly LocalizedText NotFoundBody = new(
        "Sorry, the content you are looking for does not exist.",
        "Sorry, wala ang page na hinahanap mo.");

    // --- Me page ----------------------------------------------------------
    public static readonly LocalizedText AccountLanguage = new("Language", "Language");
    public static readonly LocalizedText LanguageEnglish = new("English", "English");
    public static readonly LocalizedText LanguageTaglish = new("Taglish", "Taglish");
    public static readonly LocalizedText StartOver = new("Start over", "Magsimula ulit");
    public static readonly LocalizedText StartOverWhy = new(
        "Delete everything on this phone and answer the first questions again.",
        "Burahin lahat sa phone na ito at sagutin ulit ang mga unang tanong.");
    public static readonly LocalizedText StartOverAsk = new("Delete everything and start over?", "Burahin lahat at magsimula ulit?");
    public static readonly LocalizedText StartOverWarning = new(
        "Everything you added on this phone will be deleted. You can't undo this.",
        "Mabubura lahat ng nilagay mo sa phone na ito. Hindi na ito maibabalik.");
    public static readonly LocalizedText StartOverYes = new("Yes, delete and start over", "Oo, burahin at magsimula ulit");
    public static readonly LocalizedText DeleteAccount = new("Delete account", "I-delete ang account");
    public static readonly LocalizedText DeleteAccountWhy = new(
        "Delete your account and everything in it for good. You will be signed out.",
        "Burahin nang tuluyan ang account mo at lahat ng laman nito. Masa-sign out ka.");
    public static readonly LocalizedText DeleteAccountAsk = new("Delete your account for good?", "I-delete nang tuluyan ang account mo?");
    public static readonly LocalizedText DeleteAccountWarning = new(
        "Your profile, photos, chats and reviews will be deleted. You can't undo this.",
        "Mabubura ang profile, photos, chats at reviews mo. Hindi na ito maibabalik.");
    public static readonly LocalizedText DeleteAccountYes = new("Yes, delete my account", "Oo, i-delete ang account ko");
    public static readonly LocalizedText DeleteAccountFailed = new(
        "Couldn't delete your account. Check your internet and try again.",
        "Hindi na-delete ang account mo. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText StartOverFailed = new(
        "Couldn't delete everything. Check your internet and try again.",
        "Hindi nabura lahat. I-check ang internet mo at subukan ulit.");

    // --- A screen that broke (MainLayout's error boundary) -----------------
    public static readonly LocalizedText ScreenBroke = new(
        "Something went wrong on this screen. We got a report and will fix it.",
        "May nagka-problema sa screen na 'to. Na-report na namin at aayusin.");
    public static readonly LocalizedText ScreenBrokeHome = new("Back to the start", "Bumalik sa simula");

    // --- No connection (the error boundary's network screen, ConnectionBanner) --
    public static readonly LocalizedText NoConnection = new(
        "No internet. Check your data or Wi-Fi, then try again.",
        "Walang internet. I-check ang data o Wi-Fi mo, tapos subukan ulit.");
    public static readonly LocalizedText TryAgain = new("Try again", "Subukan ulit");
    public static readonly LocalizedText Offline = new("You're offline. Some things won't load.", "Offline ka. May mga hindi maglo-load.");

    // --- Legal pages (AppBrand.Legal) --------------------------------------
    public static readonly LocalizedText LegalTitle = new("Privacy and terms", "Privacy at terms");
    public static readonly LocalizedText LegalPrivacy = new("Privacy Policy", "Privacy Policy");
    public static readonly LocalizedText LegalTerms = new("Terms of Service", "Terms of Service");
    public static readonly LocalizedText LegalData = new("Your data and deleting your account", "Ang data mo at pag-delete ng account");
    public static readonly LocalizedText LegalAgree = new("By continuing, you agree to our", "Kapag nagtuloy ka, sang-ayon ka sa aming");
    public static readonly LocalizedText LegalAnd = new("and", "at");

    // --- Sign-in (Components/Auth) ----------------------------------------
    public static readonly LocalizedText AuthTitle = new("Log in or sign up", "Mag-log in o mag-sign up");
    /// <summary>{0} = the app name.</summary>
    public static readonly LocalizedText AuthWelcome = new("Welcome to {0}", "Welcome sa {0}");
    public static readonly LocalizedText AuthContinue = new("Continue", "Tuloy");
    public static readonly LocalizedText AuthRegionLabel = new("Country/Region", "Bansa");
    public static readonly LocalizedText AuthPhoneHint = new(
        "We'll text you a code to confirm your number. Standard SMS rates may apply.",
        "Ite-text ka namin ng code para ma-confirm ang number mo. Baka may SMS charge depende sa network mo.");
    public static readonly LocalizedText AuthCodeTitle = new("Confirm your number", "I-confirm ang number mo");
    /// <summary>{0} = the number the code went to.</summary>
    public static readonly LocalizedText AuthCodeSent = new("Enter the code we sent by SMS to {0}.", "Ilagay ang code na tinext namin sa {0}.");
    public static readonly LocalizedText AuthNoCode = new("Didn't get a code?", "Walang dumating na code?");
    public static readonly LocalizedText AuthOr = new("or", "o");
    public static readonly LocalizedText AuthGoogle = new("Continue with Google", "Tuloy gamit ang Google");
    public static readonly LocalizedText AuthApple = new("Continue with Apple", "Tuloy gamit ang Apple");
    public static readonly LocalizedText AuthConnecting = new("Connecting…", "Kumokonekta…");
    public static readonly LocalizedText AuthPhoneLabel = new("Phone number", "Phone number");
    public static readonly LocalizedText AuthSending = new("Sending…", "Sine-send…");
    public static readonly LocalizedText AuthCodeLabel = new("6-digit code", "6-digit code");
    public static readonly LocalizedText AuthVerifying = new("Verifying…", "Vine-verify…");
    public static readonly LocalizedText AuthOtherNumber = new("Use a different number", "Ibang number na lang");
    public static readonly LocalizedText AuthSignInWithNumber = new("Sign in with this number", "Mag-sign in gamit ang number na 'to");
    public static readonly LocalizedText AuthSignOutFailed = new("Couldn't sign out. Check your signal and try again.", "Hindi maka-sign out. I-check ang signal at subukan ulit.");
    public static readonly LocalizedText AuthErrorNetwork = new("No connection. Check your internet and try again.", "Walang connection. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText AuthErrorInvalidCode = new("Wrong or expired code. Check the SMS and try again.", "Mali o expired ang code. I-check ang SMS at subukan ulit.");
    public static readonly LocalizedText AuthErrorInvalidPhone = new("Check the mobile number.", "I-check ang mobile number.");
    public static readonly LocalizedText AuthErrorPhoneInUse = new("Another account already uses this number. Sign in with your number instead.", "May ibang account na gumagamit ng number na 'to. Mag-sign in na lang gamit ang number mo.");
    public static readonly LocalizedText AuthNameTitle = new("Finish signing up", "Tapusin ang sign up");
    public static readonly LocalizedText AuthNameEditTitle = new("Change your name", "Palitan ang pangalan mo");
    public static readonly LocalizedText AuthSave = new("Save", "I-save");
    public static readonly LocalizedText AuthNameWhy = new("Use your real name. It's the name people you talk to will see.", "Gamitin ang totoong pangalan mo. Ito ang makikita ng mga kausap mo.");
    public static readonly LocalizedText AuthFirstName = new("First name", "Pangalan");
    public static readonly LocalizedText AuthLastName = new("Last name", "Apelyido");
    public static readonly LocalizedText AuthPhoneTitle = new("Add your mobile number", "Ilagay ang mobile number mo");
    public static readonly LocalizedText AuthPhoneWhy = new("This is the number people reach you on. You only add it once.", "Dito ka nila tatawagan o ite-text. Isang beses mo lang 'to ilalagay.");
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
