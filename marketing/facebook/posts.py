# The Facebook posts. build.py turns each into a 1080x1080 feed image and a 1080x1920 story, and writes POSTS.md.
#
# Voice: how Filipinos actually talk. Short. Hook in the first line, then the problem, then Tarbaho.
#   Households (Seekers): everyday Taglish ("Sobrang init", "Barado? Tulo?"), never textbook English.
#   Skilled workers: Tagalog, in the trade's own words (Maestro, kabo, arawan, pakyaw, bale; walang lamig,
#   general cleaning, pull-out, freon).
#   "Errands" is "Pakisuyo" here: bayad ng bills, grocery, padala.
#
# stage = the reader's awareness (Eugene Schwartz):
#   unaware   they don't see a problem yet          -> show them one
#   problem   they feel it, don't know a better way -> make it sting, hint there's a fix
#   solution  they use the usual fixes (kakilala, FB group, poster sa poste) -> why Tarbaho beats those
#   product   they know Tarbaho, not sold yet       -> the difference
#   most      ready                                  -> the offer, download now
# season = when to post it: "all", "Mar-May" (init, aircon), "Jun-Oct" (ulan, baha, bagyo), "Nov-Dec" (Pasko),
#   "May-Jun" (enrollment, lipat-bahay).
#
# Rules: never job/hire/employment/minimum wage; never vouch for a worker. Price claims: "the lowest offer near
# you" is fine (they compare offers and pick); workers are Kuya/Ate/Manong/Manang/Maestro, never "laborers".

POSTS = [
    # ======================= Households (Taglish) =======================
    dict(
        id="h1-aircon", side="household", stage="problem", season="Mar-May", icon="aircon",
        problem="Sobrang init. Sira pa ang aircon.",
        sub="Walang lamig, tumutulo pa. Ang technician, seen lang.",
        answer="I-post sa Tarbaho. Mga aircon tech na malapit sa'yo ang mag-o-offer ng presyo. Ikaw ang pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Sobrang init. Sira pa ang aircon. 🥵\n\n"
            "Walang lamig, tumutulo pa. 'Yung technician, seen lang.\n\n"
            "Sa Tarbaho, i-post mo lang. Mga aircon tech sa Angono, Antipolo at Taytay ang mag-o-offer ng presyo. "
            "Ikaw ang pipili. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h12-aircon-pms", side="household", stage="unaware", season="Mar-May", icon="thermometer",
        problem="Kailan mo huling pinalinis ang aircon?",
        sub="Kung hindi mo na maalala, baka kaya mahina na ang lamig.",
        answer="Mag-book ng general cleaning sa Tarbaho bago pa uminit nang todo. Kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Kailan mo huling pinalinis ang aircon? 🤔\n\n"
            "Kung hindi mo na maalala, baka kaya mahina na ang lamig.\n\n"
            "Sa Tarbaho, hanapin ang aircon tech na malapit sa'yo para sa general cleaning o chemical wash. "
            "Kita mo ang presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #AirconCleaning #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h2-plumbing", side="household", stage="problem", season="all", icon="toilet",
        problem="Barado na naman yung CR?",
        sub="Tapos tulo pa ang gripo. Laging sa pinaka-busy mong araw.",
        answer="Sa Tarbaho, hanapin ang tubero sa bayan mo. Kita mo ang presyo, reviews, at gaano kalapit.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Barado na naman yung CR? 🚽\n\n"
            "Tapos tulo pa ang gripo. Laging sa pinaka-busy mong araw.\n\n"
            "Sa Tarbaho, kita mo agad ang mga tubero sa Angono, Antipolo at Taytay: presyo, reviews, at gaano kalapit. "
            "Chat ka muna sa app, hindi mo kailangang ibigay ang number mo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h3-overprice", side="household", stage="product", season="all", icon="receipt",
        problem="₱800 daw. Naging ₱2,000.",
        sub="Tapos na ang trabaho, wala ka nang magawa.",
        answer="Sa Tarbaho, ikaw ang nagsasabi ng budget. Kita mo lahat ng offer bago ka pumili, at bayad pagkatapos lang.",
        cta="Download Tarbaho. Lowest offer, ikaw pipili.",
        caption=(
            "₱800 daw. Naging ₱2,000. 😤\n\n"
            "Tapos na ang trabaho, wala ka nang magawa.\n\n"
            "Sa Tarbaho, ikaw ang nagsasabi ng budget. Ang mga skilled worker na malapit sa'yo ang mag-o-offer. "
            "Kita mo lahat ng presyo bago ka pumili, at magbabayad ka lang pagkatapos ng trabaho, sa presyong pinagkasunduan.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h14-inflation", side="household", stage="solution", season="all", icon="cart",
        problem="Mahal na ang bilihin.",
        sub="'Wag nang dagdagan ng overpriced na repair.",
        answer="Sa Tarbaho, ilang offer ang darating mula sa mga malapit sa'yo. Piliin ang pasok sa budget.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mahal na ang bilihin. 🛒\n\n"
            "'Wag nang dagdagan ng overpriced na repair.\n\n"
            "Sa Tarbaho, i-post mo ang kailangan at ang budget mo. Ilang offer ang darating mula sa mga skilled worker "
            "sa Angono, Antipolo at Taytay. Piliin ang pasok sa bulsa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tipid #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h16-gc", side="household", stage="solution", season="all", icon="chat",
        problem="\"May kilala ba kayong tubero?\"",
        sub="Tinanong mo sa GC. Tatlong araw na, wala pa ring sumasagot.",
        answer="Sa Tarbaho, nandoon na ang mga tubero, electrician at aircon tech sa bayan mo, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "\"May kilala ba kayong tubero?\" 📱\n\n"
            "Tinanong mo sa GC. Tatlong araw na, wala pa ring sumasagot.\n\n"
            "Sa Tarbaho, nandoon na ang mga tubero, electrician, aircon tech at iba pa sa Angono, Antipolo at Taytay. "
            "May presyo at reviews na. 'Di mo na kailangang magtanong-tanong.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h13-pasko", side="household", stage="problem", season="Nov-Dec", icon="calendar",
        problem="Darating na ang bisita sa Pasko.",
        sub="Sira pa rin ang ilaw, ang gripo, at hindi pa napipinturahan.",
        answer="Ipaayos na bago mag-24. I-post sa Tarbaho at hintayin ang mga offer mula sa malapit sa'yo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Darating na ang bisita sa Pasko. 🎄\n\n"
            "Sira pa rin ang ilaw, tumutulo pa ang gripo, at hindi pa napipinturahan ang sala.\n\n"
            "Ipaayos na bago mag-24. Sa Tarbaho, i-post mo ang kailangan at ang mga skilled worker sa Angono, Antipolo "
            "at Taytay ang mag-o-offer ng presyo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h15-baha", side="household", stage="problem", season="Jun-Oct", icon="drop",
        problem="Bumaha na naman?",
        sub="Barado ang kanal, basa ang dingding, ang daming lilinisin.",
        answer="Sa Tarbaho, may tutulong na malapit sa'yo: tubero, linis, at repair pagkatapos ng baha.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Bumaha na naman? 🌊\n\n"
            "Barado ang kanal, basa ang dingding, ang daming lilinisin.\n\n"
            "Sa Tarbaho, hanapin ang tutulong sa Angono, Antipolo at Taytay: tubero para sa kanal, "
            "linis ng bahay, at repair pagkatapos ng baha. Kita mo ang presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Baha #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h7-roof", side="household", stage="problem", season="Jun-Oct", icon="roofing",
        problem="May timba na naman sa sala?",
        sub="Tulo ang bubong. Tapos may bagyo pa raw next week.",
        answer="I-post sa Tarbaho ang problema sa yero o alulod. Mga malapit sa'yo ang mag-o-offer. Ikaw ang pipili.",
        cta="Download Tarbaho. Lowest offer, ikaw pipili.",
        caption=(
            "May timba na naman sa sala? 🌧️\n\n"
            "Tulo ang bubong. Tapos may bagyo pa raw next week.\n\n"
            "Sa Tarbaho, i-post mo ang problema: yero, alulod, waterproofing. Lagyan ng picture at budget. "
            "Mga skilled worker sa Angono, Antipolo at Taytay ang mag-o-offer. Piliin mo, bayad pagkatapos.\n\n"
            "I-download bago pa umulan. 👉 {link}\n\n#Tarbaho #Bubong #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h5-breaker", side="household", stage="problem", season="all", icon="electrical",
        problem="Nag-trip na naman ang breaker?",
        sub="Akala mo brownout. Kayo lang pala.",
        answer="Sa Tarbaho, i-post ang problema. Mga electrician na malapit sa'yo ang mag-o-offer ng presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nag-trip na naman ang breaker? ⚡\n\n"
            "Akala mo brownout. Kayo lang pala. At ayaw mong galawin, delikado.\n\n"
            "Sa Tarbaho, i-post mo ang problema at kung saan ka sa Angono, Antipolo o Taytay. "
            "Mga electrician na malapit ang mag-o-offer. Ikaw ang pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h8-solar", side="household", stage="unaware", season="Mar-May", icon="solar",
        problem="Ang laki na naman ng Meralco?",
        sub="Buong summer naka-aircon. Ramdam sa bill.",
        answer="Sa Tarbaho, magtanong sa mga solar panel tech sa bayan mo. Kumuha ng presyo, ikumpara.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ang laki na naman ng Meralco? 💡\n\n"
            "Buong summer naka-aircon. Ramdam sa bill.\n\n"
            "Iniisip mo na mag-solar? Sa Tarbaho, magtanong sa mga solar panel tech sa Angono, Antipolo at Taytay. "
            "Kumuha ng presyo at ikumpara bago pumili. Pwede rin pa-linis ng panels mo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Solar #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h9-pakisuyo", side="household", stage="unaware", season="all", icon="errand",
        problem="Ubos ang Sabado mo sa pila?",
        sub="Bayad ng bills, grocery, padala. Wala nang pahinga.",
        answer="Sa Tarbaho, may Ate at Kuya na malapit sa'yo na pwedeng pakisuyuan: bills, grocery, PasaBuy, padala.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ubos ang Sabado mo sa pila? 🧾\n\n"
            "Bayad ng kuryente at tubig, grocery, padala ng parcel. Wala ka nang pahinga.\n\n"
            "Sa Tarbaho, may mga Ate at Kuya sa Angono, Antipolo at Taytay na pwede mong pakisuyuan: "
            "bayad ng bills, grocery, PasaBuy, padala at kuha ng dokumento. Kita mo ang presyo nila.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #PasaBuy #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h4-cleaning", side="household", stage="problem", season="all", icon="house-clean",
        problem="Pagod sa trabaho. Makalat pa sa bahay.",
        sub="Labahin, hugasin, linisin. Hindi na matapos.",
        answer="Sa Tarbaho, hanapin si Ate na malapit sa'yo para sa general cleaning. Kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Pagod sa trabaho. Makalat pa sa bahay. 🧹\n\n"
            "Labahin, hugasin, linisin. Hindi na matapos.\n\n"
            "Sa Tarbaho, may mga Ate at Kuya sa Angono, Antipolo at Taytay para sa general cleaning ng bahay. "
            "Kita mo ang presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Linis #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h6-mechanic", side="household", stage="problem", season="all", icon="mechanic",
        problem="Ayaw mag-start ng sasakyan?",
        sub="May lakad ka pa. Ang talyer, malayo at punuan.",
        answer="Sa Tarbaho, hanapin si Manong Mekaniko na malapit sa'yo. Presyo at reviews, kita mo agad.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ayaw mag-start ng sasakyan? 🚗\n\n"
            "May lakad ka pa. Ang talyer, malayo at punuan.\n\n"
            "Sa Tarbaho, may mga mekaniko sa Angono, Antipolo at Taytay. Kita mo ang presyo, reviews, at gaano kalapit. "
            "Chat ka muna sa app.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Mekaniko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h10-it", side="household", stage="problem", season="all", icon="it",
        problem="Mabagal ang WiFi? Ayaw bumukas ng laptop?",
        sub="May online class ang anak mo. May meeting ka bukas.",
        answer="Sa Tarbaho, may IT na malapit sa'yo: WiFi, laptop, printer, CCTV. Presyo muna bago chat.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mabagal ang WiFi? Ayaw bumukas ng laptop? 💻\n\n"
            "May online class ang anak mo. May meeting ka bukas.\n\n"
            "Sa Tarbaho, may mga IT sa Angono, Antipolo at Taytay para sa WiFi setup, laptop at PC repair, printer at CCTV. "
            "Kita mo ang presyo at reviews nila.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #IT #CCTV #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h11-tiles", side="household", stage="unaware", season="all", icon="tiles",
        problem="Ilang taon mo nang tinitiis ang CR?",
        sub="Basag na tiles, may tagas. \"Saka na\" palagi.",
        answer="I-post sa Tarbaho ang gusto mong ipagawa, may picture at budget. Mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Lowest offer, ikaw pipili.",
        caption=(
            "Ilang taon mo nang tinitiis ang CR? 🚿\n\n"
            "Basag na tiles, may tagas. \"Saka na\" palagi kasi 'di mo alam kung magkano aabutin.\n\n"
            "Sa Tarbaho, i-post mo ang gusto mong ipagawa: tiles, masonry, tubo. Lagyan ng picture at budget. "
            "Mga skilled worker sa Angono, Antipolo at Taytay ang mag-o-offer. Piliin mo, bayad pagkatapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Renovation #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h17-lipat", side="household", stage="problem", season="May-Jun", icon="box",
        problem="Lilipat ng bahay?",
        sub="Kabit ng aircon, ilaw, kurtina. Paayos ng gripo. Linis bago pumasok.",
        answer="Sa Tarbaho, isang app lang: aircon tech, electrician, tubero, linis. Lahat malapit sa'yo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Lilipat ng bahay? 📦\n\n"
            "Kabit ng aircon, ilaw, kurtina. Paayos ng gripo. Linis bago pumasok.\n\n"
            "Sa Tarbaho, isang app lang para sa lahat: aircon tech, electrician, tubero, karpintero at linis "
            "sa Angono, Antipolo at Taytay. Kita mo ang presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #LipatBahay #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h18-why", side="household", stage="product", season="all", icon="search",
        problem="Bakit Tarbaho, hindi FB group?",
        sub="Kita mo na ang presyo. Malapit lang sila. Bayad pagkatapos.",
        answer="Mga skilled worker lang sa Angono, Antipolo at Taytay. Ikaw ang pipili ng offer. Libre ang app.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Bakit Tarbaho, hindi FB group? 🤷\n\n"
            "✅ Kita mo na ang presyo bago ka mag-message\n"
            "✅ Malapit lang sila: Angono, Antipolo, Taytay\n"
            "✅ Mag-post ka, sila ang mag-o-offer. Ikaw ang pipili\n"
            "✅ Chat sa app muna, number mamaya na\n"
            "✅ Bayad pagkatapos ng trabaho\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h19-now", side="household", stage="most", season="all", icon="done",
        problem="Lowest offer na malapit sa'yo.",
        sub="Mag-post. Ikumpara. Pumili. Bayad pagkatapos.",
        answer="Libre ang Tarbaho. Aircon, tubero, electrician, construction, linis, pakisuyo at iba pa.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Lowest offer na malapit sa'yo. 👇\n\n"
            "Mag-post. Ikumpara. Pumili. Bayad pagkatapos.\n\n"
            "Aircon, tubero, electrician, construction, linis, pakisuyo at iba pa, sa Angono, Antipolo at Taytay.\n\n"
            "Download Tarbaho ngayon. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    # ======================= Skilled workers (Tagalog) =======================
    dict(
        id="w2-between", side="worker", stage="problem", season="all", icon="foreman",
        problem="Wala nang project. Nag-aantay ng tawag ni Maestro.",
        sub="Isang linggo na. Wala pa rin. Tuloy ang gastos sa bahay.",
        answer="Habang naghihintay, may mga bahay sa bayan mo na kailangan ng karpintero, mason, tubero at electrician.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Wala nang project. Nag-aantay ng tawag ni Maestro. 📵\n\n"
            "Isang linggo na. Wala pa rin. Tuloy ang gastos sa bahay.\n\n"
            "Habang naghihintay, may mga bahay sa Angono, Antipolo at Taytay na nagpo-post ng kailangan nila: "
            "karpintero, mason, tiles, welding, tubero, electrician. Kita mo sa Requests, ikaw ang mag-o-offer ng presyo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w7-gas", side="worker", stage="problem", season="all", icon="gas",
        problem="Mahal ng gas. Malayo pa ang customer.",
        sub="Ang laki ng napupunta sa biyahe. Kulang na ang uuwi.",
        answer="Sa Tarbaho, ikaw ang pipili kung hanggang saan ka lang. Mga customer na malapit sa'yo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Mahal ng gas. Malayo pa ang customer. ⛽\n\n"
            "Ang laki ng napupunta sa biyahe. Kulang na ang naiuuwi.\n\n"
            "Sa Tarbaho, ikaw ang nagsasabi kung hanggang saan ka lang pupunta. Ang makikita mo, mga household na "
            "malapit sa'yo sa Angono, Antipolo at Taytay.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Sideline #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w8-bilihin", side="worker", stage="problem", season="all", icon="cart",
        problem="Tumaas ang bilihin. Ang kita, hindi.",
        sub="Bigas, gas, kuryente. Lahat tumaas. Ang arawan mo, ganoon pa rin.",
        answer="Sa Tarbaho, dagdag na customer sa bayan mo. Ikaw ang nagtatakda ng presyo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Tumaas ang bilihin. Ang kita, hindi. 😮‍💨\n\n"
            "Bigas, gas, kuryente. Lahat tumaas. Ang arawan mo, ganoon pa rin.\n\n"
            "Sa Tarbaho, dagdag na customer sa bayan mo. Ikaw ang nagtatakda ng presyo mo. "
            "Ang mga household sa Angono, Antipolo at Taytay ang magme-message sa'yo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w1-extra", side="worker", stage="problem", season="all", icon="wallet",
        problem="Kailangan mo ng extra?",
        sub="Magaling ka sa trabaho mo. Pero kulang pa rin.",
        answer="Sa Tarbaho, ang mga household sa bayan mo ang maghahanap sa'yo. Walang komisyon. Sa'yo ang buong bayad.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Kailangan mo ng extra? 💪\n\n"
            "Magaling ka. Pero ang customer mo, puro kakilala lang. Pag walang tumawag, walang kita.\n\n"
            "Sa Tarbaho, gumawa ng libreng profile: serbisyo mo, presyo mo, at mga bayan na pinupuntahan mo. "
            "Ang mga household sa Angono, Antipolo at Taytay ang magme-message sa'yo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Raket #Sideline #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w12-aircon-season", side="worker", stage="solution", season="Mar-May", icon="aircon",
        problem="Aircon tech ka? Peak season na.",
        sub="General cleaning, pull-out, freon, walang lamig. Ang dami nang naghahanap.",
        answer="Sa Tarbaho, kita mo ang mga request sa bayan mo. Mag-offer ng presyo mo, walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Aircon tech ka? Peak season na. ❄️\n\n"
            "General cleaning, pull-out, chemical wash, walang lamig. Ang dami nang naghahanap ngayong summer.\n\n"
            "Sa Tarbaho, kita mo ang mga request ng household sa Angono, Antipolo at Taytay. "
            "Mag-offer ng presyo mo. Pag pinili ka, sila ang magme-message.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #AirconTech #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w10-pasko", side="worker", stage="solution", season="Nov-Dec", icon="calendar",
        problem="Pandagdag pang-Noche Buena?",
        sub="Maraming bahay ang nagpapaayos bago mag-Pasko.",
        answer="Nasa Tarbaho na ang mga request nila: pintura, ilaw, gripo, linis. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Pandagdag pang-Noche Buena? 🎄\n\n"
            "Maraming bahay ang nagpapaayos bago mag-Pasko: pintura, ilaw, gripo, linis.\n\n"
            "Sa Tarbaho, kita mo ang mga request nila sa Angono, Antipolo at Taytay. Mag-offer ng presyo mo. "
            "Pag pinili ka, sila ang magme-message.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w3-waiting", side="worker", stage="solution", season="all", icon="megaphone",
        problem="Pagod ka na mag-antay ng tawag?",
        sub="Ang susunod mong customer, baka nasa kabilang kanto lang.",
        answer="Sa Tarbaho, kita mo ang mga bahay na may kailangang ipagawa malapit sa'yo. Ikaw ang mag-o-offer.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Pagod ka na mag-antay ng tawag? 📱\n\n"
            "Ang susunod mong customer, baka nasa kabilang kanto lang. Hindi ka lang nila kilala.\n\n"
            "Sa Tarbaho, may Requests ka: mga household na malapit sa'yo na may kailangang ipagawa, kasama ang budget nila. "
            "Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. Libre i-download. 👉 {link}\n\n#Tarbaho #Sideline #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w5-welder", side="worker", stage="solution", season="all", icon="weld",
        problem="Welder ka? Gate, grills, railings?",
        sub="Maraming bahay ang nangangailangan. Hindi ka lang nila mahanap.",
        answer="Sa Tarbaho, ilagay ang gawa mo at presyo mo. Ang mga household na malapit ang magme-message.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Welder ka? Gate, grills, railings, trusses? 🔥\n\n"
            "Maraming bahay ang nangangailangan niyan. Hindi ka lang nila kilala.\n\n"
            "Sa Tarbaho, ilagay ang picture ng mga natapos mo, ang presyo mo, at ang mga bayan na pinupuntahan mo. "
            "Ang mga household sa Angono, Antipolo at Taytay ang magme-message sa'yo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Welding #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w6-pakisuyo", side="worker", stage="unaware", season="all", icon="truck",
        problem="May motor ka at may bakanteng oras?",
        sub="Sa pagitan ng mga lakad mo, pwede ka nang kumita.",
        answer="Sa Tarbaho, tumanggap ng pakisuyo sa bayan mo: bills, grocery, PasaBuy, padala.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "May motor ka at may bakanteng oras? 🛵\n\n"
            "Maraming nanay at nagtatrabaho ang walang oras pumila sa bayad-center o mag-grocery.\n\n"
            "Sa Tarbaho, tumanggap ng pakisuyo sa Angono, Antipolo at Taytay: bayad ng bills, grocery, PasaBuy, padala. "
            "Ikaw ang nagtatakda ng presyo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Pakisuyo #PasaBuy #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w4-unknown", side="worker", stage="unaware", season="all", icon="medal",
        problem="Sampung taon ka na. Kapitbahay lang ang nakakakilala.",
        sub="Magaling ang gawa mo. Pero hindi ito nakikita.",
        answer="Sa Tarbaho, ipakita ang gawa mo: picture, reviews, at taon ng karanasan. Ang gawa mo ang magsasalita.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Sampung taon ka na. Kapitbahay lang ang nakakakilala. 🏅\n\n"
            "Magaling ang gawa mo. Pero hindi ito nakikita.\n\n"
            "Sa Tarbaho, ipakita ang gawa mo: picture ng mga natapos mo, reviews ng mga household, at ilang taon ka na. "
            "Mas maraming bahay sa Angono, Antipolo at Taytay ang makakakita sa'yo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #SkilledWorker #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w9-komisyon", side="worker", stage="product", season="all", icon="money",
        problem="May kaltas ba ang bawat bayad sa'yo?",
        sub="Ikaw ang nagpagod. Bakit may hati ang iba?",
        answer="Sa Tarbaho, walang komisyon. Ang bayad ng customer, diretso sa'yo. Buo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "May kaltas ba ang bawat bayad sa'yo? 💸\n\n"
            "Ikaw ang nagpagod. Bakit may hati ang iba?\n\n"
            "Sa Tarbaho, walang komisyon. Ang bayad ng customer, diretso sa'yo. Buo. "
            "Ikaw pa ang nagtatakda ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #WalangKomisyon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w11-now", side="worker", stage="most", season="all", icon="done",
        problem="Libre. Walang komisyon. Malapit lang.",
        sub="Gumawa ng profile ngayon para makita ka ng mga bahay sa bayan mo.",
        answer="Aircon, construction, tubero, electrician, linis, pakisuyo. Angono, Antipolo at Taytay.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Libre. Walang komisyon. Malapit lang. 👇\n\n"
            "Gumawa ng profile ngayon, para makita ka ng mga household sa bayan mo.\n\n"
            "Aircon, construction, tubero, electrician, linis, pakisuyo at iba pa, sa Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
]
