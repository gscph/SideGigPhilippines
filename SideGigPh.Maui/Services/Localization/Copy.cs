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
    public static readonly LocalizedText RoleSeekerHint = new("Aircon, plumbing, repairs, errands, mechanic", "Aircon, tubero, repair, PasaBuy, mekaniko");
    public static readonly LocalizedText RoleProfessional = new("I want to earn money", "Gusto kong kumita sa trabaho");
    public static readonly LocalizedText RoleProfessionalHint = new("Get customers near you", "Maka kuha ng trabaho malapit sayo");

    // --- Seeker interview -------------------------------------------------
    public static readonly LocalizedText SeekerCategoryTitle = new("What do you need done?", "Ano'ng ipapagawa mo?");
    public static readonly LocalizedText SeekerServiceTitle = new("What kind of job?", "Anong klaseng trabaho?");
    public static readonly LocalizedText SeekerServiceWhy = new("So you only see Skilled Workers who do exactly this.", "So you only see Skilled Workers who do exactly this.");
    public static readonly LocalizedText OtherJobSeekerLabel = new("What job do you need?", "Anong trabaho ang kailangan mo?");
    public static readonly LocalizedText OtherJobProLabel = new("What other job do you do?", "Anong iba pang trabaho ang kaya mo?");
    public static readonly LocalizedText OtherJobHelp = new("Optional. It helps us add more jobs.", "Optional. Para madagdagan namin ang listahan.");
    public static readonly LocalizedText SeekerCityTitle = new("Where's the Job?", "Saan ang trabaho?");
    public static readonly LocalizedText SeekerCityWhy = new("See skilled workers near you.", "Makikita mo ang mga skilled worker na malapit sa iyo.");
    public static readonly LocalizedText CitiesNote = new("More towns coming soon.", "Marami pang bayan, abangan!");
    public static readonly LocalizedText TownsAll = new("Select all", "Piliin lahat");
    public static readonly LocalizedText TownsClear = new("Remove all", "Alisin lahat");
    public static readonly LocalizedText SeekerWhenTitle = new("When do you need it?", "Kailan mo kailangan?");
    public static readonly LocalizedText SeekerWhenWhy = new("We'll tell the skilled worker when you need it.", "Sasabihin namin sa skilled worker kung kailan mo kailangan.");
    public static readonly LocalizedText SeekerBudgetTitle = new("What's your budget for this job?", "Magkano ang budget mo dito?");
    public static readonly LocalizedText SeekerBudgetWhy = new("Skilled workers who fit your budget show up first.", "Uunahin namin ang mga skilled worker na pasok sa budget mo.");

    // Budget. {0} = a peso amount or range, {1} = the job.
    public static readonly LocalizedText BudgetTypical = new("Most people pay {0} for {1}.", "Karaniwang bayad sa {1}: {0}.");
    public static readonly LocalizedText BudgetLow = new("Budget-friendly", "Tipid");
    public static readonly LocalizedText BudgetMid = new("Average", "Karaniwan");
    public static readonly LocalizedText BudgetHigh = new("For bigger or harder jobs", "Para sa mas malaki o mahirap na trabaho");
    public static readonly LocalizedText BudgetCustom = new("Other amount", "Ibang halaga");
    public static readonly LocalizedText BudgetCustomHint = new("Type your own range", "I-type ang sarili mong budget");
    public static readonly LocalizedText BudgetRange = new("{0} – {1}", "{0} – {1}");
    public static readonly LocalizedText BudgetUpTo = new("Up to {0}", "Hanggang {0}");
    public static readonly LocalizedText BudgetAndUp = new("{0} and up", "{0} pataas");
    public static readonly LocalizedText BudgetFromLabel = new("From (₱)", "Mula (₱)");
    public static readonly LocalizedText BudgetToLabel = new("Up to (₱)", "Hanggang (₱)");
    public static readonly LocalizedText BudgetFromHelp = new("At least {0}, the minimum wage", "Hindi bababa sa {0}, ang minimum wage");
    public static readonly LocalizedText BudgetToError = new("Make it the same or more than the first amount", "Dapat pareho o mas mataas sa unang halaga");

    // "Others" after the trades: kinds of work we don't offer yet (sent for analytics).
    public static readonly LocalizedText OtherTradeSeekerLabel = new("What work do you need?", "Anong klaseng trabaho ang kailangan mo?");
    public static readonly LocalizedText OtherTradeProLabel = new("What kind of work do you do?", "Anong klaseng trabaho mo?");
    public static readonly LocalizedText OtherTradeHelp = new("We use this to add new kinds of work.", "Gagamitin namin ito para magdagdag ng bagong trabaho.");
    public static readonly LocalizedText OtherTradeThanksTitle = new("Thanks! We noted it.", "Salamat! Na-note namin.");
    public static readonly LocalizedText OtherTradeSeekerThanks = new(
        "We don't have skilled workers for this yet. It helps us choose what to add next.",
        "Wala pa kaming skilled worker para dito. Makakatulong ito sa pagpili namin ng susunod na idadagdag.");
    public static readonly LocalizedText OtherTradeProThanks = new(
        "We don't list this kind of work yet. It helps us choose what to add next. For now, you can pick the closest one.",
        "Hindi pa namin nililista ang ganitong trabaho. Makakatulong ito sa pagpili namin ng idadagdag. Pwede ka munang pumili ng pinakamalapit.");
    public static readonly LocalizedText OtherTradePickAnother = new("Pick another", "Pumili ng iba");

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
    public static readonly LocalizedText ResultsEmpty = new("No skilled workers for this job yet. We're adding more soon.", "Wala pang skilled worker para dito. Magdadagdag pa kami.");
    public static readonly LocalizedText FitsBudget = new("Fits your budget", "Pasok sa budget mo");

    // --- A pro's public profile -------------------------------------------
    public static readonly LocalizedText ProProfileJobs = new("Jobs they take", "Mga tinatanggap na trabaho");
    public static readonly LocalizedText ProProfileGoesTo = new("Goes to {0}", "Pumupunta sa {0}");
    public static readonly LocalizedText StatNew = new("New", "Bago");
    public static readonly LocalizedText StatNoReviews = new("no reviews yet", "wala pang review");
    public static readonly LocalizedText StatOneReview = new("1 review", "1 review");
    public static readonly LocalizedText StatReviews = new("{0} reviews", "{0} reviews");
    public static readonly LocalizedText StatExperience = new("experience", "experience");
    public static readonly LocalizedText StatPerJob = new("per job", "per job");
    public static readonly LocalizedText StatAskPrice = new("Ask", "Itanong");
    public static readonly LocalizedText StatPriceDepends = new("price depends on the job", "depende sa trabaho");
    public static readonly LocalizedText ProProfileTrust = new("Why you can trust them", "Bakit sila maaasahan");
    public static readonly LocalizedText TrustPhone = new("Phone number verified", "Verified ang phone number");
    public static readonly LocalizedText TrustPhoneNote = new("Checked with an SMS code.", "Na-check gamit ang SMS code.");
    public static readonly LocalizedText TrustOneReference = new("1 reference", "May 1 reference");
    public static readonly LocalizedText TrustReferences = new("{0} references", "May {0} references");
    public static readonly LocalizedText TrustReferencesNote = new(
        "People who can vouch for their work. Ask about them in chat.",
        "Mga taong pwedeng magpatunay sa gawa nila. Itanong sa chat.");
    public static readonly LocalizedText ProProfilePhotos = new("Photos of their work", "Photos ng mga gawa nila");
    public static readonly LocalizedText ProProfileNoReviews = new("No reviews yet.", "Wala pang review.");
    public static readonly LocalizedText ProNotFoundTitle = new("Skilled worker not found", "Skilled worker not found");
    public static readonly LocalizedText ProNotFound = new("This listing isn't available anymore.", "Wala na ang listing na ito.");
    public static readonly LocalizedText ContactPro = new("Contact", "Contact");

    // --- Inbox and chat (both sides) -------------------------------------
    public static readonly LocalizedText InboxTitle = new("Inbox", "Inbox");
    public static readonly LocalizedText InboxEmpty = new("No messages yet. Find a skilled worker and tap Contact.", "Wala pang message. Maghanap ng skilled worker at i-tap ang I-contact.");
    public static readonly LocalizedText InboxYouWrote = new("You: {0}", "Ikaw: {0}");
    public static readonly LocalizedText YouCalled = new("You called", "Tumawag ka");
    public static readonly LocalizedText CalledYou = new("Called you", "Tumawag sa'yo");
    public static readonly LocalizedText ThreadHowItWorks = new(
        "{0} gets your message here in the app and replies here. You can also call.",
        "Makikita ni {0} ang message mo dito sa app, at dito rin siya sasagot. Puwede ka ring tumawag.");
    public static readonly LocalizedText ThreadStart = new(
        "We wrote a message for you. Change it if you like, then tap Send.",
        "May nakahanda nang message. Puwede mo itong baguhin, tapos i-tap ang I-send.");
    public static readonly LocalizedText YourMessage = new("Your message", "Message mo");
    public static readonly LocalizedText Send = new("Send", "I-send");
    public static readonly LocalizedText SendFailed = new("Your message didn't send. Check your internet and try again.", "Hindi na-send ang message mo. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText ChatCustomer = new("Customer", "Customer");
    public static readonly LocalizedText Call = new("Call", "Tawagan");
    public static readonly LocalizedText ViewProfile = new("View profile", "Tingnan ang profile");
    public static readonly LocalizedText RateThisPro = new("Rate this skilled worker", "I-rate ang skilled worker na ito");
    public static readonly LocalizedText RateLater = new(
        "You can rate {0} 24 hours after your first message.",
        "Puwede mo nang i-rate si {0} 24 oras pagkatapos ng una mong message.");
    public static readonly LocalizedText EditYourReview =new("Edit your review", "Baguhin ang review mo");
    public static readonly LocalizedText ContactFailed = new("Couldn't open your phone app. Please try again.", "Hindi mabuksan ang phone app mo. Subukan ulit.");

    // --- Seeker reviews ---------------------------------------------------
    public static readonly LocalizedText ToRateTitle = new("Rate skilled workers you contacted", "I-rate ang mga na-contact mo");
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

    // --- Notification channels (Android Settings › Notifications; made at launch, before the language is known, so English) ---
    public static readonly LocalizedText PushChannelCustomers = new("New customers", "Bagong customer");
    public static readonly LocalizedText PushChannelReplies = new("Replies from skilled workers", "Sagot ng skilled worker");
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
    public static readonly LocalizedText ProAreasWhy = new("Customers in these towns see you first.", "Ikaw ang unang makikita ng customer sa mga bayang ito.");
    public static readonly LocalizedText ProExperienceTitle = new("How long have you done this work?", "Gano na katagal ang experience mo?");
    public static readonly LocalizedText ProExperienceWhy = new("Customers look at this first.", "Ito ang hinahanap ng mga customer.");
    public static readonly LocalizedText ProRateTitle = new("How much do you usually charge per job?", "Magkano ang singil mo kada trabaho?");
    public static readonly LocalizedText ProRateWhy = new(
        "Each job has its own price. Customers see your price for the job they need, and you can still agree on the final price.",
        "Iba-iba ang presyo kada trabaho. Makikita ng customer ang presyo mo sa trabahong kailangan nila. Pwede pa rin ninyong pag-usapan.");
    public static readonly LocalizedText ProRateNote = new(
        "The minimum is never below the NCR minimum wage ({0} a day).",
        "Hindi bababa sa minimum wage sa NCR ({0} kada araw) ang pinakamababa.");
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

    public static readonly LocalizedText ProPreviewTitle = new("This is how customers will see you", "Ganito ka makikita ng mga customer");
    public static readonly LocalizedText ProPreviewWhy = new("Your listing, built from your answers.", "Ang listing mo, galing sa mga sagot mo.");
    public static readonly LocalizedText ProPreviewConfirm = new("DONE", "DONE");

    // Loader after the preview's Done, while the answers save and Pro home loads.
    public static readonly LocalizedText SettingUpListing = new("Saving your listing", "Sine-save ang listing mo");
    public static readonly LocalizedText SettingUpHome = new("Getting your home page ready", "Inihahanda ang home page mo");
    public static readonly LocalizedText SettingUpChecklist = new("Next: photos and a reference", "Next: photos at reference");

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
    public static readonly LocalizedText SectionReviews = new("Latest reviews", "Mga bagong review");
    public static readonly LocalizedText SectionGoal = new("Your goal", "Goal mo");

    // Setup checklist on Pro home: photo, 2 work photos, a reference, a confirmed number, then the Publish button.
    public static readonly LocalizedText NotLiveBanner = new("Not visible yet · Finish your listing", "Di ka pa nakikita · Tapusin ang listing");
    public static readonly LocalizedText SetupTitle =new("Get ready for customers", "Ihanda ang listing mo");
    public static readonly LocalizedText SetupPhoto = new("Add a photo of you", "Maglagay ng photo mo");
    public static readonly LocalizedText SetupPhotoWhy = new("Customers trust a face.", "Mas nagtitiwala ang customer kapag may mukha.");
    public static readonly LocalizedText SetupWork = new("Add at least 2 photos of your work", "Maglagay ng 2 o higit pang photos ng gawa mo");
    public static readonly LocalizedText SetupWorkWhy = new("Show your best jobs. {0} of 2 added.", "Ipakita ang pinakamagandang gawa mo. {0} sa 2 na.");
    public static readonly LocalizedText SetupReference = new("Add a reference", "Maglagay ng reference");
    public static readonly LocalizedText SetupReferenceWhy = new("Someone you worked for who can vouch for you.", "Isang pinagtrabahuhan mo na magsasabing maayos ka.");
    public static readonly LocalizedText SetupPhone = new("Confirm your number", "I-confirm ang number mo");
    public static readonly LocalizedText SetupPhoneWhy = new("We text you a code. Customers call this number.", "Ite-text ka namin ng code. Ito ang tatawagan ng customer.");
    public static readonly LocalizedText SetupPhoneDone = new("Confirmed with a text code.", "Na-confirm na gamit ang code sa text.");
    public static readonly LocalizedText SetupPublishReady = new("All set! Publish your listing to start finding work.", "Ready ka na! I-publish ang listing mo para magka-trabaho.");
    public static readonly LocalizedText SetupPublishButton = new("Publish and find work", "I-publish at maghanap ng trabaho");
    public static readonly LocalizedText SetupRepublishNote = new("Your listing is live, but it now needs these to stay up to date.", "Live ang listing mo, pero kailangan na ito para ma-update.");

    // --- References (private, under Me) -------------------------------------
    public static readonly LocalizedText ReferencesTitle = new("References", "Mga reference");
    public static readonly LocalizedText ReferencesWhy = new(
        "Someone you did work for: a past customer or boss. Add 1 to {0}. Only SideGigPh sees these, not customers. We may call to check.",
        "Isang pinagtrabahuhan mo: dating customer o amo. 1 hanggang {0}. SideGigPh lang ang makakakita, hindi ang customer. Baka tawagan namin para i-check.");
    public static readonly LocalizedText ReferencesCount = new("{0} of {1}", "{0} sa {1}");
    public static readonly LocalizedText ReferencesEmpty = new("No reference yet. You need 1 to publish.", "Wala pang reference. Kailangan ng 1 para ma-publish.");
    public static readonly LocalizedText ReferenceName = new("Their name", "Pangalan niya");
    public static readonly LocalizedText ReferenceNumber = new("Their mobile number", "Mobile number niya");
    public static readonly LocalizedText ReferenceAdd = new("Add reference", "I-add ang reference");
    public static readonly LocalizedText ReferenceRemove = new("Remove reference", "Tanggalin ang reference");
    public static readonly LocalizedText ReferenceNoName = new("Type their name.", "I-type ang pangalan niya.");
    public static readonly LocalizedText ReferenceBadNumber = new("That's not a mobile number. Try 0917 123 4567.", "Hindi mobile number 'yan. Halimbawa: 0917 123 4567.");
    public static readonly LocalizedText ReferenceOwnNumber = new("That's your own number. Add someone else.", "Number mo 'yan. Ibang tao ang ilagay.");
    public static readonly LocalizedText ReferenceDuplicate = new("That number is already on your list.", "Nasa listahan mo na ang number na 'yan.");
    public static readonly LocalizedText ReferencesFull = new("You've added {0}, the most you can.", "Naka-{0} ka na, 'yan na ang pinakamarami.");
    public static readonly LocalizedText ReferenceFailed = new("Couldn't save. Check your internet and try again.", "Hindi na-save. I-check ang internet mo at subukan ulit.");

    // --- Pro inbox --------------------------------------------------------
    public static readonly LocalizedText ProInboxWhy = new("Customers who messaged you about a job. Answer them here.", "Mga customer na nag-message tungkol sa trabaho. Dito mo sila sagutin.");
    public static readonly LocalizedText ProInboxEmpty = new("No messages yet. They show up here when a customer messages you.", "Wala pang message. Lalabas dito kapag may customer na nag-message sa'yo.");
    public static readonly LocalizedText ChatBudget = new("Budget: {0}", "Budget: {0}");

    // --- Reviews ----------------------------------------------------------
    public static readonly LocalizedText ReviewsTitle = new("Reviews", "Reviews");
    public static readonly LocalizedText ReviewsCount = new("{0} reviews", "{0} reviews");
    public static readonly LocalizedText ReviewsCountOne = new("1 review", "1 review");
    public static readonly LocalizedText OverallRating = new("Overall rating", "Overall rating");
    public static readonly LocalizedText ReviewsEmpty = new("No reviews yet. After a job, ask your customer to rate you on SideGigPh.", "Wala pang review. Pagkatapos ng trabaho, sabihan ang customer na i-rate ka sa SideGigPh.");

    // --- Photos -----------------------------------------------------------
    public static readonly LocalizedText PhotosTitle = new("Photos of your work", "Photos ng gawa mo");
    public static readonly LocalizedText PhotosCount = new("{0} of {1} photos", "{0} sa {1} photos");
    public static readonly LocalizedText PhotosWhy = new("Customers see these on your listing. At least {0}, up to {1} photos.", "Makikita ito ng customer sa listing mo. {0} o higit pa, hanggang {1} photos.");
    public static readonly LocalizedText PhotosEmpty = new("No photos yet. Customers like to see your work before they call.", "Wala pang photos. Gusto ng customer makita muna ang gawa mo bago tumawag.");
    public static readonly LocalizedText PhotosAdd = new("Add photos", "Mag-add ng photos");
    public static readonly LocalizedText PhotosFull = new("You've reached {0} photos. Remove one to add another.", "Umabot ka na sa {0} photos. Magtanggal muna bago mag-add.");
    public static readonly LocalizedText PhotoRemove = new("Remove photo", "Tanggalin ang photo");
    public static readonly LocalizedText PhotosDuplicate = new("Skipped photos you already added.", "Hindi na isinama ang photos na nandito na.");
    public static readonly LocalizedText PhotosFailed = new("Couldn't open your photos. Please try again.", "Hindi mabuksan ang photos mo. Subukan ulit.");
    public static readonly LocalizedText PhotosNoPermission = new("SideGigPh can't see your photos. Allow Photos in Settings, then try again.", "Hindi makita ng SideGigPh ang photos mo. I-allow ang Photos sa Settings, tapos subukan ulit.");
    public static readonly LocalizedText PhotosOpenSettings = new("Open Settings", "Buksan ang Settings");
    public static readonly LocalizedText PhotosUploadFailed = new("Couldn't upload your photo. Check your internet and try again.", "Hindi na-upload ang photo. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText PhotosTaken = new("This photo is already on another account, so it can't be used. Please use your own photo.", "Gamit na ng ibang account ang photo na ito, kaya hindi pwede. Gamitin ang sarili mong photo.");

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
    public static readonly LocalizedText PublishPhoneUsedBySeeker = new(
        "This number is already used by a customer account. One number, one account.",
        "Gamit na ang number na ito ng isang customer account. Isang number, isang account lang.");
    public static readonly LocalizedText PublishBadReference = new(
        "One of your references has your own number. Change it to someone you worked for.",
        "Number mo ang nasa isa sa mga reference mo. Palitan ng taong pinagtrabahuhan mo.");
    public static readonly LocalizedText PublishFixReferences = new("Fix references", "Ayusin ang reference");
    public static readonly LocalizedText PublishFailed =new("Your listing didn't publish. Check your internet and try again.", "Hindi na-publish ang listing mo. I-check ang internet mo at subukan ulit.");
}
