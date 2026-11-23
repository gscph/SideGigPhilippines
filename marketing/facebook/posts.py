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
        answer="I-post sa Tarbaho. Mga aircon tech na malapit sa'yo ang mag-o-offer. Ikaw ang pipili.",
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
        sub="Hindi mo na maalala? Kaya pala mahina na ang lamig.",
        answer="Ipa-general cleaning na bago pa uminit nang todo. Sa Tarbaho, kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Kailan mo huling pinalinis ang aircon? 🤔\n\n"
            "Hindi mo na maalala? Kaya pala mahina na ang lamig.\n\n"
            "Sa Tarbaho, hanapin ang aircon tech na malapit sa'yo para sa general cleaning o chemical wash. "
            "Kita mo ang presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #AirconCleaning #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h2-plumbing", side="household", stage="problem", season="all", icon="toilet",
        problem="Barado na naman yung CR?",
        sub="Tapos tulo pa ang gripo. Laging sa pinaka-busy mong araw.",
        answer="Sa Tarbaho, may tubero sa bayan mo. Kita mo ang presyo, reviews at gaano kalapit.",
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
        sub="Tapos na. Wala ka nang magawa.",
        answer="Sa Tarbaho, ikaw ang nagsasabi ng budget. Kita mo lahat ng offer, at bayad pagkatapos lang.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "₱800 daw. Naging ₱2,000. 😤\n\n"
            "Tapos na. Wala ka nang magawa.\n\n"
            "Sa Tarbaho, ikaw ang nagsasabi ng budget. Ang mga skilled worker na malapit sa'yo ang mag-o-offer. "
            "Kita mo lahat ng presyo bago ka pumili, at magbabayad ka lang pagkatapos ng trabaho, sa presyong pinagkasunduan.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h14-inflation", side="household", stage="solution", season="all", icon="cart",
        problem="Mahal na ang bilihin.",
        sub="'Wag nang dagdagan ng overprice.",
        answer="Sa Tarbaho, mga malapit sa'yo ang mag-o-offer. Piliin ang pasok sa budget.",
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
        sub="Nag-post sa GC. Puro seen.",
        answer="Nasa Tarbaho na ang tubero, electrician at aircon tech sa bayan mo. May presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "\"May kilala ba kayong tubero?\" 📱\n\n"
            "Nag-post sa GC. Puro seen.\n\n"
            "Sa Tarbaho, nandoon na ang mga tubero, electrician, aircon tech at iba pa sa Angono, Antipolo at Taytay. "
            "May presyo at reviews na. 'Di mo na kailangang magtanong-tanong.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h13-pasko", side="household", stage="problem", season="Nov-Dec", icon="calendar",
        problem="Darating na ang bisita sa Pasko.",
        sub="Sira pa ang ilaw. Tulo pa ang gripo. Hindi pa napipinturahan.",
        answer="Ipaayos na bago mag-24. I-post sa Tarbaho, mga malapit sa'yo ang mag-o-offer.",
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
        answer="Sa Tarbaho, may tubero at taga-linis na malapit sa'yo pagkatapos ng baha.",
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
        answer="I-post sa Tarbaho ang sira sa yero o alulod. Mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
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
        answer="I-post sa Tarbaho. Mga electrician na malapit sa'yo ang mag-o-offer ng presyo.",
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
        sub="Buong summer naka-aircon.",
        answer="Sa Tarbaho, may solar tech sa bayan mo. Kita mo ang presyo, ikumpara.",
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
        answer="Sa Tarbaho, pakisuyo kay Ate o Kuya na malapit sa'yo: bills, grocery, PasaBuy, padala.",
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
        answer="Sa Tarbaho, may Ate na malapit sa'yo para sa general cleaning. Kita mo agad ang presyo.",
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
        problem="Mabagal ang WiFi?",
        sub="May online class ang anak mo. May meeting ka bukas.",
        answer="Sa Tarbaho, may IT na malapit sa'yo: WiFi, laptop, printer, CCTV. Presyo muna bago chat.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mabagal ang WiFi? 💻\n\n"
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
        answer="I-post sa Tarbaho, may picture at budget. Mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
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
        sub="Kabit ng aircon at ilaw. Ayos ng gripo. Linis bago pumasok.",
        answer="Sa Tarbaho, isang app lang: aircon tech, electrician, tubero, linis. Lahat malapit sa'yo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Lilipat ng bahay? 📦\n\n"
            "Kabit ng aircon at ilaw. Ayos ng gripo. Linis bago pumasok.\n\n"
            "Sa Tarbaho, isang app lang para sa lahat: aircon tech, electrician, tubero, karpintero at linis "
            "sa Angono, Antipolo at Taytay. Kita mo ang presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #LipatBahay #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h18-why", side="household", stage="product", season="all", icon="search",
        problem="Bakit Tarbaho, hindi FB group?",
        sub="Presyo muna. Malapit lang. Chat sa app. Bayad pagkatapos.",
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
    dict(
        id="h20-pasukan", side="household", stage="problem", season="May-Jun", icon="bulb",
        problem="Pasukan na next week.",
        sub="Sira pa ang ilaw, electric fan at WiFi.",
        answer="Ipaayos na bago ang first day. Sa Tarbaho, mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Pasukan na next week. 🎒\n\n"
            "Sira pa ang ilaw, electric fan at WiFi.\n\n"
            "Ipaayos na bago ang first day. Sa Tarbaho, i-post mo lang: mga electrician at IT sa Angono, "
            "Antipolo at Taytay ang mag-o-offer ng presyo. Ikaw ang pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasukan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h21-bagyo", side="household", stage="unaware", season="Jun-Oct", icon="alert",
        problem="May bagyo raw ngayong linggo.",
        sub="Kumusta ang yero at alulod mo? 'Wag nang hintaying tumulo.",
        answer="Ipa-check na sa Tarbaho. May roofing sa bayan mo, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "May bagyo raw ngayong linggo. 🌀\n\n"
            "Kumusta ang yero at alulod mo? 'Wag nang hintaying tumulo sa sala.\n\n"
            "Ipa-check na habang maaraw pa. Sa Tarbaho, may roofing at construction sa Angono, Antipolo at "
            "Taytay, may presyo at reviews. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Bagyo #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h22-number", side="household", stage="product", season="all", icon="cyber-lock",
        problem="Takot i-post ang number?",
        sub="Hindi lahat ng nagme-message, legit.",
        answer="Sa Tarbaho, chat muna sa app. Lalabas lang ang number kapag pumili ka na ng offer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Takot i-post ang number? 🔒\n\n"
            "Tama ka. Hindi lahat ng nagme-message, legit.\n\n"
            "Sa Tarbaho, chat muna sa app. Ang number mo, lalabas lang kapag pumili ka na ng offer. "
            "Mga skilled worker lang sa Angono, Antipolo at Taytay.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h23-tubig", side="household", stage="unaware", season="all", icon="pipe",
        problem="Bakit ang taas ng bill sa tubig?",
        sub="Baka may tagas na hindi mo nakikita.",
        answer="Ipa-check sa tubero sa Tarbaho. Kita mo ang presyo bago ka mag-message.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Bakit ang taas ng bill sa tubig? 💧\n\n"
            "Baka may tagas na hindi mo nakikita. Sa ilalim ng lababo, sa CR, sa tubo sa pader.\n\n"
            "Ipa-check bago lumaki pa. Sa Tarbaho, may tubero sa Angono, Antipolo at Taytay. "
            "Kita mo ang presyo at reviews bago ka mag-message. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h24-bayad", side="household", stage="most", season="all", icon="money",
        problem="Bayad pagkatapos ng trabaho.",
        sub="Mag-post, libre. Ikaw ang pipili ng offer.",
        answer="Aircon, tubero, electrician, construction, linis at pakisuyo, sa Angono, Antipolo at Taytay.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Bayad pagkatapos ng trabaho. ✅\n\n"
            "Mag-post ngayon, libre. Mga skilled worker na malapit sa'yo ang mag-o-offer. Ikaw ang pipili.\n\n"
            "Aircon, tubero, electrician, construction, linis at pakisuyo, sa Angono, Antipolo at Taytay.\n\n"
            "Download Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h25-cctv", side="household", stage="unaware", season="all", icon="gate",
        problem="Sino 'yung nasa gate kagabi?",
        sub="Kung may CCTV ka, alam mo sana.",
        answer="Sa Tarbaho, may IT sa bayan mo na nagkakabit ng CCTV. Kita mo ang presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Sino 'yung nasa gate kagabi? 👀\n\n"
            "Kung may CCTV ka, alam mo sana.\n\n"
            "Sa Tarbaho, may IT sa Angono, Antipolo at Taytay na nagkakabit ng CCTV at nag-aayos ng WiFi. "
            "Kita mo ang presyo at reviews bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #CCTV #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h26-carwash", side="household", stage="problem", season="all", icon="car-wash",
        problem="Ang dumi na naman ng kotse?",
        sub="Wala kang oras pumila sa carwash.",
        answer="Sa Tarbaho, may Kuya na malapit sa'yo na maglilinis ng kotse mo sa bahay mismo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ang dumi na naman ng kotse? 🚗\n\n"
            "Walang oras pumila sa carwash tuwing Linggo.\n\n"
            "Sa Tarbaho, may Kuya sa Angono, Antipolo at Taytay na pumupunta sa bahay para maglinis ng sasakyan. "
            "Kita mo agad ang presyo. Bayad pagkatapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #CarWash #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h27-mama", side="household", stage="solution", season="all", icon="home",
        problem="Si Mama, mag-isa sa bahay. Sira ang gripo.",
        sub="Hindi niya alam kung sino ang tatawagan. Malayo ka pa.",
        answer="I-post mo para sa kanya sa Tarbaho. Mga tubero sa bayan niya ang mag-o-offer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Si Mama, mag-isa sa bahay. Sira ang gripo. 🏠\n\n"
            "Hindi niya alam kung sino ang tatawagan. Malayo ka pa.\n\n"
            "Sa Tarbaho, ikaw na ang mag-post para sa kanya. Mga tubero sa Angono, Antipolo at Taytay ang "
            "mag-o-offer. Chat muna sa app, kita mo ang presyo at reviews. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h28-kisame", side="household", stage="problem", season="Jun-Oct", icon="home-repair",
        problem="May mantsa na naman sa kisame?",
        sub="Tulo 'yan galing sa bubong. Lalaki pa 'yan ngayong tag-ulan.",
        answer="I-post sa Tarbaho, may picture. Mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "May mantsa na naman sa kisame? 🌧️\n\n"
            "Tulo 'yan galing sa bubong. Lalaki pa 'yan ngayong tag-ulan.\n\n"
            "Sa Tarbaho, i-post ang picture ng kisame at ang budget mo. Mga karpintero at roofing sa Angono, "
            "Antipolo at Taytay ang mag-o-offer. Ikaw ang pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #TagUlan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h29-xmas-lights", side="household", stage="problem", season="Nov-Dec", icon="bolt",
        problem="Patay-sindi na naman ang Christmas lights?",
        sub="Baka hindi lang ilaw. Baka saksakan na.",
        answer="Ipa-check bago mag-Noche Buena. Mga electrician na malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Patay-sindi na naman ang Christmas lights? 🎄\n\n"
            "Baka hindi lang ilaw. Baka 'yung saksakan o wiring na.\n\n"
            "Ipa-check bago mag-Noche Buena. Sa Tarbaho, i-post mo lang: mga electrician sa Angono, Antipolo "
            "at Taytay ang mag-o-offer ng presyo. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h30-damo", side="household", stage="problem", season="Jun-Oct", icon="garden",
        problem="Ang taas na ng damo sa bakuran?",
        sub="Tag-ulan kasi. Isang linggo, gubat na ulit.",
        answer="Sa Tarbaho, may Kuya na malapit sa'yo para sa bakuran. Kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ang taas na ng damo sa bakuran? 🌿\n\n"
            "Tag-ulan kasi. Isang linggo lang, gubat na ulit. Tapos may lamok pa.\n\n"
            "Sa Tarbaho, may Kuya sa Angono, Antipolo at Taytay para sa gardening at linis ng bakuran. "
            "Kita mo agad ang presyo. Bayad pagkatapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h31-printer", side="household", stage="problem", season="all", icon="it",
        problem="Ayaw mag-print. Deadline bukas.",
        sub="Project ng anak. Printer, blinking lang.",
        answer="Sa Tarbaho, may IT na malapit sa'yo: printer, laptop, WiFi. Kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ayaw mag-print. Deadline bukas. 🖨️\n\n"
            "Project ng anak. Printer, blinking lang.\n\n"
            "Sa Tarbaho, may IT sa Angono, Antipolo at Taytay para sa printer, laptop at WiFi. "
            "Kita mo agad ang presyo at reviews. Bayad pagkatapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h32-magkano", side="household", stage="product", season="Mar-May", icon="checklist",
        problem="Magkano ba talaga ang linis ng aircon?",
        sub="Sa FB, \"PM for price\" lagi ang sagot.",
        answer="Sa Tarbaho, nakalagay na ang presyo sa bawat profile. Ikumpara bago ka mag-message.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Magkano ba talaga ang linis ng aircon? ❄️\n\n"
            "Sa FB, \"PM for price\" lagi ang sagot.\n\n"
            "Sa Tarbaho, nakalagay na ang presyo sa bawat profile ng aircon tech sa Angono, Antipolo at Taytay. "
            "Ikumpara bago ka mag-message. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h33-lababo", side="household", stage="problem", season="all", icon="sink",
        problem="Amoy kanal na naman ang lababo?",
        sub="Hindi 'yan madadaan sa zonrox lang.",
        answer="Sa Tarbaho, may tubero na malapit sa'yo. Kita mo agad ang presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Amoy kanal na naman ang lababo? 🤢\n\n"
            "Hindi 'yan madadaan sa zonrox lang. Baka barado na ang tubo o sira ang trap.\n\n"
            "Sa Tarbaho, may tubero sa Angono, Antipolo at Taytay. Kita mo ang presyo at reviews bago ka "
            "mag-message. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h34-pasko-linis", side="household", stage="solution", season="Nov-Dec", icon="house-clean",
        problem="General cleaning bago mag-Pasko?",
        sub="Kisame, bintana, kurtina. Ang dami!",
        answer="Sa Tarbaho, may Ate at Kuya na malapit sa'yo para sa general cleaning. Kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "General cleaning bago mag-Pasko? 🎄\n\n"
            "Kisame, bintana, kurtina, sa ilalim ng ref. Hindi mo 'yan kaya mag-isa.\n\n"
            "Sa Tarbaho, may Ate at Kuya sa Angono, Antipolo at Taytay para sa general cleaning. "
            "Kita mo agad ang presyo. Bayad pagkatapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h35-gate", side="household", stage="problem", season="all", icon="weld",
        problem="Kumakalampag na ang gate?",
        sub="Kalawang na, lumalaylay pa.",
        answer="I-post sa Tarbaho, may picture. Mga welder na malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "Kumakalampag na ang gate? 🚪\n\n"
            "Kalawang na, lumalaylay pa. Hirap nang isara tuwing gabi.\n\n"
            "Sa Tarbaho, i-post ang picture ng gate at ang budget mo. Mga welder sa Angono, Antipolo at "
            "Taytay ang mag-o-offer. Ikaw ang pipili. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Welding #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h36-saksakan", side="household", stage="unaware", season="all", icon="electrical",
        problem="Mainit ang saksakan?",
        sub="Hindi 'yan normal. Delikado 'yan.",
        answer="Ipa-check sa electrician na malapit sa'yo. Sa Tarbaho, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mainit ang saksakan? 🔌\n\n"
            "Hindi 'yan normal. Lalo na kung nakasaksak ang aircon, ref o plantsa. 'Wag nang hintaying umusok.\n\n"
            "Ipa-check sa electrician. Sa Tarbaho, may electrician sa Angono, Antipolo at Taytay, may presyo "
            "at reviews. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h37-lubog", side="household", stage="problem", season="Jun-Oct", icon="mechanic",
        problem="Nalubog ang kotse sa baha?",
        sub="Ayaw na mag-start. Basa ang makina.",
        answer="Sa Tarbaho, may mekaniko na malapit sa'yo. Kita mo ang presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nalubog ang kotse sa baha? 🌊🚗\n\n"
            "Ayaw na mag-start. Basa ang makina. Hindi mo pa madala sa talyer.\n\n"
            "Sa Tarbaho, may mekaniko sa Angono, Antipolo at Taytay. Kita mo ang presyo at reviews bago ka "
            "mag-message. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Baha #Mekaniko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h38-offers", side="household", stage="product", season="all", icon="megaphone",
        problem="Isang post. Ilang offer.",
        sub="Wala nang message isa-isa.",
        answer="I-post ang kailangan mo sa Tarbaho. Mga malapit sa'yo ang mag-o-offer. Ikaw ang pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Isang post. Ilang offer. 📣\n\n"
            "Hindi na kailangang mag-message isa-isa sa sampung page.\n\n"
            "Sa Tarbaho, i-post ang kailangan mo, may picture at budget. Mga skilled worker sa Angono, Antipolo "
            "at Taytay ang mag-o-offer ng presyo. Ikumpara, pumili, at bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h39-bitak", side="household", stage="unaware", season="all", icon="masonry",
        problem="May bitak na ang pader?",
        sub="Maliit pa ngayon. Lalaki 'yan.",
        answer="Ipa-check sa mason sa Tarbaho. Kita mo ang presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "May bitak na ang pader? 🧱\n\n"
            "Maliit pa ngayon. Lalaki 'yan, lalo na pag tag-ulan at may pumapasok na tubig.\n\n"
            "Ipa-check habang maaga. Sa Tarbaho, may mason sa Angono, Antipolo at Taytay, may presyo at reviews. "
            "Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h40-pasabuy", side="household", stage="problem", season="all", icon="bag",
        problem="Walang oras mamalengke?",
        sub="Ubos ang lunch break sa pila.",
        answer="Sa Tarbaho, may Ate at Kuya na malapit sa'yo para sa PasaBuy at grocery.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Walang oras mamalengke? 🛍️\n\n"
            "Ubos ang lunch break sa pila. Pag-uwi, sarado na.\n\n"
            "Sa Tarbaho, may Ate at Kuya sa Angono, Antipolo at Taytay na pwedeng pakisuyuan: PasaBuy, grocery, "
            "bayad ng bills. Kita mo agad ang presyo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #PasaBuy #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h41-wfh", side="household", stage="problem", season="all", icon="chip",
        problem="Nag-lag na naman sa meeting?",
        sub="WFH ka. Ang WiFi, hindi.",
        answer="Sa Tarbaho, may IT na malapit sa'yo para sa WiFi at router. Kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nag-lag na naman sa meeting? 💻\n\n"
            "WFH ka. Ang WiFi, hindi. Mahina sa kwarto, patay sa sala.\n\n"
            "Sa Tarbaho, may IT sa Angono, Antipolo at Taytay para sa WiFi, router at extender. "
            "Kita mo agad ang presyo at reviews. Bayad pagkatapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #WFH #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h42-pasko-now", side="household", stage="most", season="Nov-Dec", icon="checklist",
        problem="Pasko na. Ipaayos na.",
        sub="Mag-post ngayon. Libre.",
        answer="Ilaw, gripo, pintura, linis. Mga malapit sa'yo ang mag-o-offer. Bayad pagkatapos.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Pasko na. Ipaayos na. 🎄\n\n"
            "Mag-post ngayon, libre. Ilaw, gripo, pintura, general cleaning.\n\n"
            "Mga skilled worker sa Angono, Antipolo at Taytay ang mag-o-offer. Ikaw ang pipili. "
            "Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h43-ingay", side="household", stage="problem", season="Mar-May", icon="aircon",
        problem="Maingay na ang aircon?",
        sub="Parang may kumakalampag sa loob.",
        answer="Ipa-check sa aircon tech na malapit sa'yo. Sa Tarbaho, kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Maingay na ang aircon? 😵\n\n"
            "Parang may kumakalampag sa loob. Tapos humihina pa ang lamig.\n\n"
            "Ipa-check bago tuluyang masira sa gitna ng summer. Sa Tarbaho, may aircon tech sa Angono, "
            "Antipolo at Taytay. Kita mo agad ang presyo at reviews.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h44-lola", side="household", stage="unaware", season="all", icon="toilet",
        problem="Madulas ang CR ni Lola?",
        sub="Lagyan ng hawakan bago pa madulas.",
        answer="Sa Tarbaho, may karpintero at tile setter na malapit sa'yo. Kita mo ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Madulas ang CR ni Lola? 👵\n\n"
            "Lagyan ng hawakan at anti-slip na tiles bago pa siya madulas.\n\n"
            "Sa Tarbaho, may karpintero at tile setter sa Angono, Antipolo at Taytay. Kita mo ang presyo "
            "at reviews bago ka mag-message. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h45-budget", side="household", stage="product", season="all", icon="wallet",
        problem="₱1,500 lang ang budget?",
        sub="I-post mo. Sila ang mag-o-offer.",
        answer="Sa Tarbaho, ilagay ang budget mo. Ikumpara ang offers, at bayad pagkatapos ng trabaho.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "₱1,500 lang ang budget? 💸\n\n"
            "I-post mo. Sila ang mag-o-offer.\n\n"
            "Sa Tarbaho, ilagay ang kailangan mo at ang budget mo. Mga skilled worker sa Angono, Antipolo at "
            "Taytay ang mag-o-offer ng presyo. Ikumpara, pumili, at bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h46-yero", side="household", stage="problem", season="Jun-Oct", icon="roofing",
        problem="Natanggal ang yero sa bagyo?",
        sub="Bukas pa ang butas. Uulan ulit.",
        answer="I-post sa Tarbaho, may picture. Mga roofing na malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "Natanggal ang yero sa bagyo? 🌀\n\n"
            "Bukas pa ang butas. Uulan ulit mamaya.\n\n"
            "Sa Tarbaho, i-post ang picture ng bubong at ang budget mo. Mga roofing at construction sa Angono, "
            "Antipolo at Taytay ang mag-o-offer. Ikaw ang pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Bagyo #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h47-shower", side="household", stage="problem", season="all", icon="drop",
        problem="Mahina ang tubig sa shower?",
        sub="Patak-patak lang. Late ka na.",
        answer="Ipa-check sa tubero na malapit sa'yo. Sa Tarbaho, kita mo agad ang presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mahina ang tubig sa shower? 🚿\n\n"
            "Patak-patak lang. Late ka na sa trabaho.\n\n"
            "Baka barado ang shower head o may problema sa tubo. Sa Tarbaho, may tubero sa Angono, Antipolo "
            "at Taytay. Kita mo agad ang presyo at reviews. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h48-reviews", side="household", stage="product", season="all", icon="people",
        problem="Kita mo muna ang reviews.",
        sub="Bago ka mag-message, alam mo na.",
        answer="Sa Tarbaho, may presyo, reviews at picture ng gawa sa bawat profile. Ikaw ang pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Kita mo muna ang reviews. ⭐\n\n"
            "Bago ka mag-message, alam mo na: magkano, gaano kalapit, at ano ang sabi ng ibang bahay.\n\n"
            "Sa Tarbaho, may presyo, reviews at picture ng gawa sa bawat profile ng skilled worker sa Angono, "
            "Antipolo at Taytay. Ikaw ang pipili. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h49-pullout", side="household", stage="problem", season="May-Jun", icon="aircon",
        problem="Lilipat, kasama ang aircon?",
        sub="Pull-out dito, kabit doon.",
        answer="Sa Tarbaho, mga aircon tech na malapit sa'yo ang mag-o-offer. Ikaw ang pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Lilipat, kasama ang aircon? 📦❄️\n\n"
            "Pull-out dito, kabit doon. Baka kailangan pang i-check ang freon.\n\n"
            "Sa Tarbaho, i-post mo lang. Mga aircon tech sa Angono, Antipolo at Taytay ang mag-o-offer ng "
            "presyo. Ikaw ang pipili. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #LipatBahay #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h50-bills", side="household", stage="unaware", season="all", icon="document",
        problem="Nakalimutan ang due date?",
        sub="Busy lang. Pero totoo ang multa.",
        answer="Sa Tarbaho, may Ate at Kuya na pwedeng pakisuyuan sa bayad ng bills.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nakalimutan ang due date? 📄\n\n"
            "Busy lang. Pero totoo ang multa, at ang disconnection.\n\n"
            "Sa Tarbaho, may Ate at Kuya sa Angono, Antipolo at Taytay na pwedeng pakisuyuan: bayad ng bills, "
            "padala, documents. Kita mo agad ang presyo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h51-basa-saksakan", side="household", stage="unaware", season="Jun-Oct", icon="bolt",
        problem="Nabasa ang saksakan?",
        sub="'Wag munang buksan ang breaker.",
        answer="Ipa-check muna sa electrician na malapit sa'yo. Sa Tarbaho, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nabasa ang saksakan? ⚡\n\n"
            "'Wag munang buksan ang breaker. Delikado.\n\n"
            "Ipa-check muna sa electrician. Sa Tarbaho, may electrician sa Angono, Antipolo at Taytay, may "
            "presyo at reviews. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Baha #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h52-sabado", side="household", stage="solution", season="all", icon="calendar",
        problem="Sabado lang ang free mo?",
        sub="Doon mo na ipagawa lahat.",
        answer="Sa Tarbaho, i-post at piliin ang araw. Mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Sabado lang ang free mo? 📅\n\n"
            "Doon mo na ipagawa lahat: gripo, ilaw, aircon, linis.\n\n"
            "Sa Tarbaho, i-post ang kailangan mo at piliin ang araw. Mga skilled worker sa Angono, Antipolo "
            "at Taytay ang mag-o-offer. Ikaw ang pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h53-gripo", side="household", stage="problem", season="all", icon="plumbing",
        problem="Tulo nang tulo ang gripo?",
        sub="Sayang ang tubig at bill.",
        answer="Sa Tarbaho, may tubero sa bayan mo. Presyo at reviews, kita agad.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Tulo nang tulo ang gripo? 💧\n\n"
            "Sayang ang tubig at ang bill. Gabi-gabi pang naririnig.\n\n"
            "Sa Tarbaho, may tubero sa Angono, Antipolo at Taytay. Kita mo agad ang presyo at reviews. "
            "Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h54-pipili", side="household", stage="most", season="all", icon="done",
        problem="Ikaw ang pipili.",
        sub="Hindi ang ahente. Ikaw.",
        answer="Mag-post sa Tarbaho ngayon, libre. Mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Ikaw ang pipili. ✅\n\n"
            "Hindi ang ahente. Ikaw. Kita mo ang presyo, reviews at gaano kalapit bago ka pumili.\n\n"
            "Mag-post sa Tarbaho ngayon, libre. Mga skilled worker sa Angono, Antipolo at Taytay ang "
            "mag-o-offer. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h55-bunso", side="household", stage="solution", season="May-Jun", icon="home-repair",
        problem="Kwarto ni bunso?",
        sub="Pintura, ilaw, cabinet.",
        answer="I-post ang kwarto, may picture at budget. Mga malapit sa'yo ang mag-o-offer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "Kwarto ni bunso? 🧸\n\n"
            "Pintura, ilaw, cabinet, bago mag-pasukan.\n\n"
            "Sa Tarbaho, i-post ang gusto mong ipagawa, may picture at budget. Mga skilled worker sa Angono, "
            "Antipolo at Taytay ang mag-o-offer. Ikaw ang pipili. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h56-kulob", side="household", stage="problem", season="Mar-May", icon="aircon",
        problem="Amoy kulob ang aircon?",
        sub="Kailangan na ng linis.",
        answer="Sa Tarbaho, may aircon tech na malapit sa'yo. Kita mo agad ang presyo ng cleaning.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Amoy kulob ang aircon? 😷\n\n"
            "Kailangan na ng linis. Lalo na't buong araw nakabukas ngayong summer.\n\n"
            "Sa Tarbaho, may aircon tech sa Angono, Antipolo at Taytay para sa general cleaning o chemical wash. "
            "Kita mo agad ang presyo. Bayad pagkatapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h57-ceiling-fan", side="household", stage="solution", season="Mar-May", icon="drill",
        problem="Pakabit ng ceiling fan?",
        sub="Nabili na. Wala pang magkakabit.",
        answer="Sa Tarbaho, mga electrician na malapit sa'yo ang mag-o-offer. Ikaw ang pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Pakabit ng ceiling fan? 🌀\n\n"
            "Nabili na. Nasa kahon pa rin. Wala pang magkakabit.\n\n"
            "Sa Tarbaho, i-post mo lang. Mga electrician sa Angono, Antipolo at Taytay ang mag-o-offer ng "
            "presyo. Ikaw ang pipili. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h58-patong", side="household", stage="product", season="all", icon="receipt",
        problem="Walang patong ng ahente.",
        sub="Diretso ka sa skilled worker.",
        answer="Sa Tarbaho, presyo muna, chat sa app, at bayad pagkatapos ng trabaho.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Walang patong ng ahente. 🧾\n\n"
            "Diretso ka sa skilled worker na malapit sa'yo.\n\n"
            "Sa Tarbaho, kita mo muna ang presyo, chat sa app bago magbigay ng number, at bayad pagkatapos "
            "ng trabaho. Angono, Antipolo at Taytay.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h59-poso", side="household", stage="problem", season="Jun-Oct", icon="toilet",
        problem="Puno na ang poso negro?",
        sub="Amoy na sa buong bahay.",
        answer="Ipa-check sa tubero na malapit sa'yo. Sa Tarbaho, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Puno na ang poso negro? 🚽\n\n"
            "Amoy na sa buong bahay. Tapos umaapaw pa pag malakas ang ulan.\n\n"
            "Sa Tarbaho, may tubero sa Angono, Antipolo at Taytay. Kita mo ang presyo at reviews bago ka "
            "mag-message. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h60-malapit", side="household", stage="product", season="all", icon="pin",
        problem="Malapit lang sila.",
        sub="Taga-bayan mo mismo.",
        answer="Sa Tarbaho, mga skilled worker sa Angono, Antipolo at Taytay lang. Kita mo kung gaano kalapit.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Malapit lang sila. 📍\n\n"
            "Hindi taga-malayo. Taga-bayan mo mismo, kaya mabilis makarating.\n\n"
            "Sa Tarbaho, mga skilled worker sa Angono, Antipolo at Taytay lang. Kita mo kung gaano kalapit, "
            "ang presyo at reviews. Bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    # ======================= Skilled workers (Tagalog) =======================
    dict(
        id="w2-between", side="worker", stage="problem", season="all", icon="foreman",
        problem="Wala nang project. Nag-aantay ng tawag ni Maestro.",
        sub="Isang linggo na. Wala pa rin. Tuloy ang gastos sa bahay.",
        answer="Habang naghihintay, may mga bahay sa bayan mo na kailangan ng karpintero, mason at tubero.",
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
        answer="Sa Tarbaho, ikaw ang pipili kung gaano kalayo ka pupunta. Malapit lang ang customer.",
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
        sub="Bigas, gas, kuryente, tumaas lahat. Ang arawan mo, hindi.",
        answer="Sa Tarbaho, dagdag na customer sa bayan mo. Ikaw ang nagtatakda ng presyo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Tumaas ang bilihin. Ang kita, hindi. 😮‍💨\n\n"
            "Bigas, gas, kuryente, tumaas lahat. Ang arawan mo, hindi.\n\n"
            "Sa Tarbaho, dagdag na customer sa bayan mo. Ikaw ang nagtatakda ng presyo mo. "
            "Ang mga household sa Angono, Antipolo at Taytay ang magme-message sa'yo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w1-extra", side="worker", stage="problem", season="all", icon="wallet",
        problem="Kailangan mo ng extra?",
        sub="Magaling ka. Masipag ka. Kulang pa rin.",
        answer="Sa Tarbaho, ang mga household sa bayan mo ang hahanap sa'yo. Walang komisyon.",
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
        sub="General cleaning, pull-out, freon, walang lamig. Sunod-sunod ang naghahanap.",
        answer="Sa Tarbaho, kita mo ang mga request sa bayan mo. Mag-offer ng presyo mo, walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Aircon tech ka? Peak season na. ❄️\n\n"
            "General cleaning, pull-out, chemical wash, walang lamig. Sunod-sunod ang naghahanap ngayong summer.\n\n"
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
        answer="Sa Tarbaho, kita mo ang mga request malapit sa'yo. Ikaw ang mag-o-offer ng presyo.",
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
        sub="May mga bahay na naghahanap. Hindi ka lang nila makita.",
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
        answer="Sa Tarbaho, ipakita ang gawa mo: picture, reviews, at ilang taon ka na. Walang komisyon.",
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
    dict(
        id="w13-ulan", side="worker", stage="problem", season="Jun-Oct", icon="rebar",
        problem="Tag-ulan. Walang buhos, walang arawan.",
        sub="Tigil ang site. Pero tuloy ang bayarin.",
        answer="Nasa Tarbaho ang mga bahay na may tulo at barado. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Tag-ulan. Walang buhos, walang arawan. 🌧️\n\n"
            "Tigil ang site. Pero tuloy ang bayarin sa bahay.\n\n"
            "Habang tag-ulan, ang daming bahay sa Angono, Antipolo at Taytay ang may tulo sa bubong, barado "
            "na alulod at basang kisame. Nasa Tarbaho ang mga request nila. Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w14-chat", side="worker", stage="product", season="all", icon="chat",
        problem="Tawag nang tawag, hindi naman tuloy?",
        sub="Sayang ang load. Sayang ang biyahe.",
        answer="Sa Tarbaho, kita mo muna ang request, picture at budget. Chat sa app bago ka pumunta.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Tawag nang tawag, hindi naman tuloy? 📵\n\n"
            "Sayang ang load. Sayang ang biyahe.\n\n"
            "Sa Tarbaho, kita mo muna ang request: ano ang sira, may picture, at magkano ang budget. "
            "Chat sa app bago ka pumunta. Walang komisyon, sa'yo ang buong bayad.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w15-suki", side="worker", stage="solution", season="all", icon="people",
        problem="Ang suki mo, galing lang sa sabi-sabi?",
        sub="Matagal bago kumalat ang pangalan mo. Isang bahay, isang kwento.",
        answer="Sa Tarbaho, kita ng mga bahay sa bayan mo ang reviews at gawa mo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Ang suki mo, galing lang sa sabi-sabi? 🗣️\n\n"
            "Matagal bago kumalat ang pangalan mo. Isang bahay, isang kwento.\n\n"
            "Sa Tarbaho, nakikita ng mga household sa Angono, Antipolo at Taytay ang reviews at picture ng "
            "gawa mo. Bawat natapos mo, nadadagdag sa profile mo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Suki #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w16-profile", side="worker", stage="most", season="all", icon="profile-card",
        problem="Gawa ng profile ngayon. Libre.",
        sub="Serbisyo, presyo, bayan. Selfie at number. Tapos na.",
        answer="Makikita ka na ng mga bahay sa Angono, Antipolo at Taytay. Walang komisyon, sa'yo ang buong bayad.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Gawa ng profile ngayon. Libre. 📲\n\n"
            "Serbisyo mo, presyo mo, mga bayan na pinupuntahan mo. Selfie at number. Tapos na.\n\n"
            "Makikita ka na ng mga household sa Angono, Antipolo at Taytay, at makikita mo ang mga request nila.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w17-tesda", side="worker", stage="product", season="all", icon="document",
        problem="May TESDA ka? Ipakita mo.",
        sub="Pinaghirapan mo 'yan. Dapat nakikita ng customer.",
        answer="Sa Tarbaho, may badge ang TESDA, ID at past project mo sa profile. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "May TESDA ka? Ipakita mo. 📜\n\n"
            "Pinaghirapan mo 'yan. Dapat nakikita ng customer.\n\n"
            "Sa Tarbaho, magpadala ng picture ng TESDA certificate, ID o past project mo. Kapag na-check, "
            "may badge ka sa profile na kita ng mga household sa Angono, Antipolo at Taytay.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #TESDA #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w18-solar", side="worker", stage="solution", season="Mar-May", icon="solar",
        problem="Solar tech ka? Summer na.",
        sub="Dami nang nagtatanong sa solar.",
        answer="Sa Tarbaho, makikita ka ng mga bahay sa bayan mo. Ilagay ang presyo mo at mag-offer sa mga request.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Solar tech ka? Summer na. ☀️\n\n"
            "Summer na. Maraming bahay ang nagtatanong na tungkol sa solar.\n\n"
            "Sa Tarbaho, makikita ka ng mga household sa Angono, Antipolo at Taytay. Ilagay ang presyo mo "
            "at mag-offer sa mga request nila.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Solar #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w19-weekend", side="worker", stage="unaware", season="all", icon="calendar",
        problem="Sabado't Linggo, nakatambay ka lang?",
        sub="'Yung dalawang araw na 'yon, pwedeng dagdag na kita.",
        answer="Sa Tarbaho, ikaw ang pipili kung kailan ka libre. Mag-offer sa mga request sa bayan mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Sabado't Linggo, nakatambay ka lang? 📅\n\n"
            "'Yung dalawang araw na 'yon, pwedeng dagdag na kita.\n\n"
            "Sa Tarbaho, ikaw ang pipili kung kailan ka libre. Kita mo ang mga request ng household sa Angono, "
            "Antipolo at Taytay. Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Sideline #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w20-tiles", side="worker", stage="solution", season="all", icon="tiles",
        problem="Tile setter ka? Ang daming lumang CR.",
        sub="Basag na tiles, may tagas. Gusto nang ipaayos, walang mahanap.",
        answer="Sa Tarbaho, kita mo ang mga request sa bayan mo, may picture at budget. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Tile setter ka? Ang daming lumang CR. 🧱\n\n"
            "Basag na tiles, may tagas. Gusto nang ipaayos, walang mahanap.\n\n"
            "Sa Tarbaho, kita mo ang mga request ng household sa Angono, Antipolo at Taytay, may picture at budget. "
            "Mag-offer ng presyo mo, pakyaw man o per square meter.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w21-kabit-ilaw", side="worker", stage="solution", season="Nov-Dec", icon="electrical",
        problem="Electrician ka? Kabit-ilaw na.",
        sub="Christmas lights, saksakan, breaker. Sunod-sunod 'yan ngayong Disyembre.",
        answer="Nasa Tarbaho ang mga request sa bayan mo. Mag-offer ng presyo mo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Electrician ka? Kabit-ilaw na. 🎄\n\n"
            "Christmas lights, saksakan, breaker na nagti-trip. Sunod-sunod 'yan ngayong Disyembre.\n\n"
            "Nasa Tarbaho ang mga request ng household sa Angono, Antipolo at Taytay. Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w22-helper", side="worker", stage="unaware", season="all", icon="worker",
        problem="\"Helper lang\" daw?",
        sub="Pero ikaw ang unang dumarating sa site at huling umuuwi.",
        answer="Sa Tarbaho, may sarili kang profile at presyo. Buhat, hakot, linis ng site, sa bayan mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "\"Helper lang\" daw? 💪\n\n"
            "Pero ikaw ang unang dumarating sa site at huling umuuwi.\n\n"
            "Sa Tarbaho, may sarili kang profile at sariling presyo. Buhat, hakot, linis ng site, para sa mga "
            "household sa Angono, Antipolo at Taytay.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w23-mekaniko", side="worker", stage="solution", season="all", icon="mechanic",
        problem="Marunong ka sa makina?",
        sub="May mga kotse sa bayan mo na ayaw mag-start. Hindi ka lang nila kilala.",
        answer="Sa Tarbaho, makikita ka ng mga household na malapit. Ilagay ang presyo mo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Marunong ka sa makina? 🔧\n\n"
            "May mga kotse sa bayan mo na ayaw mag-start. Hindi ka lang nila kilala.\n\n"
            "Sa Tarbaho, makikita ka ng mga household sa Angono, Antipolo at Taytay. Ilagay ang presyo mo "
            "at ang mga serbisyo mo: tune-up, change oil, battery, preno.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Mekaniko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w24-presyo", side="worker", stage="product", season="all", icon="edit",
        problem="Ikaw ang magsasabi ng presyo mo.",
        sub="Walang ahente na nagtatakda. Walang patong sa customer.",
        answer="Sa Tarbaho, ilagay ang average price mo. Gabay lang 'yan, kayo ng customer ang magkakasundo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Ikaw ang magsasabi ng presyo mo. ✍️\n\n"
            "Walang ahente na nagtatakda. Walang patong sa customer.\n\n"
            "Sa Tarbaho, ilagay ang average price mo kada serbisyo. Gabay lang 'yan, kayo ng customer ang "
            "magkakasundo sa final. Sa Requests, ikaw rin ang mag-o-offer ng presyo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #WalangKomisyon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w25-ate-linis", side="worker", stage="solution", season="all", icon="clean",
        problem="Ate, sanay ka maglinis ng bahay?",
        sub="May mga pamilyang naghahanap ng tulong tuwing Sabado.",
        answer="Sa Tarbaho, gumawa ng profile at ilagay ang presyo mo. Ikaw ang pipili ng oras.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Ate, sanay ka maglinis ng bahay? 🧹\n\n"
            "May mga pamilyang naghahanap ng tulong tuwing Sabado.\n\n"
            "Sa Tarbaho, gumawa ng libreng profile at ilagay ang presyo mo. Ikaw ang pipili ng oras at "
            "ng mga bayan: Angono, Antipolo o Taytay.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w26-baha", side="worker", stage="solution", season="Jun-Oct", icon="drop",
        problem="Pagkatapos ng baha, ang daming aayusin.",
        sub="Barado, basang kisame, sirang saksakan. Kailangan ka ng mga bahay.",
        answer="Nasa Tarbaho ang mga bahay na binaha, may picture at budget. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Pagkatapos ng baha, ang daming aayusin. 🌊\n\n"
            "Barado, basang kisame, sirang saksakan. Kailangan ka ng mga bahay.\n\n"
            "Nasa Tarbaho ang mga request ng household sa Angono, Antipolo at Taytay, may picture at budget. "
            "Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Baha #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w27-foreman", side="worker", stage="solution", season="all", icon="construction",
        problem="Foreman ka? Kaya mo ang renovation.",
        sub="Extension, second floor, kusina. May mga bahay na nagpapagawa.",
        answer="Nasa Tarbaho ang mga request, may picture at budget. Arawan o pakyaw, ikaw ang bahala.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Foreman ka? Kaya mo ang renovation. 🏗️\n\n"
            "Extension, second floor, kusina. May mga bahay na nagpapagawa.\n\n"
            "Nasa Tarbaho ang mga request ng household sa Angono, Antipolo at Taytay, may picture at budget. "
            "Mag-offer ng presyo mo, arawan man o pakyaw.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w28-why", side="worker", stage="product", season="all", icon="search",
        problem="Bakit Tarbaho, hindi FB group?",
        sub="Walang komisyon. Malapit lang. Kita mo ang budget bago pumunta.",
        answer="Mga household lang sa Angono, Antipolo at Taytay. Chat sa app muna, number mamaya na.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Bakit Tarbaho, hindi FB group? 🤷\n\n"
            "✅ Walang komisyon, sa'yo ang buong bayad\n"
            "✅ Malapit lang: Angono, Antipolo, Taytay\n"
            "✅ Kita mo ang request, picture at budget bago pumunta\n"
            "✅ Ikaw ang nagtatakda ng presyo mo\n"
            "✅ Chat sa app muna, number mamaya na\n\n"
            "I-download ang Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w29-uwi", side="worker", stage="unaware", season="all", icon="briefcase",
        problem="Umuwi ka galing abroad?",
        sub="May skills ka. Wala pang suki dito.",
        answer="Sa Tarbaho, gumawa ng profile. Makikita ka ng mga bahay sa bayan mo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Umuwi ka galing abroad? ✈️\n\n"
            "May skills ka. Wala pang suki dito.\n\n"
            "Sa Tarbaho, gumawa ng libreng profile: serbisyo mo, presyo mo, at mga bayan mo. Makikita ka ng mga "
            "household sa Angono, Antipolo at Taytay.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #OFW #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w30-tag-ulan-now", side="worker", stage="most", season="Jun-Oct", icon="roofing",
        problem="Tag-ulan na. Mag-profile na.",
        sub="Dami nang nagpapaayos ng bubong.",
        answer="Libre ang Tarbaho. Walang komisyon. Angono, Antipolo at Taytay.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Tag-ulan na. Mag-profile na. 🌧️\n\n"
            "Dami nang nagpapaayos ng bubong, alulod at kisame.\n\n"
            "Sa Tarbaho, libre ang profile at walang komisyon. Ang mga household sa Angono, Antipolo at Taytay "
            "ang magme-message, at makikita mo ang mga request nila.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #TagUlan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w31-tubero", side="worker", stage="solution", season="Jun-Oct", icon="plumbing",
        problem="Tubero ka? Tag-ulan na.",
        sub="CR, lababo, kanal. Barado lahat.",
        answer="Nasa Tarbaho ang mga request sa bayan mo, may picture at budget. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Tubero ka? Tag-ulan na. 🚿\n\n"
            "CR, lababo, kanal. Barado lahat. Sunod-sunod ang nagpapaayos.\n\n"
            "Nasa Tarbaho ang mga request ng household sa Angono, Antipolo at Taytay, may picture at budget. "
            "Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w32-bayarin", side="worker", stage="problem", season="all", icon="wallet",
        problem="Ang daming bayarin?",
        sub="Hulog, tuition, kuryente. Sabay-sabay.",
        answer="Sa Tarbaho, dagdag na raket sa bayan mo. Walang komisyon, sa'yo ang buong bayad.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Ang daming bayarin? 🧾\n\n"
            "Hulog sa motor, tuition, kuryente. Sabay-sabay pa.\n\n"
            "Sa Tarbaho, dagdag na customer sa bayan mo. Ang mga household sa Angono, Antipolo at Taytay ang "
            "magme-message, at ikaw ang nagtatakda ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w33-karpintero", side="worker", stage="solution", season="all", icon="carpentry",
        problem="Karpintero ka?",
        sub="Pinto, cabinet, kisame. Maraming sira.",
        answer="Nasa Tarbaho ang mga bahay na may sirang pinto at cabinet. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Karpintero ka? 🪚\n\n"
            "Pinto na ayaw sumara, cabinet na bumigay, kisame na lumulubog. Maraming sira sa bayan mo.\n\n"
            "Nasa Tarbaho ang mga request ng household sa Angono, Antipolo at Taytay, may picture at budget. "
            "Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Karpintero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w34-reviews", side="worker", stage="product", season="all", icon="level-3",
        problem="Bawat natapos mo, may review.",
        sub="Nadadagdag sa profile mo.",
        answer="Sa Tarbaho, nasa profile mo ang bawat review. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Bawat natapos mo, may review. ⭐\n\n"
            "Nadadagdag sa profile mo, kasama ang picture ng gawa mo at ilang taon ka na.\n\n"
            "Sa Tarbaho, kita ng mga household sa Angono, Antipolo at Taytay ang reviews mo bago sila "
            "mag-message. Walang komisyon, sa'yo ang buong bayad.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w35-summer-now", side="worker", stage="most", season="Mar-May", icon="thermometer",
        problem="Summer na. Mag-profile na.",
        sub="Init na. Dami nang nagpapalinis.",
        answer="Libre ang profile, walang komisyon. Gawa na bago ang peak.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Summer na. Mag-profile na. ☀️\n\n"
            "Init na. Dami nang nagpapalinis ng aircon, nagpapakabit, nagpapa-check ng freon.\n\n"
            "Sa Tarbaho, libre ang profile at walang komisyon. Ang mga household sa Angono, Antipolo at Taytay "
            "ang magme-message, at makikita mo ang mga request nila.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w36-nakalampas", side="worker", stage="unaware", season="all", icon="pin",
        problem="Ilang customer na ang lumampas?",
        sub="Malapit lang sila. Hindi ka lang nila nakita.",
        answer="Sa Tarbaho, makikita ka ng mga bahay sa bayan mo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Ilang customer na ang lumampas? 📍\n\n"
            "Malapit lang sila. Hindi ka lang nila nakita, kaya iba ang natawagan.\n\n"
            "Sa Tarbaho, makikita ka ng mga household sa Angono, Antipolo at Taytay, kasama ang presyo mo at "
            "reviews.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w37-garden", side="worker", stage="solution", season="Jun-Oct", icon="garden",
        problem="Marunong ka mag-garden?",
        sub="Tag-ulan. Tubo nang tubo ang damo.",
        answer="Nasa Tarbaho ang mga bahay na nagpapalinis ng bakuran. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Marunong ka mag-garden? 🌿\n\n"
            "Tag-ulan. Tubo nang tubo ang damo sa bakuran ng mga bahay.\n\n"
            "Nasa Tarbaho ang mga household sa Angono, Antipolo at Taytay na nagpapalinis ng bakuran. "
            "Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w38-notif", side="worker", stage="product", season="all", icon="chat-alert",
        problem="Customer ang lalapit.",
        sub="'Di mo na kailangang mag-post.",
        answer="Sa Tarbaho, may notification kapag may nag-message. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Customer ang lalapit. 🔔\n\n"
            "'Di mo na kailangang mag-post araw-araw sa FB group.\n\n"
            "Sa Tarbaho, may profile ka na kita ng mga household sa Angono, Antipolo at Taytay. Kapag may "
            "nag-message, may notification ka. Walang komisyon.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w39-disyembre", side="worker", stage="problem", season="Nov-Dec", icon="building",
        problem="Disyembre. Tigil ang site.",
        sub="Pero tuloy ang Noche Buena.",
        answer="Sa Tarbaho, may mga bahay na nagpapaayos bago mag-Pasko. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Disyembre. Tigil ang site. 🏗️\n\n"
            "Pero tuloy ang Noche Buena, ang regalo, ang bayarin.\n\n"
            "Sa Tarbaho, may mga household sa Angono, Antipolo at Taytay na nagpapaayos bago mag-Pasko: "
            "pintura, ilaw, gripo, kisame. Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w40-it", side="worker", stage="solution", season="all", icon="it",
        problem="IT ka? Ang daming mahina ang WiFi.",
        sub="WFH, online class. Kailangan ka.",
        answer="Nasa Tarbaho ang mga request sa bayan mo: WiFi, laptop, printer, CCTV. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "IT ka? Ang daming mahina ang WiFi. 💻\n\n"
            "WFH, online class, CCTV. Kailangan ka ng mga bahay sa bayan mo.\n\n"
            "Nasa Tarbaho ang mga request ng household sa Angono, Antipolo at Taytay: WiFi, laptop, printer, "
            "CCTV. Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #IT #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w41-umaga", side="worker", stage="solution", season="all", icon="bag",
        problem="Bakante tuwing umaga?",
        sub="Grocery, PasaBuy, bayad ng bills.",
        answer="Sa Tarbaho, tumanggap ng pakisuyo sa bayan mo. Ikaw ang nagtatakda ng presyo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Bakante tuwing umaga? 🛍️\n\n"
            "Grocery, PasaBuy, bayad ng bills. May mga bahay na kailangan ng tulong.\n\n"
            "Sa Tarbaho, tumanggap ng pakisuyo sa Angono, Antipolo o Taytay. Ikaw ang nagtatakda ng presyo mo "
            "at ng oras mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Pakisuyo #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w42-tatlong-bayan", side="worker", stage="most", season="all", icon="people",
        problem="Isang profile. Tatlong bayan.",
        sub="Angono, Antipolo, Taytay.",
        answer="Libre ang Tarbaho. Walang komisyon. Ikaw ang pipili kung saan ka pupunta.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Isang profile. Tatlong bayan. 📍\n\n"
            "Angono, Antipolo, Taytay. Ikaw ang pipili kung saan ka lang pupunta.\n\n"
            "Libre ang Tarbaho at walang komisyon. Ang mga household ang magme-message, at makikita mo ang "
            "mga request nila.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w43-carwash", side="worker", stage="solution", season="all", icon="car-wash",
        problem="May car wash kit ka?",
        sub="Linis ng kotse sa bahay nila.",
        answer="Sa Tarbaho, makikita ka ng mga bahay sa bayan mo. Ilagay ang presyo mo. Walang komisyon.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "May car wash kit ka? 🚗\n\n"
            "Maraming may kotse ang walang oras pumila sa carwash. Ikaw na ang pupunta sa bahay nila.\n\n"
            "Sa Tarbaho, makikita ka ng mga household sa Angono, Antipolo at Taytay. Ilagay ang presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #CarWash #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w44-photos", side="worker", stage="product", season="all", icon="photos",
        problem="Picture ng gawa mo, nasa profile.",
        sub="Hindi na kailangang ikwento.",
        answer="Sa Tarbaho, hanggang 6 na picture ng gawa mo. Kita ng mga bahay bago mag-message.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Picture ng gawa mo, nasa profile. 📸\n\n"
            "Hindi na kailangang ikwento. Kita na nila.\n\n"
            "Sa Tarbaho, maglagay ng hanggang 6 na picture ng mga natapos mo. Kita ng mga household sa Angono, "
            "Antipolo at Taytay bago sila mag-message.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w45-tapos", side="worker", stage="problem", season="all", icon="foreman",
        problem="Tapos na ang project.",
        sub="Saan ang susunod?",
        answer="Sa Tarbaho, kita mo ang mga request sa bayan mo habang naghihintay ng susunod na site.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Tapos na ang project. 🏗️\n\n"
            "Saan ang susunod? Kailan ang susunod na tawag?\n\n"
            "Habang naghihintay, may mga household sa Angono, Antipolo at Taytay na may ipapagawa. "
            "Nasa Tarbaho ang mga request nila. Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w46-id-badge", side="worker", stage="product", season="all", icon="person",
        problem="May ID badge ka?",
        sub="Kita sa profile mo.",
        answer="Sa Tarbaho, magpadala ng picture ng ID. Kapag na-check, may badge ka sa profile.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "May ID badge ka? 🪪\n\n"
            "Kita ito ng mga household sa profile mo.\n\n"
            "Sa Tarbaho, magpadala ng picture ng government ID mo. Kapag na-check, may badge ka sa profile. "
            "Pwede ring TESDA at past project.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w47-mason", side="worker", stage="solution", season="all", icon="masonry",
        problem="Mason ka?",
        sub="Bakod, pader, bitak.",
        answer="Nasa Tarbaho ang mga bahay na may ipapagawang pader. Arawan o pakyaw, ikaw ang bahala.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Mason ka? 🧱\n\n"
            "Bakod, pader, bitak, sahig. Maraming bahay sa bayan mo ang may ipapagawa.\n\n"
            "Nasa Tarbaho ang mga request ng household sa Angono, Antipolo at Taytay, may picture at budget. "
            "Mag-offer ng presyo mo, arawan man o pakyaw.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Mason #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w48-libre", side="worker", stage="most", season="all", icon="level-4",
        problem="Libre ang profile.",
        sub="Walang komisyon. Walang monthly.",
        answer="Gumawa ng profile sa Tarbaho ngayon. Angono, Antipolo at Taytay.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Libre ang profile. 🆓\n\n"
            "Walang komisyon. Walang monthly. Sa'yo ang buong bayad.\n\n"
            "Gumawa ng profile sa Tarbaho ngayon, para makita ka ng mga household sa Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #WalangKomisyon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w49-biyahe", side="worker", stage="problem", season="all", icon="wheel",
        problem="Isang oras na biyahe.",
        sub="Para sa ₱500 na gawa.",
        answer="Sa Tarbaho, ikaw ang pipili kung gaano kalayo. Malapit lang ang customer.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Isang oras na biyahe. 🛵\n\n"
            "Para sa ₱500 na gawa. Kalahati, napunta lang sa gas.\n\n"
            "Sa Tarbaho, ikaw ang pipili kung gaano kalayo ka lang pupunta. Mga customer sa Angono, Antipolo "
            "at Taytay na malapit sa'yo.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w50-tuition", side="worker", stage="problem", season="May-Jun", icon="document",
        problem="Para sa tuition ni anak.",
        sub="Bawat raket, malaking tulong.",
        answer="Sa Tarbaho, dagdag na customer sa bayan mo. Walang komisyon, sa'yo ang buong bayad.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Para sa tuition ni anak. 🎒\n\n"
            "Pasukan na. Bawat raket, malaking tulong.\n\n"
            "Sa Tarbaho, dagdag na customer sa Angono, Antipolo at Taytay. Ang mga household ang magme-message, "
            "at ikaw ang nagtatakda ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Pasukan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w51-suki-mo", side="worker", stage="solution", season="all", icon="phone",
        problem="Sa'yo ba ang suki mo?",
        sub="O sa ahente?",
        answer="Sa Tarbaho, ikaw ang kausap ng customer. Walang komisyon, sa'yo ang buong bayad.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Sa'yo ba ang suki mo? 📱\n\n"
            "O sa ahente na kumukuha ng parte?\n\n"
            "Sa Tarbaho, ikaw mismo ang kausap ng household. Chat sa app, ikaw ang nagtatakda ng presyo, "
            "at sa'yo ang buong bayad. Angono, Antipolo at Taytay.\n\n"
            "Walang komisyon. 👉 {link}\n\n#Tarbaho #WalangKomisyon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w52-linis-season", side="worker", stage="solution", season="Nov-Dec", icon="house-clean",
        problem="Linis season na.",
        sub="General cleaning bago mag-Pasko.",
        answer="Nasa Tarbaho ang mga bahay na nagpapalinis. Mag-offer ng presyo mo.",
        cta="I-download ang Tarbaho. Walang komisyon.",
        caption=(
            "Linis season na. 🎄\n\n"
            "General cleaning bago mag-Pasko: kisame, bintana, kurtina, CR.\n\n"
            "Nasa Tarbaho ang mga household sa Angono, Antipolo at Taytay na nagpapalinis. "
            "Mag-offer ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    # ---- First post: early access (the app is not on Google Play yet; the CTA is the form) ----
    dict(
        id="w53-foreman", side="worker", stage="problem", season="all", icon="foreman",
        problem="Tagal ng tawag ni Foreman?",
        sub="Tapos na ang project, wala pang kasunod.",
        answer="Sa Tarbaho, sariling kliyente sa bayan mo. Sariling presyo, 0% commission.",
        cta="Mag-sign up: early access",
        towns="Malapit na sa Android · Angono · Antipolo · Taytay",
        caption=(
            "Tagal ng tawag ni Foreman? 📞\n\n"
            "Tapos na ang project, wala pang kasunod.\n"
            "Naghihintay ka lang. O may ahente pang kumukuha ng parte mo.\n\n"
            "Kaya namin ginawa ang Tarbaho.\n"
            "Isang libreng app kung saan ang mga nagpapagawa sa bahay\n"
            "ang direktang hahanap sa'yo, Kuya, Ate.\n\n"
            "✅ 0% commission. Sa'yo ang buong bayad.\n"
            "✅ Sariling presyo. Ikaw ang maglalagay ng range mo.\n"
            "✅ Kliyente na malapit lang: Angono muna, tapos Taytay at Antipolo.\n"
            "✅ Private ang number mo. Chat muna sa app, ibibigay lang pag pinili ka.\n\n"
            "Para sa Aircon Technician at Construction:\n"
            "Foreman, Karpintero, Electrical, Tubero, Mason, Tiles, Welding, Bubong, Steelman, Helper.\n\n"
            "📲 Malapit na sa Android.\n"
            "Gusto mo ng early access? Sagutan ang form (mga 3 minuto lang):\n"
            "👉 [GOOGLE FORM LINK]\n\n"
            "Ang mga nauna sa form, sila ang unang makakapag-setup ng profile\n"
            "at unang makakatanggap ng requests pag bukas na ang app.\n\n"
            "Kailangan lang namin ang Gmail na gamit mo sa Play Store\n"
            "para mapadala namin ang early access sa'yo.\n\n"
            "Naka-iPhone? Pwede rin via TestFlight. Sagutan lang din ang form.\n\n"
            "I-share sa kasamahan mo sa site! 🔨\n\n"
            "#Tarbaho #Angono #Taytay #Antipolo #Rizal #AirconTechnician #Construction #Karpintero #Electrician "
            "#Tubero #Raket"
        ),
    ),
    dict(
        id="w54-project", side="worker", stage="problem", season="all", icon="construction",
        problem="Patapos na naman ang project?",
        sub="Tapos ang buhos, tapos ang tiles. Uwi, antay ulit.",
        answer="Ihanda na ang susunod na raket sa Tarbaho. Mga nagpapagawa sa malapit, diretso sa'yo.",
        cta="Mag-sign up: early access",
        towns="Malapit na sa Android · Angono · Antipolo · Taytay",
        caption=(
            "Patapos na naman ang project? 🏗️\n\n"
            "Alam mo na 'yan: tapos ang buhos, tapos ang tiles,\n"
            "tapos uwi ka na naman at maghihintay.\n\n"
            "Sa Tarbaho, ikaw mismo ang may sariling kliyente.\n"
            "Libreng app para sa mga Kuya at Ate ng construction at aircon\n"
            "sa Angono, Taytay at Antipolo.\n\n"
            "✅ 0% commission. Buo ang bayad sa'yo.\n"
            "✅ Ikaw ang bahala sa presyo.\n"
            "✅ Mga nagpapagawa na malapit lang sa'yo.\n"
            "✅ Private ang number mo hanggang piliin ka.\n\n"
            "📲 Malapit na sa Android. Mag-early access na ngayon:\n"
            "👉 [GOOGLE FORM LINK]\n\n"
            "Ang mga nauna sa form, unang makakapag-setup ng profile\n"
            "at unang makakatanggap ng requests pag bukas na ang app.\n"
            "(Naka-iPhone? Pwede rin via TestFlight, sagutan lang ang form.)\n\n"
            "#Tarbaho #Angono #Taytay #Antipolo #Rizal #Construction #AirconTechnician #Raket"
        ),
    ),
]
