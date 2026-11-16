namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// Every Tarbaho UI string, English and Taglish side by side (the skeleton's shared strings,
/// e.g. Back, Next, Start over, are in MobileAppTheme's <see cref="CommonCopy"/>). Many of our users are not confident
/// English readers: keep both versions short and plain, and keep Taglish conversational.
/// Reference-data labels (jobs, towns, prices) live in <see cref="ReferenceData"/>.
/// </summary>
public static class Copy
{
    // --- Brand taglines (Tarbaho) -------------------------------------------
    /// <summary>The Seeker line under the logo; {0} = a rotating honorific, plural in English (<see cref="Honorifics"/>).</summary>
    public static readonly LocalizedText SeekerTagline = new("Find {0} nearby", "May {0} para diyan");

    /// <summary>The Skilled Worker lines that take turns on the busy splash (<see cref="RoleSplashLines"/>).</summary>
    public static readonly IReadOnlyList<LocalizedText> ProTaglines =
    [
        new("Get jobs near you", "May trabaho malapit sa'yo"),
        new("Customers come to you", "Ikaw ang hahanapin ng customer"),
        new("No commission, ever", "Walang commission, sa'yo lahat"),
        new("Earn more for your family", "Umextra para sa pamilya"),
    ];

    // --- Role fork --------------------------------------------------------
    // Service words, never job/hire/employment words: Tarbaho is a directory of independent workers, not an employer
    // or recruiter (Labor Code Art. 13(b); docs/legal/README.md L1).
    public static readonly LocalizedText RoleTitle = new("Find a Skilled Worker or Offer Your Services", "Maghanap ng Skilled Worker o Mag-alok ng Serbisyo");
    public static readonly LocalizedText RoleSeeker = new("I need someone for a job", "May ipapagawa ako");
    public static readonly LocalizedText RoleSeekerHint = new("Aircon, plumbing, repairs, errands, mechanic", "Aircon, tubero, repair, PasaBuy, mekaniko");
    public static readonly LocalizedText RoleProfessional = new("I offer my skills", "May skills akong maiaalok");
    public static readonly LocalizedText RoleProfessionalHint = new("Get customers near you", "Magkaroon ng customer malapit sa'yo");

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
    // A suggested price, never "minimum wage": workers are independent and set their own prices (docs/legal/README.md L2).
    public static readonly LocalizedText BudgetFromHelp = new("At least {0}, the suggested minimum price", "Hindi bababa sa {0}, ang suggested na pinakamababang presyo");
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

    // --- Seeker shell + home ----------------------------------------------
    public static readonly LocalizedText LastSearch = new("Recent Search", "Recent Search");
    public static readonly LocalizedText SeePros = new("View more", "View more");
    public static readonly LocalizedText SectionMessages = new("Recent messages", "Recent messages");

    // --- Search -----------------------------------------------------------
    public static readonly LocalizedText SearchTitle = new("Find a Skilled Worker", "Maghanap ng Skilled Worker");
    /// <summary>{0} = an honorific (<see cref="Honorifics"/>), e.g. "Find a Manong…" / "Hanap ng Kuya…".</summary>
    public static readonly LocalizedText SearchPlaceholder = new("Find a {0}…", "Hanap ng {0}…");
    public static readonly LocalizedText SearchNoMatch = new("We couldn't tell what job \"{0}\" is. Pick one below.", "Hindi namin makuha kung anong trabaho ang \"{0}\". Pumili sa baba.");
    public static readonly LocalizedText SearchPickTrade = new("Which kind of skilled worker is \"{0}\" for? Pick one below.", "Which kind of skilled worker is \"{0}\"? Pumili sa baba.");
    public static readonly LocalizedText SearchTradesLabel =new("Kind of skilled worker", "Klase ng skilled worker");
    public static readonly LocalizedText SearchJobsLabel = new("Job", "Trabaho");
    public static readonly LocalizedText SearchAllJobs = new("All {0} jobs", "Lahat ng {0}");
    public static readonly LocalizedText FilterTown = new("Town", "Bayan");
    public static readonly LocalizedText FilterBudget = new("Budget", "Budget");

    // --- Seeker Me: their search answers, each one tap from its question ---
    public static readonly LocalizedText PreferencesTitle = new("Your preferences", "Mga pinili mo");
    public static readonly LocalizedText PreferenceNeed = new("What you need", "Kailangan mo");
    public static readonly LocalizedText PreferenceWhen = new("When", "Kailan");
    public static readonly LocalizedText PreferenceNotSet = new("Not set", "Wala pa");
    public static readonly LocalizedText BrowseTitle = new("Browse by service", "Pumili ng serbisyo");

    // --- Search results ---------------------------------------------------
    public static readonly LocalizedText ResultsTitle = new("{0} for you", "{0} skilled worker para sa 'yo");
    public static readonly LocalizedText ResultsTitleOne = new("1 skilled worker for you", "1 skilled worker para sa 'yo");
    public static readonly LocalizedText ResultsInTown = new("Goes to {0}", "Pumupunta sa {0}");
    public static readonly LocalizedText ResultsOtherTowns = new("Other towns nearby", "Taga-ibang bayan na malapit");
    public static readonly LocalizedText ResultsWidened = new("No skilled workers in {0} yet, so here are skilled workers from nearby towns.", "Wala pang skilled worker sa {0}, kaya heto ang mga skilled worker sa kalapit na bayan.");
    /// <summary>{0} = the trade's honorific, plural in English (<see cref="Honorifics"/>).</summary>
    public static readonly LocalizedText ResultsEmpty = new("No {0} for this job here yet… soon!", "Wala pang {0} dito… soon!");
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
    // What we checked, never a promise about the worker (docs/legal/README.md L3).
    public static readonly LocalizedText ProProfileTrust = new("What we checked", "Ano ang na-check namin");
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
        "Hi! I found you on Tarbaho. I need: {0}, in {1}, today. Are you available?",
        "Hi po! Nakita ko kayo sa Tarbaho. Kailangan ko po: {0}, dito sa {1}, ngayon. Available po ba kayo?");
    public static readonly LocalizedText MessageThisWeek = new(
        "Hi! I found you on Tarbaho. I need: {0}, in {1}, this week. Are you available?",
        "Hi po! Nakita ko kayo sa Tarbaho. Kailangan ko po: {0}, dito sa {1}, ngayong linggo. Available po ba kayo?");
    public static readonly LocalizedText MessageCanvass = new(
        "Hi! I found you on Tarbaho. I need: {0}, in {1}. How much do you charge?",
        "Hi po! Nakita ko kayo sa Tarbaho. Kailangan ko po: {0}, dito sa {1}. Magkano po ang singil ninyo?");

    // --- Pro card ---------------------------------------------------------
    public static readonly LocalizedText RatePerJob = new("{0} per job", "{0} per job");
    public static readonly LocalizedText RateQuote = new("Price depends on the job", "Depende sa trabaho ang presyo");
    public static readonly LocalizedText ExperienceFact = new("Exp: {0}", "Exp: {0}");
    public static readonly LocalizedText ServesFact = new("Location: {0}", "Location: {0}");

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
        "Our suggested minimum price is {0} per job. You set your own price.",
        "Ang suggested na pinakamababang presyo ay {0} kada trabaho. Ikaw ang magpapasya ng presyo mo.");

    // Goal payoff. {0} = jobs per week, {1} = typical price per job.

    public static readonly LocalizedText ProPreviewTitle = new("This is how customers will see you", "Ganito ka makikita ng mga customer");
    public static readonly LocalizedText ProPreviewWhy = new("Your listing, built from your answers.", "Ang listing mo, galing sa mga sagot mo.");
    public static readonly LocalizedText ProPreviewConfirm = new("DONE", "DONE");

    // Loader after the preview's Done, while the answers save and Pro home loads.

    // --- Pro home shell (top bar + bottom tabs) ----------------------------
    public static readonly LocalizedText NavHome = new("Home", "Home");
    public static readonly LocalizedText NavInbox = new("Inbox", "Inbox");
    public static readonly LocalizedText NavReviews = new("Reviews", "Reviews");
    public static readonly LocalizedText NavPosts = new("Post", "Post");
    public static readonly LocalizedText NavRequests = new("Requests", "Requests");

    // Job posts. A household says what it needs done and its budget; workers of that trade nearby see it.
    // Service words only (CLAUDE.md, Legal): "what you need done", never job / hire.
    public static readonly LocalizedText PostsTitle = new("What you need done", "Mga ipapagawa mo");
    public static readonly LocalizedText PostsWhy = new("Post it once. Workers of that trade near you will see it.", "I-post nang isang beses. Makikita ng mga malapit na gumagawa nito.");
    public static readonly LocalizedText PostsEmpty = new("Nothing posted yet. Say what you need done and your budget.", "Wala ka pang post. Sabihin kung ano ang ipapagawa at magkano ang budget mo.");
    public static readonly LocalizedText PostsNew = new("Post what you need", "Mag-post ng ipapagawa");
    public static readonly LocalizedText PostNewTitle = new("What do you need done?", "Ano ang ipapagawa mo?");
    public static readonly LocalizedText PostNewWhy = new("Only workers of this trade near you will see it. Your exact address is never shown.", "Mga malapit na gumagawa lang nito ang makakakita. Hindi ipinapakita ang eksaktong address mo.");
    public static readonly LocalizedText PostTrade = new("Kind of work", "Uri ng gawa");
    public static readonly LocalizedText PostService = new("What exactly", "Ano mismo");
    public static readonly LocalizedText PostDescription = new("Describe the problem", "Ikuwento ang problema");
    public static readonly LocalizedText PostDescriptionHint = new("e.g. The kitchen faucet leaks even when closed", "hal. Tumutulo ang gripo sa kusina kahit sarado");
    public static readonly LocalizedText PostBudget = new("Your budget", "Budget mo");
    public static readonly LocalizedText PostBudgetWhy = new("Your own number. You and the worker agree on the final price.", "Sarili mong presyo. Kayo ng gagawa ang mag-uusap sa final na presyo.");
    public static readonly LocalizedText PostPhotosAdd = new("Add photos of the problem", "Maglagay ng photo ng problema");
    public static readonly LocalizedText PostPhotosHint = new("Up to {0} photos. You can skip this.", "Hanggang {0} photo. Puwedeng wala.");
    public static readonly LocalizedText PostPhotosLabel = new("Photos of the problem", "Mga photo ng problema");
    public static readonly LocalizedText PostSend = new("Post", "I-post");
    public static readonly LocalizedText PostFailed = new("It didn't post. Check your internet and try again.", "Hindi na-post. I-check ang internet at subukan ulit.");
    public static readonly LocalizedText PostShowing = new("Workers can see it", "Nakikita ng mga gagawa");
    public static readonly LocalizedText PostTakenDown = new("Taken down", "Tinanggal na");
    public static readonly LocalizedText PostTakeDown = new("Take down", "Tanggalin");
    public static readonly LocalizedText PostTakeDownFailed = new("It wasn't taken down. Check your internet and try again.", "Hindi natanggal. I-check ang internet at subukan ulit.");
    public static readonly LocalizedText PostBudgetFact = new("Budget {0}", "Budget {0}");
    public static readonly LocalizedText PostDistance = new("{0} · {1} km away", "{0} · {1} km ang layo");
    public static readonly LocalizedText PostOffersService = new("You offer this", "Serbisyo mo ito");
    public static readonly LocalizedText PostReport = new("Report as fake", "I-report na peke");
    public static readonly LocalizedText PostReported = new("Thanks. We will check it.", "Salamat. Iche-check namin.");
    public static readonly LocalizedText PostReportFailed = new("The report didn't send. Check your internet and try again.", "Hindi na-send ang report. I-check ang internet at subukan ulit.");
    public static readonly LocalizedText ProPostsTitle = new("Customers need this", "Kailangan ng mga customer");
    public static readonly LocalizedText ProPostsWhy = new("Your trade, near you. What you offer comes first, then the nearest.", "Trade mo, malapit sa'yo. Una ang mga serbisyo mo, tapos ang pinakamalapit.");
    public static readonly LocalizedText ProPostsEmpty = new("Nothing near you right now. New posts show up here.", "Wala pang malapit sa'yo ngayon. Dito lalabas ang mga bagong post.");
    public static readonly LocalizedText ProPostsNeedListing = new("Publish your listing first, so we know your trade and where you go.", "I-publish muna ang listing mo para alam namin ang trade mo at kung saan ka pumupunta.");

    // --- Pro home ---------------------------------------------------------
    public static readonly LocalizedText ProHomeNotLive = new("Saved on this phone. Not visible to customers yet.", "Saved on this phone. Hindi pa ito nakikita ng customer.");
    public static readonly LocalizedText StatusPublished = new("Published", "Naka-publish na");
    public static readonly LocalizedText StatusNotPublished = new("Not published yet", "Hindi pa naka-publish");
    public static readonly LocalizedText StatRating = new("{0} ★ ({1})", "{0} ★ ({1})");
    public static readonly LocalizedText SectionReviews = new("Latest reviews", "Mga bagong review");

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
        "Someone you did work for: a past customer or boss. Add 1 to {0}. Only Tarbaho sees these, not customers. We may call to check.",
        "Isang pinagtrabahuhan mo: dating customer o amo. 1 hanggang {0}. Tarbaho lang ang makakakita, hindi ang customer. Baka tawagan namin para i-check.");
    public static readonly LocalizedText ReferencesCount = new("{0} of {1}", "{0} sa {1}");
    public static readonly LocalizedText ReferencesEmpty = new("No reference yet. You need 1 to publish.", "Wala pang reference. Kailangan ng 1 para ma-publish.");
    public static readonly LocalizedText ReferenceName = new("Their name", "Pangalan niya");
    public static readonly LocalizedText ReferenceNumber = new("Their mobile number", "Mobile number niya");
    public static readonly LocalizedText ReferenceAdd = new("Add reference", "I-add ang reference");
    public static readonly LocalizedText ReferenceConsent = new(
        "They agreed to be my reference and to be contacted by Tarbaho.",
        "Pumayag siyang maging reference ko at na tawagan siya ng Tarbaho.");
    public static readonly LocalizedText ReferenceRemove = new("Remove reference", "Tanggalin ang reference");
    public static readonly LocalizedText ReferenceNoName = new("Type their name.", "I-type ang pangalan niya.");
    public static readonly LocalizedText ReferenceBadNumber = new("That's not a mobile number. Try 0917 123 4567.", "Hindi mobile number 'yan. Halimbawa: 0917 123 4567.");
    public static readonly LocalizedText ReferenceOwnNumber = new("That's your own number. Add someone else.", "Number mo 'yan. Ibang tao ang ilagay.");
    public static readonly LocalizedText ReferenceDuplicate = new("That number is already on your list.", "Nasa listahan mo na ang number na 'yan.");
    public static readonly LocalizedText ReferencesFull = new("You've added {0}, the most you can.", "Naka-{0} ka na, 'yan na ang pinakamarami.");
    public static readonly LocalizedText ReferenceFailed = new("Couldn't save. Check your internet and try again.", "Hindi na-save. I-check ang internet mo at subukan ulit.");

    // --- Badges (on cards and profiles; the pro's own under Me) ---------------
    public static readonly LocalizedText BadgeId = new("ID Verified", "ID Verified");
    public static readonly LocalizedText BadgeTesda = new("TESDA Certified", "TESDA Certified");
    public static readonly LocalizedText BadgeProject = new("{0} project", "Project ng {0}");
    public static readonly LocalizedText BadgeMore = new("+{0} more", "+{0} pa");
    public static readonly LocalizedText BadgeIdNote = new("We checked a government ID against their photo.", "Na-check namin ang government ID at ang photo niya.");
    public static readonly LocalizedText BadgeTesdaNote = new("We checked a TESDA certificate.", "Na-check namin ang TESDA certificate niya.");
    public static readonly LocalizedText BadgeProjectNote = new("We checked proof that they worked there.", "Na-check namin ang patunay na nagtrabaho siya doon.");
    public static readonly LocalizedText BadgeProjectAt = new("{0}. We checked proof that they worked there.", "{0}. Na-check namin ang patunay na nagtrabaho siya doon.");
    public static readonly LocalizedText BadgesTitle = new("Badges", "Mga badge");
    public static readonly LocalizedText BadgesWhy = new(
        "Customers pick workers they can trust. Each badge shows on your listing and moves you up in search. It's free.",
        "Pinipili ng customer ang mapagkakatiwalaan. Lalabas ang bawat badge sa listing mo at aangat ka sa search. Libre ito.");
    public static readonly LocalizedText BadgesCount = new("{0} of {1}", "{0} sa {1}");
    public static readonly LocalizedText BadgeIdTitle = new("Government ID", "Government ID");
    public static readonly LocalizedText BadgeTesdaTitle = new("TESDA certificate or training", "TESDA certificate o training");
    public static readonly LocalizedText BadgeProjectTitle = new("Past project with a company", "Dating project sa isang kumpanya");
    public static readonly LocalizedText BadgeIdHint = new("Send a photo of 1 valid ID.", "Mag-send ng photo ng 1 valid ID.");
    public static readonly LocalizedText BadgeTesdaHint = new("Send a photo of your NC or training certificate.", "Mag-send ng photo ng NC o training certificate mo.");
    public static readonly LocalizedText BadgeProjectHint = new("Ayala Land, DMCI, or any company you worked for.", "Ayala Land, DMCI, o kahit anong kumpanyang pinagtrabahuhan mo.");
    public static readonly LocalizedText BadgeStatePending = new("Being checked", "Chine-check pa");
    public static readonly LocalizedText BadgeStateApproved = new("Approved", "Approved");
    public static readonly LocalizedText BadgeStateRejected = new("Not approved", "Hindi na-approve");
    public static readonly LocalizedText BadgePendingNote = new("We check within 2 days. You don't need to do anything.", "Iche-check namin sa loob ng 2 araw. Wala ka nang kailangang gawin.");
    public static readonly LocalizedText BadgeRejectedNote = new("Why: {0}. You can send a clearer photo.", "Dahilan: {0}. Pwede kang mag-send ng mas malinaw na photo.");
    public static readonly LocalizedText BadgeRejectedNoReason = new("You can send a clearer photo.", "Pwede kang mag-send ng mas malinaw na photo.");
    public static readonly LocalizedText BadgeAddProject = new("Add another company", "Mag-add ng ibang kumpanya");
    public static readonly LocalizedText BadgeIdWhat = new(
        "Any valid government ID: National ID, driver's license, UMID, passport, voter's ID. The whole card, clear and not blurry.",
        "Kahit anong valid government ID: National ID, driver's license, UMID, passport, voter's ID. Buong card, malinaw at hindi malabo.");
    public static readonly LocalizedText BadgeTesdaWhat = new(
        "Your TESDA National Certificate (NC I to NC IV) or a training certificate. The whole page, with your name and the certificate number.",
        "Ang TESDA National Certificate mo (NC I hanggang NC IV) o training certificate. Buong page, kita ang pangalan mo at ang certificate number.");
    public static readonly LocalizedText BadgeProjectWhat = new(
        "Proof that you worked there: company or site ID, certificate of employment, payslip, or a photo of you at the site.",
        "Patunay na nagtrabaho ka doon: company o site ID, certificate of employment, payslip, o photo mo sa site.");
    public static readonly LocalizedText BadgeCompanyLabel = new("Which company?", "Anong kumpanya?");
    public static readonly LocalizedText BadgeCompanyOther = new("Another company", "Ibang kumpanya");
    public static readonly LocalizedText BadgeCompanyName = new("Company name", "Pangalan ng kumpanya");
    public static readonly LocalizedText BadgeProjectName = new("Building or project (optional)", "Building o project (optional)");
    public static readonly LocalizedText BadgeConsentId = new(
        "I agree that Tarbaho reads this ID with AI (name, ID number, expiry) and that a person at Tarbaho checks it against my photo. The photo is kept private and deleted after checking.",
        "Pumapayag akong basahin ng Tarbaho ang ID na ito gamit ang AI (pangalan, ID number, expiry) at i-check ito ng tao sa Tarbaho sa photo ko. Private ang photo at buburahin pagkatapos i-check.");
    public static readonly LocalizedText BadgeConsentDocument = new(
        "I agree that a person at Tarbaho checks this photo. It is kept private and deleted after checking.",
        "Pumapayag akong i-check ng tao sa Tarbaho ang photo na ito. Private ito at buburahin pagkatapos i-check.");
    public static readonly LocalizedText BadgeChoosePhoto = new("Choose photo and send", "Pumili ng photo at i-send");
    public static readonly LocalizedText BadgeAlreadySent = new("You already sent this one.", "Na-send mo na ito.");
    public static readonly LocalizedText BadgeNoCompany = new("Pick a company or type its name.", "Pumili ng kumpanya o i-type ang pangalan nito.");
    public static readonly LocalizedText BadgePhotoDenied = new("Tarbaho can't open your photos. Allow it in Settings.", "Hindi mabuksan ng Tarbaho ang photos mo. I-allow sa Settings.");
    public static readonly LocalizedText BadgeFailed = new("Didn't send. Check your internet and try again.", "Hindi na-send. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText BadgeNothingToSend = new("This badge is already sent. Nothing more to do.", "Na-send na ang badge na ito. Wala ka nang kailangang gawin.");

    // --- Pro inbox --------------------------------------------------------
    public static readonly LocalizedText ProInboxWhy = new("Customers who messaged you about a job. Answer them here.", "Mga customer na nag-message tungkol sa trabaho. Dito mo sila sagutin.");
    public static readonly LocalizedText ProInboxEmpty = new("No messages yet. They show up here when a customer messages you.", "Wala pang message. Lalabas dito kapag may customer na nag-message sa'yo.");
    public static readonly LocalizedText ChatBudget = new("Budget: {0}", "Budget: {0}");

    // --- Reviews ----------------------------------------------------------
    public static readonly LocalizedText ReviewsTitle = new("Reviews", "Reviews");
    public static readonly LocalizedText ReviewsCount = new("{0} reviews", "{0} reviews");
    public static readonly LocalizedText ReviewsCountOne = new("1 review", "1 review");
    public static readonly LocalizedText OverallRating = new("Overall rating", "Overall rating");
    public static readonly LocalizedText ReviewsEmpty = new("No reviews yet. After a job, ask your customer to rate you on Tarbaho.", "Wala pang review. Pagkatapos ng trabaho, sabihan ang customer na i-rate ka sa Tarbaho.");

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
    public static readonly LocalizedText PhotosNoPermission = new("Tarbaho can't see your photos. Allow Photos in Settings, then try again.", "Hindi makita ng Tarbaho ang photos mo. I-allow ang Photos sa Settings, tapos subukan ulit.");
    public static readonly LocalizedText CameraNoPermission = new("Tarbaho can't use your camera. Allow Camera in Settings, or choose a photo instead.", "Hindi magamit ng Tarbaho ang camera mo. I-allow ang Camera sa Settings, o pumili na lang ng photo.");
    public static readonly LocalizedText SelfieTake = new("Take a selfie", "Mag-selfie");
    public static readonly LocalizedText SelfieRetake = new("Take another", "Mag-selfie ulit");
    public static readonly LocalizedText SelfieFromPhotos = new("Choose from your photos", "Pumili sa photos mo");
    public static readonly LocalizedText SelfieWhy = new("Customers trust a face. Your front camera will open.", "Mas nagtitiwala ang customer kapag may mukha. Bubukas ang front camera mo.");
    public static readonly LocalizedText PhotosOpenSettings = new("Open Settings", "Buksan ang Settings");
    public static readonly LocalizedText PhotosUploadFailed = new("Couldn't upload your photo. Check your internet and try again.", "Hindi na-upload ang photo. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText PhotosTaken = new("This photo is already on another account, so it can't be used. Please use your own photo.", "Gamit na ng ibang account ang photo na ito, kaya hindi pwede. Gamitin ang sarili mong photo.");

    // --- Profile ----------------------------------------------------------
    public static readonly LocalizedText ProfileTitle = new("Your profile", "Profile");
    public static readonly LocalizedText ProfilePhotoAdd = new("Add your photo", "Maglagay ng photo mo");
    public static readonly LocalizedText ProfilePhotoChange = new("Change photo", "Palitan ang photo");
    public static readonly LocalizedText ProfileEdit = new("Edit listing", "Edit listing");
    public static readonly LocalizedText ProfileNameChange = new("Change name", "Palitan ang pangalan");
    // Me page: the error reports switch (Services/Diagnostics).
    public static readonly LocalizedText ErrorReportsLabel = new("Send error reports", "Magpadala ng error reports");
    public static readonly LocalizedText ErrorReportsHelp = new(
        "If the app has a problem, it sends us a technical report so we can fix it. It never includes your messages, name or number.",
        "Kapag nagka-problema ang app, magpapadala ito ng technical report para maayos namin. Walang kasamang messages, pangalan o number mo.");
    public static readonly LocalizedText ProfileSince = new("On Tarbaho since {0}", "Nasa Tarbaho mula {0}");

    // --- Publish ----------------------------------------------------------
    public static readonly LocalizedText PublishTitle = new("Ready to publish?", "Ready na bang i-publish?");
    public static readonly LocalizedText PublishWhy = new("Customers in your towns will see this and can call or text you.", "Makikita ito ng customer sa mga bayan mo at puwede ka nilang tawagan o i-text.");
    public static readonly LocalizedText PublishPledge = new("I'll answer customers and do honest work at the price I posted.", "Sasagutin ko ang customer at tapat akong gagawa sa presyong nilagay ko.");
    public static readonly LocalizedText PublishConfirm = new("Publish", "Publish");
    public static readonly LocalizedText PublishPhoneTaken = new("This number already has a listing. One number, one listing.", "May listing na ang number na ito. Isang number, isang listing lang.");
    public static readonly LocalizedText PublishBadReference = new(
        "One of your references has your own number. Change it to someone you worked for.",
        "Number mo ang nasa isa sa mga reference mo. Palitan ng taong pinagtrabahuhan mo.");
    public static readonly LocalizedText ListingEditUnfinished = new(
        "Your listing edit isn't finished, so customers still see your old listing.",
        "Hindi pa tapos ang pag-edit ng listing mo, kaya luma pa rin ang nakikita ng customers.");
    public static readonly LocalizedText ListingEditFinish = new("Finish editing", "Tapusin ang pag-edit");
    public static readonly LocalizedText PublishFixReferences = new("Fix references", "Ayusin ang reference");
    public static readonly LocalizedText PublishFailed = new("Your listing didn't publish. Check your internet and try again.", "Hindi na-publish ang listing mo. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText PublishRefused = new(
        "Your listing didn't publish. It's on our side: we got a report and will fix it. Please try again later.",
        "Hindi na-publish ang listing mo. Problema ito sa amin: na-report na at aayusin namin. Subukan ulit mamaya.");
    public static readonly LocalizedText PublishPhotoInUse = new(
        "One of your photos is already used by another account. Change your photos and try again.",
        "Gamit na ng ibang account ang isa sa mga photo mo. Palitan ang photos mo at subukan ulit.");
    public static readonly LocalizedText PublishTooManyTries = new("Too many tries. Wait a few minutes, then try again.", "Sobrang daming try. Maghintay ng ilang minuto bago ulitin.");

    // --- Report and block (Components/Safety) --------------------------------
    public static readonly LocalizedText ReportButton = new("Report or block", "I-report o i-block");
    public static readonly LocalizedText ReportTitle = new("What's wrong?", "Ano ang problema?");
    public static readonly LocalizedText ReportWhy = new(
        "Only Tarbaho sees your report. We look into every report within 5 business days.",
        "Tarbaho lang ang makakakita ng report mo. Titingnan namin ang bawat report sa loob ng 5 business days.");
    public static readonly LocalizedText ReportEmergency = new("In an emergency, call 911.", "Kung emergency, tumawag sa 911.");
    public static readonly LocalizedText ReportScam = new("Scam or asking for money", "Scam o nanghihingi ng pera");
    public static readonly LocalizedText ReportHarassment = new("Rude, threatening or sexual", "Bastos, nananakot o malaswa");
    public static readonly LocalizedText ReportFake = new("Fake profile, photos or review", "Pekeng profile, photos o review");
    public static readonly LocalizedText ReportInappropriate = new("Offensive photo or words", "Nakakasakit na photo o salita");
    public static readonly LocalizedText ReportEmployment = new("Offering a job or hiring workers", "Nag-aalok ng trabaho o naghahanap ng empleyado");
    public static readonly LocalizedText ReportOther = new("Something else", "Iba pa");
    public static readonly LocalizedText ReportNote = new("Tell us more (optional)", "Ikuwento pa (optional)");
    public static readonly LocalizedText ReportAlsoBlock = new(
        "Also block them: you won't see each other or chat again",
        "I-block din: hindi na kayo magkikita o makakapag-chat sa app");
    public static readonly LocalizedText ReportSend = new("Send report", "I-send ang report");
    public static readonly LocalizedText ReportJustBlock = new("Just block, don't report", "I-block lang, huwag i-report");
    public static readonly LocalizedText ReportFailed = new("Didn't send. Check your internet and try again.", "Hindi na-send. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText ReportThanksTitle = new("Thanks for telling us", "Salamat sa pagsabi");
    public static readonly LocalizedText ReportThanksWhy = new(
        "We'll look into it within 5 business days.",
        "Titingnan namin ito sa loob ng 5 business days.");
    public static readonly LocalizedText BlockedTitle = new("Blocked", "Na-block na");
    public static readonly LocalizedText BlockedWhy = new(
        "You won't see each other in the app, and they can't message you.",
        "Hindi na kayo magkikita sa app, at hindi ka na nila mame-message.");
    public static readonly LocalizedText ReportDone = new("Done", "Tapos na");

    // --- Feedback board (Components/Feedback) ---------------------------------
    public static readonly LocalizedText FeedbackMenu = new("Suggest a feature", "Mag-suggest ng feature");
    public static readonly LocalizedText FeedbackMenuHint = new("Vote on what we add next", "Bumoto kung ano ang susunod naming idadagdag");
    public static readonly LocalizedText FeedbackTitle = new("Ideas for Tarbaho", "Mga idea para sa Tarbaho");
    public static readonly LocalizedText FeedbackWhy = new(
        "Vote for what you want most. We work on the top ones first.",
        "I-vote ang pinakagusto mo. Uunahin namin ang may pinakamaraming boto.");
    public static readonly LocalizedText FeedbackEmpty = new("No ideas yet. Be the first to suggest one.", "Wala pang idea. Ikaw na ang mauna.");
    public static readonly LocalizedText FeedbackSuggest = new("Suggest a feature", "Mag-suggest ng feature");
    public static readonly LocalizedText FeedbackVote = new("Vote for this", "I-vote ito");
    public static readonly LocalizedText FeedbackUnvote = new("Take back your vote", "Bawiin ang boto mo");
    public static readonly LocalizedText FeedbackVotesOne = new("1 vote", "1 boto");
    /// <summary>{0} = how many people voted.</summary>
    public static readonly LocalizedText FeedbackVotes = new("{0} votes", "{0} boto");
    public static readonly LocalizedText FeedbackPlanned = new("Planned", "Gagawin na");
    public static readonly LocalizedText FeedbackDone = new("Done", "Tapos na");
    public static readonly LocalizedText FeedbackMine = new("Your idea", "Idea mo");
    public static readonly LocalizedText FeedbackRemove = new("Remove", "Alisin");
    public static readonly LocalizedText FeedbackReport = new("Report this", "I-report ito");
    public static readonly LocalizedText FeedbackReported = new("Reported. Thank you.", "Na-report na. Salamat.");
    public static readonly LocalizedText FeedbackFailed = new(
        "That didn't go through. Check your internet and try again.",
        "Hindi natuloy. I-check ang internet mo at subukan ulit.");
    public static readonly LocalizedText SuggestTitle = new("What should we add?", "Ano ang gusto mong idagdag namin?");
    public static readonly LocalizedText SuggestWhy = new(
        "One idea at a time. Everyone can see it, so no names or numbers.",
        "Isang idea lang muna. Makikita ito ng lahat, kaya walang pangalan o number.");
    public static readonly LocalizedText SuggestLabel = new("Your idea", "Idea mo");
    public static readonly LocalizedText SuggestPlaceholder = new("e.g. Pay with GCash", "hal. Bayad gamit ang GCash");
    public static readonly LocalizedText SuggestDetails = new("More details (optional)", "Dagdag na detalye (optional)");
    public static readonly LocalizedText SuggestSimilarTitle = new("Is it one of these?", "Isa ba ito sa mga ito?");
    public static readonly LocalizedText SuggestSimilarWhy = new(
        "Someone may have asked already. Add your vote so it moves up.",
        "Baka may nag-suggest na. I-vote mo na lang para umangat.");
    /// <summary>{0} = what the person typed.</summary>
    public static readonly LocalizedText SuggestYours = new("Your idea: {0}", "Idea mo: {0}");
    public static readonly LocalizedText SuggestSame = new("Yes, same idea: add my vote", "Oo, pareho: i-vote ko");
    public static readonly LocalizedText SuggestSameVoted = new("Yes, same idea", "Oo, pareho");
    public static readonly LocalizedText SuggestDifferent = new("No, mine is different: post it", "Hindi, iba ang sa akin: i-post");
    public static readonly LocalizedText SuggestPostedTitle = new("Posted. Thank you!", "Na-post na. Salamat!");
    public static readonly LocalizedText SuggestPostedWhy = new(
        "Others can now vote for it. We work on the most voted ideas first.",
        "Pwede na itong i-vote ng iba. Uunahin namin ang may pinakamaraming boto.");
    public static readonly LocalizedText SuggestVotedTitle = new("Your vote is in", "Nabilang na ang boto mo");
    public static readonly LocalizedText SuggestVotedWhy = new("That idea just moved up. Thank you!", "Umangat ang idea na iyon. Salamat!");
    public static readonly LocalizedText SuggestDone = new("Back to ideas", "Balik sa mga idea");
}
