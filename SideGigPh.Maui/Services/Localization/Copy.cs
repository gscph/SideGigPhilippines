using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// Every UI string, English and Taglish side by side. Many of our users are not confident
/// English readers: keep both versions short and plain, and keep Taglish conversational.
/// Reference-data labels (jobs, towns, prices) live in <see cref="ReferenceData"/>.
/// </summary>
public static class Copy
{
    // --- Common -----------------------------------------------------------
    public static readonly LocalizedText Next = new("Next", "Tuloy");
    public static readonly LocalizedText Back = new("Back", "Bumalik");

    // --- Role fork --------------------------------------------------------
    public static readonly LocalizedText RoleTitle = new("What brings you here?", "Ano'ng hanap mo?");
    public static readonly LocalizedText RoleSeeker = new("I need someone for a job", "May ipapagawa ako");
    public static readonly LocalizedText RoleSeekerHint = new("Aircon, plumbing, repairs, errands, massage", "Aircon, tubero, repair, PasaBuy, masahe");
    public static readonly LocalizedText RoleProfessional = new("I want to earn from my skills", "Gusto kong kumita sa skills ko");
    public static readonly LocalizedText RoleProfessionalHint = new("Get customers near you", "Magkaroon ng customer malapit sa 'yo");

    // --- Seeker interview -------------------------------------------------
    public static readonly LocalizedText SeekerCategoryTitle = new("What do you need done?", "Ano'ng ipapagawa mo?");
    public static readonly LocalizedText SeekerServiceTitle = new("What kind of job?", "Anong klaseng trabaho?");
    public static readonly LocalizedText SeekerServiceWhy = new("So you only see pros who do exactly this.", "Para 'yung gumagawa talaga nito ang makita mo.");
    public static readonly LocalizedText SeekerCityTitle = new("Where's the job?", "Saan ang trabaho?");
    public static readonly LocalizedText SeekerCityWhy = new("So you only see pros who go to your area.", "Para 'yung pumupunta lang sa lugar mo ang makita mo.");
    public static readonly LocalizedText CitiesNote = new("More towns coming soon.", "Marami pang bayan, abangan!");
    public static readonly LocalizedText SeekerWhenTitle = new("When do you need it?", "Kailan mo kailangan?");
    public static readonly LocalizedText SeekerWhenWhy = new("We'll tell the pro when you need it.", "Sasabihin namin sa pro kung kailan mo kailangan.");
    public static readonly LocalizedText SeekerBudgetTitle = new("What's your budget for this job?", "Magkano ang budget mo dito?");
    public static readonly LocalizedText SeekerBudgetWhy = new("Pros who fit your budget show up first.", "Uunahin namin ang mga pro na pasok sa budget mo.");

    // Labor-illusion loader. {0} = job or town label.
    public static readonly LocalizedText FindingService = new("Finding pros for {0}", "Hinahanap ang mga pro para sa {0}");
    public static readonly LocalizedText FindingCity = new("Checking who goes to {0}", "Tinitingnan kung sino ang pumupunta sa {0}");
    public static readonly LocalizedText FindingBudget = new("Putting pros in your budget first", "Inuuna ang pasok sa budget mo");

    // --- Search results ---------------------------------------------------
    public static readonly LocalizedText ResultsTitle = new("{0} pros for you", "{0} pro para sa 'yo");
    public static readonly LocalizedText ResultsTitleOne = new("1 pro for you", "1 pro para sa 'yo");
    public static readonly LocalizedText ResultsEditHint = new("Tap an answer to change it", "I-tap ang sagot para baguhin");
    public static readonly LocalizedText ResultsWidened = new("No pros in {0} yet, so here are pros from nearby towns.", "Wala pang pro sa {0}, kaya heto ang mga pro sa kalapit na bayan.");
    public static readonly LocalizedText ResultsEmpty = new("No pros for this job yet. We're adding more soon.", "Wala pang pro para dito. Magdadagdag pa kami.");
    public static readonly LocalizedText FitsBudget = new("Fits your budget", "Pasok sa budget mo");
    public static readonly LocalizedText Call = new("Call", "Tawagan");
    public static readonly LocalizedText Text = new("Text", "I-text");
    public static readonly LocalizedText ContactFailed = new("Couldn't open your phone app. Please try again.", "Hindi mabuksan ang phone app. Pakisubukan ulit.");

    // Pre-filled SMS to a pro, by urgency. {0} = job, {1} = town.
    public static readonly LocalizedText MessageToday = new(
        "Hi! I found you on SideGigPh. I need: {0}, in {1}, today. Are you available?",
        "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: {0}, dito sa {1}, ngayon. Available po ba kayo?");
    public static readonly LocalizedText MessageThisWeek = new(
        "Hi! I found you on SideGigPh. I need: {0}, in {1}, this week. Are you available?",
        "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: {0}, dito sa {1}, ngayong linggo. Available po ba kayo?");
    public static readonly LocalizedText MessageCanvass = new(
        "Hi! I found you on SideGigPh. I need: {0}, in {1}. How much do you charge?",
        "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: {0}, dito sa {1}. Magkano po ang singil ninyo?");

    // --- Pro card ---------------------------------------------------------
    public static readonly LocalizedText RatePerJob = new("{0} per job", "{0} kada trabaho");
    public static readonly LocalizedText RateQuote = new("Price depends on the job", "Depende sa trabaho ang presyo");
    public static readonly LocalizedText ExperienceFact = new("Experience: {0}", "Experience: {0}");
    public static readonly LocalizedText ServesFact = new("Goes to {0}", "Pumupunta sa {0}");

    // --- Professional interview -------------------------------------------
    public static readonly LocalizedText ProSkillsTitle = new("What work do you do?", "Ano'ng trabaho mo?");
    public static readonly LocalizedText ProSkillsWhy = new("Pick all that apply.", "Piliin lahat ng ginagawa mo.");
    public static readonly LocalizedText ProServicesTitle = new("Which jobs do you take?", "Anong mga trabaho ang tinatanggap mo?");
    public static readonly LocalizedText ProServicesWhy = new("Customers search for these exact jobs. Pick all that apply.", "Ito mismo ang hinahanap ng customer. Piliin lahat.");
    public static readonly LocalizedText ProAreasTitle = new("Which towns do you go to?", "Saang mga bayan ka pumupunta?");
    public static readonly LocalizedText ProAreasWhy = new("Calls will only come from these towns.", "Dito lang manggagaling ang mga tatawag sa 'yo.");
    public static readonly LocalizedText ProExperienceTitle = new("How long have you done this work?", "Gaano ka na katagal sa trabahong ito?");
    public static readonly LocalizedText ProExperienceWhy = new("Customers look at this first.", "Ito ang unang tinitingnan ng customer.");
    public static readonly LocalizedText ProRateTitle = new("How much do you usually charge per job?", "Magkano ang singil mo kada trabaho?");
    public static readonly LocalizedText ProRateWhy = new("Your starting price. You can still agree on the final price with the customer.", "Panimulang presyo lang. Puwede pa ring pag-usapan ninyo ng customer.");
    public static readonly LocalizedText ProGoalTitle = new("How much do you want to earn each month?", "Magkano ang gusto mong kitain kada buwan?");
    public static readonly LocalizedText ProGoalWhy = new("Just for you. This won't show on your listing.", "Para sa 'yo lang. Hindi ito makikita sa listing mo.");

    // Goal payoff. {0} = jobs per week, {1} = typical price per job.
    public static readonly LocalizedText GoalPayoff = new(
        "That's about {0} jobs a week at around {1} each. Let's get you there!",
        "Mga {0} na trabaho kada linggo 'yan, sa mga {1} bawat isa. Kaya natin 'yan!");
    public static readonly LocalizedText GoalPayoffOne = new(
        "That's about 1 job a week at around {0}. Let's get you there!",
        "Mga 1 trabaho kada linggo 'yan, sa mga {0}. Kaya natin 'yan!");
    public static readonly LocalizedText GoalPayoffQuote = new(
        "Let's get you there! Customers will ask you for a quote.",
        "Kaya natin 'yan! Magpapa-quote sa 'yo ang mga customer.");

    public static readonly LocalizedText ProNameTitle = new("What should customers call you?", "Ano'ng itatawag sa 'yo ng customer?");
    public static readonly LocalizedText ProNameWhy = new("Your name or your shop's name.", "Pangalan mo o pangalan ng shop mo.");
    public static readonly LocalizedText ProNamePlaceholder = new("e.g. Mang Jun Aircon Services", "hal. Mang Jun Aircon Services");
    public static readonly LocalizedText ProPhoneTitle = new("What number can customers call?", "Saang number ka puwedeng tawagan?");
    public static readonly LocalizedText ProPhoneWhy = new("Customers will call or text you here.", "Dito tatawag o magte-text ang customer.");
    public static readonly LocalizedText ProPhoneInvalid = new("Enter your 11-digit mobile number, like 0917 123 4567.", "Ilagay ang 11-digit na mobile number mo, gaya ng 0917 123 4567.");
    public static readonly LocalizedText ProPreviewTitle = new("This is how customers will see you", "Ganito ka makikita ng mga customer");
    public static readonly LocalizedText ProPreviewWhy = new("Your listing, built from your answers.", "Ang listing mo, galing sa mga sagot mo.");
    public static readonly LocalizedText ProPreviewConfirm = new("Looks good!", "Ayos na 'to!");

    // --- Professional home ------------------------------------------------
    public static readonly LocalizedText ProHomeTitle = new("Your listing", "Ang listing mo");
    public static readonly LocalizedText ProHomeNotLive = new("Saved on this phone. Not visible to customers yet.", "Naka-save sa phone na ito. Hindi pa nakikita ng customer.");
    public static readonly LocalizedText ProHomeGoal = new("Your goal: {0} a month", "Goal mo: {0} kada buwan");
    public static readonly LocalizedText ProHomeEdit = new("Edit listing", "I-edit ang listing");
}
