namespace SideGigPh.FrontDoor.Localization;

/// <summary>
/// Every word on the landing page, English and Taglish (from marketing/landing/copy.md). The app's copy rules hold:
/// workers are Kuya / Ate / Manong / Manang; service words, never job / hire / employment / salary; we never vouch for a
/// worker ("what we checked"); no stats, testimonials or launch dates.
/// </summary>
public static class Copy
{
    public static class Meta
    {
        // What Google shows as the result's title; the words people search (marketing/seo/keywords.md). The description
        // is index.html's own, so the page has exactly one.
        public static readonly Say Title = new("Aircon cleaning, tubero, electrician in Angono | Tarbaho", "Linis aircon, tubero, electrician sa Angono | Tarbaho");
    }

    public static class Header
    {
        public static readonly Say HowItWorks = new("How it works", "Paano gumagana");
        public static readonly Say Trades = new("Trades", "Mga serbisyo");
        public static readonly Say Faq = new("FAQ", "Mga tanong");
        public static readonly Say EarlyAccess = new("Early access", "Early access");
        public static readonly Say LanguageLabel = new("Choose language", "Pumili ng wika");
        public static readonly Say English = new("EN", "EN");
        public static readonly Say Taglish = new("Taglish", "Taglish");
        public static readonly Say Home = new("Tarbaho, back to top", "Tarbaho, balik sa taas");
    }

    public static class Hero
    {
        // The Facebook page's words, so the page and the posts say the same thing.
        public static readonly Say Eyebrow = new("Powered by AI · Early access", "Powered by AI · Early access");
        public static readonly Say Headline = new("Finding skilled workers has never been easier.", "Mas madali na ngayong humanap ng skilled worker.");
        public static readonly Say Sub = new(
            "Tarbaho connects homes in Angono, Antipolo and Taytay with skilled workers nearby: Aircon, Construction, Pasabay, Auto, Pest Control and IT. See their prices, chat in the app.",
            "Kinokonekta ng Tarbaho ang mga bahay sa Angono, Antipolo at Taytay sa mga skilled worker na malapit: Aircon, Construction, Pasabay, Auto, Pest Control at IT. Kita agad ang presyo, chat sa app.");
        public static readonly Say Cta = new("Get early access", "Mag-early access");
        public static readonly Say WorkerLink = new("Skilled worker? Get more customers", "Skilled worker ka? Dagdag kliyente");
        public static readonly Say ShotSearch = new("The app: Aircon Technicians in Angono, each with a price", "Ang app: mga Aircon Technician sa Angono, may presyo bawat isa");
        public static readonly Say ShotOffer = new("The app: a worker sending a price for a request", "Ang app: si Kuya nagpapadala ng presyo sa isang request");
    }

    /// <summary>Section titles: the plain part, then the last word(s) shown on a tint highlight.</summary>
    public static class Titles
    {
        public static readonly (Say Lead, Say Mark) HowItWorks = (new("How it", "Paano"), new("works", "gumagana"));
        public static readonly (Say Lead, Say Mark) Why = (new("Why", "Bakit"), new("Tarbaho", "Tarbaho"));
        public static readonly (Say Lead, Say Mark) Trades = (new("Every kind of", "Bawat klase ng"), new("work", "gawa"));
        public static readonly (Say Lead, Say Mark) Trust = (new("Know who's", "Kilalanin ang"), new("coming", "darating"));
        public static readonly (Say Lead, Say Mark) Numbers = (new("Tarbaho in", "Tarbaho sa"), new("numbers", "numero"));
        public static readonly (Say Lead, Say Mark) Faq = (new("Your", "Mga"), new("questions", "tanong"));
    }

    /// <summary>Real counts only (never users, sign-ups or ratings before there are any).</summary>
    public static class Numbers
    {
        public static readonly Say TradesTitle = new("Trades", "Klase ng serbisyo");
        public static readonly Say TradesText = new("Aircon, construction, errands, IT, car mechanic and pest control.", "Aircon, construction, errands, IT, mekaniko at pest control.");
        public static readonly Say ConstructionTitle = new("Construction services", "Serbisyo sa construction");
        public static readonly Say ConstructionText = new("Foreman to helper: carpentry, electrical, plumbing, roofing and more.", "Foreman hanggang helper: karpintero, electrical, tubero, bubong at iba pa.");
        public static readonly Say TownsTitle = new("Towns in Rizal", "Bayan sa Rizal");
        public static readonly Say TownsText = new("Angono first, then Antipolo and Taytay.", "Angono muna, tapos Antipolo at Taytay.");
        public static readonly Say BadgesTitle = new("Badges we check", "Badge na chine-check");
        public static readonly Say BadgesText = new("Government ID, TESDA, and a past project.", "Government ID, TESDA, at past project.");
    }

    public static class Stores
    {
        public static readonly Say Soon = new("Soon", "Malapit na");
        public static readonly Say IosBeta = new("Beta on TestFlight", "Beta sa TestFlight");
        public static readonly Say AndroidLabel = new("Android app, coming soon", "Android app, malapit na");
        public static readonly Say IosLabel = new("iPhone beta on TestFlight", "iPhone beta sa TestFlight");
    }

    public static class Sides
    {
        public static readonly Say Household = new("I need work done", "May ipapagawa ako");
        public static readonly Say Worker = new("I'm a Kuya / Ate", "Kuya / Ate ako");
        public static readonly Say WorkerIntro = new("More customers near you, at your own price.", "Dagdag kliyente na malapit sa'yo, sa sarili mong presyo.");

        public static readonly Say H1Title = new("Post what you need done", "I-post ang ipapagawa");
        public static readonly Say H1Text = new("Pick the service, your town and your own budget. Add photos if you like.", "Piliin ang serbisyo, bayan at sariling budget mo. Lagyan ng picture kung gusto mo.");
        public static readonly Say H2Title = new("Get offers nearby", "Tumanggap ng alok");
        public static readonly Say H2Text = new("Kuya and Ate near you send their price. See their reviews and what we checked.", "Magpapadala ng presyo ang mga Kuya at Ate na malapit. Tingnan ang reviews at ang na-check namin.");
        public static readonly Say H3Title = new("Pick, chat, pay after", "Piliin, mag-chat, bayad pagkatapos");
        public static readonly Say H3Text = new("Chat in the app. Pay the worker directly after the work, at the price you both agreed.", "Mag-chat sa app. Bayaran si Kuya o Ate pagkatapos ng trabaho, sa presyong napagkasunduan.");

        public static readonly Say W1Title = new("List yourself for free", "Mag-lista nang libre");
        public static readonly Say W1Text = new("Your trade, the towns you go to, and your own price.", "Ang trade mo, mga bayan na pinupuntahan mo, at sariling presyo mo.");
        public static readonly Say W2Title = new("See Requests nearby", "Tingnan ang Requests sa malapit");
        public static readonly Say W2Text = new("Households post what they need done. Send your offer or take their budget.", "Nagpo-post ang mga nagpapagawa. Mag-alok ng presyo o kunin ang budget nila.");
        public static readonly Say W3Title = new("Get the work, get paid directly", "Makuha ang gawa, diretso ang bayad");
        public static readonly Say W3Text = new("The household pays you after the work. Your number stays private until they pick you.", "Ikaw ang babayaran pagkatapos ng gawa. Private ang number mo hanggang piliin ka.");

        public static readonly Say H1Shot = new("The app: posting what you need done, with where, when and your budget", "Ang app: pag-post ng kailangan mo, kung saan, kailan at budget mo");
        public static readonly Say H2Shot = new("The app: three offers for aircon cleaning, each with a price", "Ang app: tatlong offer para sa linis ng aircon, may presyo bawat isa");
        public static readonly Say H3Shot = new("The app: a chat between a household and a Kuya", "Ang app: chat ng household at ni Kuya");
        public static readonly Say W1Shot = new("The app: a Kuya typing his usual price per service", "Ang app: si Kuya nilalagay ang usual na presyo niya");
        public static readonly Say W2Shot = new("The app: a request nearby, and the Kuya sending his price", "Ang app: isang request malapit, at si Kuya nagpapadala ng presyo");
        public static readonly Say W3Shot = new("The app: chatting with the household that picked you", "Ang app: chat sa household na pumili sa iyo");
    }

    public static class Promises
    {
        public static readonly Say ZeroTitle = new("Pay the worker directly", "Diretso ang bayad");
        public static readonly Say ZeroText = new("Cash or GCash, straight to Kuya or Ate. The money never passes through the app.", "Cash o GCash, diretso kay Kuya o Ate. Hindi dumadaan sa app ang bayad.");
        public static readonly Say PriceTitle = new("Your own price", "Sariling presyo");
        public static readonly Say PriceText = new("Households set a budget, workers set their own price. You agree on the final price.", "May budget ang nagpapagawa, may sariling presyo si Kuya o Ate. Kayo ang magkakasundo.");
        public static readonly Say PrivateTitle = new("Numbers stay private", "Private ang number");
        public static readonly Say PrivateText = new("Chat in the app first. Numbers show only after the household picks a worker.", "Chat muna sa app. Lalabas lang ang number pag may napili na.");
        public static readonly Say NearbyTitle = new("Nearby, pay after", "Malapit lang, bayad pagkatapos");
        public static readonly Say NearbyText = new("Kuya and Ate from your town. You pay after the work is done.", "Kuya at Ate na taga-bayan mo. Bayad pagkatapos ng gawa.");
    }

    public static class Join
    {
        public static readonly Say Eyebrow = new("Early access", "Early access");
        public static readonly Say Headline = new("Be first in Angono", "Mauna ka sa Angono");
        public static readonly Say Sub = new("Leave your email. We'll tell you the moment Tarbaho opens near you.", "Iwan ang email mo. Sasabihan ka namin pag bukas na ang Tarbaho sa inyo.");
        public static readonly Say Bullet1 = new("First to get the app", "Una kang makakagamit");
        public static readonly Say Bullet2 = new("Free to join", "Libre sumali");
        public static readonly Say Bullet3 = new("Leave the list anytime", "Pwedeng umalis kahit kailan");
        public static readonly Say WorkerPerk = new("Workers on the list set up their profile first and see Requests first.", "Ang workers na nasa listahan, unang makakapag-setup at unang makakakita ng Requests.");

        public static readonly Say RoleLabel = new("I am a… (optional)", "Ako ay… (optional)");
        public static readonly Say RoleHousehold = new("Household", "Nagpapagawa");
        public static readonly Say RoleWorker = new("Kuya / Ate (worker)", "Kuya / Ate (worker)");
        public static readonly Say PhoneLabel = new("Which phone do you use?", "Anong phone mo?");
        public static readonly Say PhoneAndroid = new("Android", "Android");
        public static readonly Say PhoneIphone = new("iPhone", "iPhone");
        public static readonly Say PhoneMissing = new("Pick your phone, so we send the right invite.", "Piliin ang phone mo, para tama ang invite na ipapadala namin.");
        public static readonly Say EmailLabel = new("Email", "Email");
        public static readonly Say EmailLabelAndroid = new("Gmail on your Play Store", "Gmail sa Play Store mo");
        public static readonly Say EmailHelpAndroid = new("Open Play Store, tap your picture (top right): use that email.", "Buksan ang Play Store, i-tap ang picture mo (taas, kanan): iyon ang email.");
        public static readonly Say EmailHelpIphone = new("We'll email you when Tarbaho opens. The iPhone beta is ready now on TestFlight.", "I-email ka namin pag bukas na ang Tarbaho. Ready na ngayon ang iPhone beta sa TestFlight.");
        public static readonly Say TestFlightNote = new("On iPhone you can try it now: install Apple's free TestFlight app, then open our beta.", "Sa iPhone, pwede mo nang subukan: i-install ang libreng TestFlight app ng Apple, tapos buksan ang beta namin.");
        public static readonly Say TestFlightButton = new("Open the iPhone beta", "Buksan ang iPhone beta");
        public static readonly Say Button = new("Get early access", "Mag-early access");
        public static readonly Say Sending = new("Sending…", "Sine-send…");
        public static readonly Say Privacy = new("Only to tell you about Tarbaho. No spam. Leave anytime.", "Para lang sa balita ng Tarbaho. Walang spam. Pwede kang umalis kahit kailan.");
        public static readonly Say PrivacyLink = new("Privacy Policy", "Privacy Policy");

        public static readonly Say Empty = new("Please type your email.", "Pakilagay ang email mo.");
        public static readonly Say Invalid = new("That email looks wrong. Check it again.", "Mukhang mali ang email. Pakicheck ulit.");
        public static readonly Say DidYouMean = new("Did you mean {0}?", "{0} ba?");

        public static readonly Say SuccessTitle = new("Salamat! You're on the list.", "Salamat! Nasa listahan ka na.");
        public static readonly Say SuccessAndroid = new("We'll email your Play Store invite when Tarbaho is ready in Angono.", "I-email namin ang Play Store invite mo pag ready na ang Tarbaho sa Angono.");
        public static readonly Say SuccessIphone = new("The iPhone beta is ready now. We'll also email you when Tarbaho opens in Angono.", "Ready na ang iPhone beta. I-email ka rin namin pag bukas na ang Tarbaho sa Angono.");
        public static readonly Say ShareNudge = new("Know a Kuya, Ate or neighbor who'd use it? Send them the link.", "May kakilalang Kuya, Ate o kapitbahay na gagamit nito? I-send ang link.");
        public static readonly Say AlreadyText = new("You're already on the list. We'll email you soon.", "Nasa listahan ka na. I-email ka namin agad.");
        public static readonly Say Share = new("Share with a friend", "I-share sa kaibigan");
        public static readonly Say Copied = new("Link copied", "Na-copy ang link");
        public static readonly Say AnotherEmail = new("Use another email", "Ibang email");
        public static readonly Say ShareText = new("Tarbaho: find a Kuya or Ate nearby. Get early access:", "Tarbaho: may Kuya o Ate na malapit. Mag-early access:");

        public static readonly Say Error = new("That didn't send. Please try again.", "Hindi na-send. Pakisubukan ulit.");
        public static readonly Say TooMany = new("Too many tries. Wait a few minutes and try again.", "Sobrang dami ng try. Maghintay ng ilang minuto at subukan ulit.");
        public static readonly Say Offline = new("No internet. Your email is kept here; tap again when you're back.", "Walang internet. Nandito pa ang email mo; i-tap ulit pag may connection na.");
    }

    public static class Trades
    {
        public static readonly Say Heading = new("Kuya and Ate for every kind of work", "May Kuya at Ate sa bawat klase ng gawa");
        public static readonly Say ServicesHeading = new("Services in Angono, Antipolo and Taytay, Rizal", "Mga serbisyo sa Angono, Antipolo at Taytay, Rizal");
        public static readonly Say ServicesSub = new("Find a Kuya or Ate near you, see their price, and chat in the app.", "Humanap ng Kuya o Ate na malapit sa'yo, kita ang presyo, chat sa app.");
        public static readonly Say TownsHeading = new("Starting in Rizal", "Sa Rizal muna");
        public static readonly Say AllTrades = new("All trades", "Lahat ng serbisyo");
        public static readonly Say Construction = new("Construction", "Construction");
        public static readonly Say TownsSub = new("We start in one town, then grow barangay by barangay.", "Isang bayan muna, tapos isa-isang barangay.");
        public static readonly Say First = new("Starting here", "Dito muna");
        public static readonly Say Next = new("Next", "Susunod");
        public static readonly Say TownsFootnote = new("Not in these towns yet? Join the list anyway and we'll let you know.", "Wala ka sa mga bayang ito? Sumali pa rin, sasabihan ka namin.");
    }

    public static class Trust
    {
        public static readonly Say BadgesTitle = new("What we checked", "Ang na-check namin");
        public static readonly Say BadgesText = new("Workers can add a government ID, TESDA or past-project badge. A person checks each one, with AI help on IDs.", "Pwedeng maglagay si Kuya o Ate ng government ID, TESDA o past-project badge. Tao ang nagche-check, may tulong ng AI sa ID.");
        public static readonly Say PrivateTitle = new("Chat first, number later", "Chat muna, number mamaya");
        public static readonly Say PrivateText = new("Talk in the app. Numbers show only once the household picks a worker.", "Mag-usap sa app. Lalabas lang ang number pag may napili na.");
        public static readonly Say ReportTitle = new("Report and block", "Report at block");
        public static readonly Say ReportText = new("On every profile and chat. We answer reports within 5 business days.", "Nasa bawat profile at chat. Sinasagot namin ang report sa loob ng 5 business days.");
        public static readonly Say Reviews = new("Reviews come after the work, from both sides.", "Ang reviews, pagkatapos ng gawa, galing sa magkabilang side.");
    }

    public static class Faq
    {
        public static readonly (Say Q, Say A)[] Items =
        [
            (new("Is Tarbaho free?", "Libre ba ang Tarbaho?"),
             new("Yes. Free to post and free to list.", "Oo. Libre mag-post at libre mag-lista.")),
            (new("What can I find on Tarbaho?", "Ano'ng makikita sa Tarbaho?"),
             new("Aircon cleaning, repair, freon and installation; construction: plumbers (tubero), electricians, carpenters (karpintero), masons, tiles, welding and roof repair; pest control for termites (anay), cockroaches, rats and mosquitoes; errands like house cleaning, grocery and PasaBuy; car mechanics (mekaniko); and IT: computers, software and networks.",
                 "Linis aircon, repair, pa-freon at pakabit; construction: tubero, electrician, karpintero, mason, tiles, welding at ayos bubong; pest control sa anay, ipis, daga at lamok; errands gaya ng linis bahay, pa-grocery at PasaBuy; mekaniko ng kotse; at IT: computer, software at network.")),
            (new("How do I pay?", "Paano magbayad?"),
             new("You pay the worker directly after the work, at the price you both agreed. The money never passes through the app.", "Diretso mong babayaran si Kuya o Ate pagkatapos ng gawa, sa presyong napagkasunduan. Hindi dumadaan sa app ang bayad.")),
            (new("Where does it work?", "Saan ito pwede?"),
             new("Angono first, then Antipolo and Taytay in Rizal.", "Angono muna, tapos Antipolo at Taytay sa Rizal.")),
            (new("When can I use it?", "Kailan ko magagamit?"),
             new("iPhone: the beta is open now on TestFlight. Android: soon; join the early access list and we'll email your invite.", "iPhone: bukas na ang beta sa TestFlight. Android: malapit na; sumali sa early access at i-email namin ang invite mo.")),
            (new("How are workers checked?", "Paano chine-check ang workers?"),
             new("Workers can send a government ID, TESDA certificate or past project. A person looks at each one; AI helps read IDs. Each profile shows what we checked, and reviews come after the work.",
                 "Pwedeng magpadala ang workers ng government ID, TESDA certificate o past project. Tao ang tumitingin sa bawat isa; tumutulong ang AI sa pagbasa ng ID. Makikita sa profile ang na-check namin, at may reviews pagkatapos ng gawa.")),
            (new("What do you do with my email?", "Ano'ng gagawin n'yo sa email ko?"),
             new("Only to send your early access invite (Play Store testing or TestFlight) and tell you when Tarbaho opens. No spam, never sold. Message our Facebook Page and we delete it.",
                 "Para lang ipadala ang early access invite mo (Play Store testing o TestFlight) at sabihan ka pag bukas na ang Tarbaho. Walang spam, hindi ibinebenta. Mag-message sa Facebook Page namin at buburahin namin.")),
        ];
    }

    public static class Repeat
    {
        public static readonly Say Headline = new("Ready when Angono is", "Handa na pag bukas sa Angono");
        public static readonly Say Sub = new("Households and workers: get on the list now.", "Nagpapagawa man o worker: sumali na sa listahan.");
        public static readonly Say Button = new("Get early access", "Mag-early access");
        public static readonly Say ThanksHeadline = new("You're on the list. Salamat!", "Nasa listahan ka na. Salamat!");
        public static readonly Say ThanksSub = new("Know a Kuya, Ate or neighbor in Angono? Send them the link.", "May kakilalang Kuya, Ate o kapitbahay sa Angono? I-send ang link.");
        public static readonly Say Share = new("Share", "I-share");
        public static readonly Say Iphone = new("iPhone beta", "iPhone beta");
    }

    public static class NotFound
    {
        public static readonly Say Title = new("This page isn't here.", "Wala ang page na ito.");
        public static readonly Say Home = new("Go to Tarbaho", "Punta sa Tarbaho");
    }

    public static class Footer
    {
        public static readonly Say Tagline = new("Offer your skills, or find a Kuya or Ate nearby.", "Umextra para sa Pamilya, o maghanap ng Kuya o Ate malapit sa'yo.");
        public static readonly Say Line = new("A free local directory in Rizal.", "Libreng local directory sa Rizal.");
        public static readonly Say Privacy = new("Privacy", "Privacy");
        public static readonly Say Terms = new("Terms", "Terms");
        public static readonly Say Copyright = new("© 2026 Tarbaho", "© 2026 Tarbaho");
    }
}
