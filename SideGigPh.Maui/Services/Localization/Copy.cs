namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// Every SideGigPh UI string, English and Taglish side by side (the skeleton's shared strings,
/// e.g. Back, Next, Start over, are in MobileAppTheme's <see cref="CommonCopy"/>). Many of our users are not confident
/// English readers: keep both versions short and plain, and keep Taglish conversational.
/// Reference-data labels (jobs, towns, prices) live in <see cref="ReferenceData"/>.
/// </summary>
public static class Copy
{
    // --- Role fork --------------------------------------------------------
    public static readonly LocalizedText RoleTitle = new("Find a Job or Hire a Skilled Worker", "Find a Job or Hire a Skilled Worker");
    public static readonly LocalizedText RoleSeeker = new("I need someone for a job", "May ipapagawa ako");
    public static readonly LocalizedText RoleSeekerHint = new("Aircon, plumbing, repairs, errands, massage", "Aircon, tubero, repair, PasaBuy, masahe");
    public static readonly LocalizedText RoleProfessional = new("I want to earn money", "Gusto kong kumita sa trabaho");
    public static readonly LocalizedText RoleProfessionalHint = new("Get customers near you", "Maka kuha ng trabaho malapit sayo");

    // --- Seeker interview -------------------------------------------------
    public static readonly LocalizedText SeekerCategoryTitle = new("What do you need done?", "Ano'ng ipapagawa mo?");
    public static readonly LocalizedText SeekerServiceTitle = new("What kind of job?", "Anong klaseng trabaho?");
    public static readonly LocalizedText SeekerServiceWhy = new("So you only see Skilled Workers who do exactly this.", "So you only see Skilled Workers who do exactly this.");
    public static readonly LocalizedText SeekerCityTitle = new("Where's the Job?", "Saan ang trabaho?");
    public static readonly LocalizedText SeekerCityWhy = new("Get Professionals near you.", "Get Professionals near you.");
    public static readonly LocalizedText CitiesNote = new("More towns coming soon.", "Marami pang bayan, abangan!");
    public static readonly LocalizedText SeekerWhenTitle = new("When do you need it?", "Kailan mo kailangan?");
    public static readonly LocalizedText SeekerWhenWhy = new("We'll tell the pro when you need it.", "Sasabihin namin sa pro kung kailan mo kailangan.");
    public static readonly LocalizedText SeekerBudgetTitle = new("What's your budget for this job?", "Magkano ang budget mo dito?");
    public static readonly LocalizedText SeekerBudgetWhy = new("Pros who fit your budget show up first.", "Uunahin namin ang mga pro na pasok sa budget mo.");

    // Labor-illusion loader. {0} = job or town label.
    public static readonly LocalizedText FindingService = new("Finding Skilled Worker for {0}", "Finding Skilled Worker for {0}");
    public static readonly LocalizedText FindingCity = new("Checking who goes to {0}", "Checking who goes to {0}");
    public static readonly LocalizedText FindingBudget = new("Putting Skilled Workers in your budget first", "Putting Skilled Workers in your budget first");

    // --- Seeker shell + home ----------------------------------------------
    public static readonly LocalizedText LastSearch = new("Recent Search", "Recent Search");
    public static readonly LocalizedText SeePros = new("View more", "View more");
    public static readonly LocalizedText SectionMessages = new("Recent messages", "Recent messages");

    // --- Search -----------------------------------------------------------
    public static readonly LocalizedText SearchTitle = new("Find a Skilled Worker", "Maghanap ng Skilled Worker");
    public static readonly LocalizedText SearchPlaceholder = new("Search for Skilled Worker", "Maghanap ng Skilled Worker");
    public static readonly LocalizedText SearchNoMatch = new("We couldn't tell what job \"{0}\" is. Pick one below.", "Hindi namin makuha kung anong trabaho ang \"{0}\". Pumili sa baba.");
    public static readonly LocalizedText SearchPickTrade = new("Which kind of skilled worker is \"{0}\" for? Pick one below.", "Which kind of skilled worker is \"{0}\"? Pumili sa baba.");
    public static readonly LocalizedText SearchTradesLabel =new("Kind of skilled worker", "Klase ng skilled worker");
    public static readonly LocalizedText SearchJobsLabel = new("Job", "Trabaho");
    public static readonly LocalizedText SearchAllJobs = new("All {0} jobs", "Lahat ng {0}");
    public static readonly LocalizedText FilterTown = new("Town", "Bayan");
    public static readonly LocalizedText FilterBudget = new("Budget", "Budget");
    public static readonly LocalizedText BrowseTitle = new("Browse by service", "Pumili ng serbisyo");

    // --- Search results ---------------------------------------------------
    public static readonly LocalizedText ResultsTitle = new("{0} for you", "{0} skilled worker para sa 'yo");
    public static readonly LocalizedText ResultsTitleOne = new("1 skilled worker for you", "1 skilled worker para sa 'yo");
    public static readonly LocalizedText ResultsInTown = new("Goes to {0}", "Pumupunta sa {0}");
    public static readonly LocalizedText ResultsOtherTowns = new("Other towns nearby", "Taga-ibang bayan na malapit");
    public static readonly LocalizedText ResultsWidened = new("No skilled workers in {0} yet, so here are skilled workers from nearby towns.", "Wala pang skilled worker sa {0}, kaya heto ang mga skilled worker sa kalapit na bayan.");
    public static readonly LocalizedText ResultsServiceFallback = new("No skilled workers list \"{0}\" yet, so here are other {1} skilled workers.", "Wala pang skilled worker na may \"{0}\", kaya heto ang ibang {1}.");
    public static readonly LocalizedText ResultsEmpty = new("No skilled workers for this job yet. We're adding more soon.", "Wala pang skilled worker para dito. Magdadagdag pa kami.");
    public static readonly LocalizedText FitsBudget = new("Fits your budget", "Pasok sa budget mo");

    // --- A pro's public profile -------------------------------------------
    public static readonly LocalizedText ProProfileJobs = new("Jobs they take", "Mga tinatanggap na trabaho");
    public static readonly LocalizedText ProProfilePhotos = new("Photos of their work", "Photos ng mga gawa nila");
    public static readonly LocalizedText ProProfileNoReviews = new("No reviews yet.", "Wala pang review.");
    public static readonly LocalizedText ProNotFoundTitle = new("Skilled worker not found", "Skilled worker not found");
    public static readonly LocalizedText ProNotFound = new("This listing isn't available anymore.", "Wala na ang listing na ito.");
    public static readonly LocalizedText ContactPro = new("Contact", "Contact");

    // --- Seeker inbox -----------------------------------------------------
    public static readonly LocalizedText InboxTitle = new("Inbox", "Inbox");
    public static readonly LocalizedText InboxEmpty = new("No messages yet. Find a pro and tap Contact.", "Wala pang message. Maghanap ng pro at i-tap ang I-contact.");
    public static readonly LocalizedText InboxYouWrote = new("You: {0}", "Ikaw: {0}");
    public static readonly LocalizedText YouCalled = new("You called", "Tumawag ka");
    public static readonly LocalizedText SentBySms = new("Text · {0}", "Text · {0}");
    public static readonly LocalizedText ThreadHowItWorks = new(
        "Your text opens in your phone's SMS app. {0} replies to your phone by text or call.",
        "Bubukas ito sa SMS app mo. Sa phone mo sasagot si {0}, sa text o tawag.");
    public static readonly LocalizedText ThreadStart = new(
        "We wrote a message for you. Change it if you like, then tap Send text.",
        "May nakahanda nang message. Puwede mo itong baguhin, tapos i-tap ang I-text.");
    public static readonly LocalizedText YourMessage = new("Your message", "Message mo");
    public static readonly LocalizedText SendText = new("Send text", "I-text");
    public static readonly LocalizedText Call = new("Call", "Tawagan");
    public static readonly LocalizedText ViewProfile = new("View profile", "Tingnan ang profile");
    public static readonly LocalizedText RateThisPro = new("Rate this worker", "I-rate ang worker na ito");
    public static readonly LocalizedText EditYourReview = new("Edit your review", "Baguhin ang review mo");
    public static readonly LocalizedText ContactFailed = new("Couldn't open your phone app. Please try again.", "Hindi mabuksan ang phone app mo. Subukan ulit.");

    // --- Seeker reviews ---------------------------------------------------
    public static readonly LocalizedText ToRateTitle = new("Rate skilled worker you contacted", "I-rate ang mga na-contact mo");
    public static readonly LocalizedText ToRateWhy = new("Your stars help neighbors pick the right skilled worker.", "Makakatulong ang rating mo sa mga kapitbahay mo.");
    public static readonly LocalizedText Rate = new("Rate", "I-rate");
    public static readonly LocalizedText YourReviews = new("Your reviews", "Mga review mo");
    public static readonly LocalizedText ToRateCount = new("{0} to rate", "{0} pa ang ire-rate mo");
    public static readonly LocalizedText SeekerReviewsEmpty = new(
        "No reviews yet. Contact a skilled worker, and after the job, rate them here.",
        "Wala pang review. Mag-contact ng skilled worker, tapos i-rate sila dito pagkatapos ng trabaho.");
    public static readonly LocalizedText WriteReviewTitle = new("How was {0}?", "Kumusta si {0}?");
    public static readonly LocalizedText StarsLabel = new("Stars", "Stars");
    public static readonly LocalizedText ReviewCommentPlaceholder = new("What went well? (optional)", "Ano'ng nagustuhan mo? (optional)");
    public static readonly LocalizedText ReviewNameLabel = new("Your name on reviews", "Pangalan mo sa review");
    public static readonly LocalizedText ReviewNamePlaceholder = new("e.g. Maricel D.", "hal. Maricel D.");
    public static readonly LocalizedText SubmitReview = new("Post review", "I-post");

    // --- Me page (both sides) ---------------------------------------------
    public static readonly LocalizedText NavMe = new("Me", "Ako");
    public static readonly LocalizedText AccountTitle = new("Your account", "Account mo");
    public static readonly LocalizedText StartOverWarning = new(
        "Your answers, listing, photos, messages and reviews on this phone will be deleted. You can't undo this.",
        "Mabubura ang mga sagot, listing, photos, message at review mo sa phone na ito. Hindi na ito maibabalik.");
    public static readonly LocalizedText Save = new("Save", "I-save");
    public static readonly LocalizedText Saved = new("Saved", "Na-save");

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
    public static readonly LocalizedText RatePerJob = new("{0} per job", "{0} per job");
    public static readonly LocalizedText RateQuote = new("Price depends on the job", "Depende sa trabaho ang presyo");
    public static readonly LocalizedText ExperienceFact = new("Exp: {0}", "Exp: {0}");
    public static readonly LocalizedText ServesFact = new("Location : {0}", "Location : {0}");

    // --- Professional interview -------------------------------------------
    public static readonly LocalizedText ProSkillsTitle = new("What work do you do?", "Ano'ng trabaho mo?");
    public static readonly LocalizedText ProSkillsWhy = new("Pick all that apply.", "Piliin kung san ka pinaka magaling.");
    public static readonly LocalizedText ProServicesTitle = new("Which jobs do you take?", "Anong mga trabaho ang tinatanggap mo?");
    public static readonly LocalizedText ProServicesWhy = new("Customers search for these exact jobs. Pick all that apply.", "Ito mismo ang hinahanap ng customer. Piliin lahat.");
    public static readonly LocalizedText ProAreasTitle = new("Which towns do you go to?", "Saang mga bayan ka pumupunta?");
    public static readonly LocalizedText ProAreasWhy = new("Calls will only come from these towns.", "Dito ka lang makakatanggap ng mga trabaho.");
    public static readonly LocalizedText ProExperienceTitle = new("How long have you done this work?", "Gano na katagal ang experience mo?");
    public static readonly LocalizedText ProExperienceWhy = new("Customers look at this first.", "Ito ang hinahanap ng mga customer.");
    public static readonly LocalizedText ProRateTitle = new("How much do you usually charge per job?", "Magkano ang singil mo kada trabaho?");
    public static readonly LocalizedText ProRateWhy = new("Your starting price. You can still agree on the final price with the customer.", "Pwede parin pag usapan ninyo ang presyo ng customer.");
    public static readonly LocalizedText ProGoalTitle = new("How much do you want to earn each month?", "Magkano ang gusto mong kitain kada buwan?");
    public static readonly LocalizedText ProGoalWhy = new("Just for you. This won't show on your listing.", "Hindi ito makikita sa listing mo.");

    // Goal payoff. {0} = jobs per week, {1} = typical price per job.
    public static readonly LocalizedText GoalPayoff = new(
        "That's about {0} jobs a week at around {1} each. Let's get you there!",
        "Mga {0} na trabaho kada linggo 'yan, sa mga {1} bawat isa.");
    public static readonly LocalizedText GoalPayoffOne = new(
        "That's about 1 job a week at around {0}. Let's get you there!",
        "Mga 1 trabaho kada linggo 'yan, sa mga {0}.");
    public static readonly LocalizedText GoalPayoffQuote = new(
        "Let's get you there! Customers will ask you for a quote.",
        "Kaya natin 'yan! Magpapa-quote sa 'yo ang mga customer.");

    public static readonly LocalizedText ProNameTitle = new("What should customers call you?", "Ilagay ang pangalan.");
    public static readonly LocalizedText ProNameWhy = new("Your name or your shop's name.", "Pangalan mo o pangalan ng shop mo.");
    public static readonly LocalizedText ProNamePlaceholder = new("e.g. Mang Jun Aircon Services", "hal. Mang Jun Aircon Services");
    public static readonly LocalizedText ProPhoneTitle = new("What number can customers call?", "Saang number ka puwedeng tawagan?");
    public static readonly LocalizedText ProPhoneWhy = new("Customers will call or text you here.", "Dito tatawag o magte-text ang customer.");
    public static readonly LocalizedText ProPhoneInvalid = new("Enter your 11-digit mobile number, like 0917 123 4567.", "Ilagay ang 11-digit na mobile number mo, gaya ng 0917 123 4567.");
    public static readonly LocalizedText ProPreviewTitle = new("This is how customers will see you", "Ganito ka makikita ng mga customer");
    public static readonly LocalizedText ProPreviewWhy = new("Your listing, built from your answers.", "Ang listing mo, galing sa mga sagot mo.");
    public static readonly LocalizedText ProPreviewConfirm = new("DONE", "DONE");

    // --- Pro home shell (top bar + bottom tabs) ----------------------------
    public static readonly LocalizedText NavHome = new("Home", "Home");
    public static readonly LocalizedText NavInbox = new("Inbox", "Inbox");
    public static readonly LocalizedText NavReviews = new("Reviews", "Reviews");

    // --- Pro home ---------------------------------------------------------
    public static readonly LocalizedText ProHomeNotLive = new("Saved on this phone. Not visible to customers yet.", "Saved on this phone. Hindi pa ito nakikita ng customer.");
    public static readonly LocalizedText ProHomeGoal = new("Your goal: {0} a month", "Goal mo: {0} kada buwan");
    public static readonly LocalizedText StatusPublished = new("Published", "Naka-publish na");
    public static readonly LocalizedText StatusNotPublished = new("Not published yet", "Hindi pa naka-publish");
    public static readonly LocalizedText StatRating = new("{0} ★ ({1})", "{0} ★ ({1})");
    public static readonly LocalizedText SectionInquiries = new("Recent inquiries", "Mga bagong nagtanong");
    public static readonly LocalizedText SectionReviews = new("Latest reviews", "Mga bagong review");
    public static readonly LocalizedText SectionGoal = new("Your goal", "Goal mo");

    // Setup checklist on Pro home, until the listing is published.
    public static readonly LocalizedText SetupTitle = new("Get ready for customers", "Ihanda ang listing mo");
    public static readonly LocalizedText SetupPhoto = new("Add a photo of you", "Maglagay ng photo mo");
    public static readonly LocalizedText SetupPhotoWhy = new("Customers trust a face.", "Mas nagtitiwala ang customer kapag may mukha.");
    public static readonly LocalizedText SetupWork = new("Add photos of your work", "Maglagay ng photos ng gawa mo");
    public static readonly LocalizedText SetupWorkWhy = new("Show your best jobs.", "Ipakita ang pinakamagandang gawa mo.");
    public static readonly LocalizedText SetupPublish = new("Publish your listing", "I-publish ang listing mo");
    public static readonly LocalizedText SetupPublishWhy = new("So customers can find and call you.", "Para makita at matawagan ka ng customer.");

    // --- Inquiries --------------------------------------------------------
    public static readonly LocalizedText InquiriesTitle = new("Inquiries", "Mga nagtanong");
    public static readonly LocalizedText InquiriesWhy = new("Customers who tapped Call or Text on your listing. The call or text goes straight to your phone.", "Mga customer na nag-tap ng Tawagan o I-text sa listing mo. Diretso sa phone mo ang tawag o text.");
    public static readonly LocalizedText InquiriesEmpty = new("No inquiries yet. They show up here when a customer taps Call or Text.", "Wala pang nagtanong. Lalabas dito kapag may customer na nag-tap ng Tawagan o I-text.");
    public static readonly LocalizedText InquiryCalled = new("Tried to call you", "Sinubukan kang tawagan");
    public static readonly LocalizedText InquiryTexted = new("Tried to text you", "Sinubukan kang i-text");
    public static readonly LocalizedText InquiryBudget = new("Budget: {0}", "Budget: {0}");
    public static readonly LocalizedText New = new("New", "Bago");

    // --- Reviews ----------------------------------------------------------
    public static readonly LocalizedText ReviewsTitle = new("Reviews", "Reviews");
    public static readonly LocalizedText ReviewsCount = new("{0} reviews", "{0} reviews");
    public static readonly LocalizedText ReviewsCountOne = new("1 review", "1 review");
    public static readonly LocalizedText ReviewsEmpty = new("No reviews yet. After a job, ask your customer to rate you on SideGigPh.", "Wala pang review. Pagkatapos ng trabaho, sabihan ang customer na i-rate ka sa SideGigPh.");

    // --- Photos -----------------------------------------------------------
    public static readonly LocalizedText PhotosTitle = new("Photos of your work", "Photos ng gawa mo");
    public static readonly LocalizedText PhotosCount = new("{0} of {1} photos", "{0} sa {1} photos");
    public static readonly LocalizedText PhotosWhy = new("Customers see these on your listing. Up to {0} photos.", "Makikita ito ng customer sa listing mo. Hanggang {0} photos.");
    public static readonly LocalizedText PhotosEmpty = new("No photos yet. Customers like to see your work before they call.", "Wala pang photos. Gusto ng customer makita muna ang gawa mo bago tumawag.");
    public static readonly LocalizedText PhotosAdd = new("Add photos", "Mag-add ng photos");
    public static readonly LocalizedText PhotosFull = new("You've reached {0} photos. Remove one to add another.", "Umabot ka na sa {0} photos. Magtanggal muna bago mag-add.");
    public static readonly LocalizedText PhotoRemove = new("Remove photo", "Tanggalin ang photo");
    public static readonly LocalizedText PhotosDuplicate = new("Skipped photos you already added.", "Hindi na isinama ang photos na nandito na.");
    public static readonly LocalizedText PhotosFailed = new("Couldn't open your photos. Please try again.", "Hindi mabuksan ang photos mo. Subukan ulit.");

    // --- Profile ----------------------------------------------------------
    public static readonly LocalizedText ProfileTitle = new("Your profile", "Profile");
    public static readonly LocalizedText ProfilePhotoAdd = new("Add your photo", "Maglagay ng photo mo");
    public static readonly LocalizedText ProfilePhotoChange = new("Change photo", "Palitan ang photo");
    public static readonly LocalizedText ProfileEdit = new("Edit listing", "Edit listing");
    public static readonly LocalizedText ProfileSince = new("On SideGigPh since {0}", "Nasa SideGigPh mula {0}");

    // --- Publish ----------------------------------------------------------
    public static readonly LocalizedText PublishTitle = new("Ready to publish?", "Ready na bang i-publish?");
    public static readonly LocalizedText PublishWhy = new("Customers in your towns will see this and can call or text you.", "Makikita ito ng customer sa mga bayan mo at puwede ka nilang tawagan o i-text.");
    public static readonly LocalizedText PublishPledge = new("I'll answer customers and do honest work at the price I posted.", "Sasagutin ko ang customer at tapat akong gagawa sa presyong nilagay ko.");
    public static readonly LocalizedText PublishConfirm = new("Publish", "Publish");
    public static readonly LocalizedText PublishPhoneTaken = new("This number already has a listing. One number, one listing.", "May listing na ang number na ito. Isang number, isang listing lang.");
}
