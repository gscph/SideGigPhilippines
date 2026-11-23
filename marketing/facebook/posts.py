# The Facebook posts. build.py turns each into a 1080x1080 feed image and a 1080x1920 story, and writes POSTS.md.
#
# Voice: the owner's, the way people type on Facebook (see .claude/agents/marketing-specialist.md, Voice).
#   Casual Taglish: ung, sayo, dun, di, pag, wag, nlng. Short sentences, light punctuation, no ad phrasing,
#   no formal Filipino, no jokes of our own (humor only from a real situation in the hook), no ✅ lists.
#   Workers: the trade's own words (Maestro, arawan, pakyaw, walang lamig, pull-out, freon).
#   "Errands" is "pakisuyo" here: bills, grocery, padala.
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
# Rules: never job/hire/employment/minimum wage; never vouch for a worker; never promise the price model
# (no "walang komisyon", "sa'yo ang buong bayad", "libre forever"; "libre ngayon" is fine). "Walang patong" only
# means no agent markup. The cta and the "Download ... {link}" line are for after launch: keep them as they are.

POSTS = [
    # ======================= Households =======================
    dict(
        id="h1-aircon", side="household", stage="problem", season="Mar-May", icon="aircon",
        problem="Ang init na, sira pa ung aircon.",
        sub="Walang lamig, tumutulo pa. Ung technician, seen lang.",
        answer="Post mo lang sa Tarbaho. Mga aircon tech na malapit sayo mag-ooffer, ikaw pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ang init na, sira pa ung aircon.\n\n"
            "Walang lamig, tumutulo pa. Ung technician, seen lang.\n\n"
            "Sa Tarbaho post mo lang ung sira. Mga aircon tech dito sa Angono, Antipolo at Taytay ung mag-ooffer "
            "ng presyo, ikaw na pipili. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h12-aircon-pms", side="household", stage="unaware", season="Mar-May", icon="thermometer",
        problem="Kelan mo huling pinalinis ung aircon?",
        sub="Di mo na maalala? Kaya pala mahina na lamig.",
        answer="Pa-cleaning mo na bago pa uminit nang todo. Sa Tarbaho, kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Kelan mo huling pinalinis ung aircon?\n\n"
            "Di mo na maalala? Kaya pala mahina na lamig.\n\n"
            "Sa Tarbaho may aircon tech na malapit sayo para sa general cleaning o chemical wash. "
            "Kita mo na presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #AirconCleaning #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h2-plumbing", side="household", stage="problem", season="all", icon="toilet",
        problem="Barado na naman ung CR?",
        sub="Tapos tulo pa gripo. Lagi pa sa pinaka-busy mong araw.",
        answer="Sa Tarbaho may tubero dito sa inyo. Kita mo presyo, reviews at gaano kalapit.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Barado na naman ung CR?\n\n"
            "Tapos tulo pa gripo. Lagi pa sa pinaka-busy mong araw.\n\n"
            "Sa Tarbaho kita mo agad mga tubero dito sa Angono, Antipolo at Taytay, may presyo, reviews at kung "
            "gaano kalapit. Chat muna kayo sa app, di mo kailangan ibigay number mo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h3-overprice", side="household", stage="product", season="all", icon="receipt",
        problem="₱800 daw. Naging ₱2,000.",
        sub="Tapos na ung gawa, wala ka nang magawa.",
        answer="Sa Tarbaho ikaw magsasabi ng budget mo. Kita mo lahat ng offer bago ka pumili.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "₱800 daw. Naging ₱2,000.\n\n"
            "Tapos na ung gawa, wala ka nang magawa.\n\n"
            "Sa Tarbaho ikaw magsasabi ng budget mo, tapos ung mga malapit sayo mag-ooffer. Kita mo lahat ng "
            "presyo bago ka pumili. Bayad pag tapos na, sa presyong napag-usapan nyo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h14-inflation", side="household", stage="solution", season="all", icon="cart",
        problem="Mahal na nga bilihin.",
        sub="Wag na dagdagan ng overprice.",
        answer="Sa Tarbaho mga malapit sayo mag-ooffer. Piliin mo ung pasok sa budget mo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mahal na nga bilihin.\n\n"
            "Wag na dagdagan ng overprice na repair.\n\n"
            "Sa Tarbaho post mo kailangan mo tapos lagay mo budget mo. Ilang offer darating galing sa mga "
            "malapit sayo dito sa Angono, Antipolo at Taytay. Pili ka nlng ng pasok sa bulsa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tipid #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h16-gc", side="household", stage="solution", season="all", icon="chat",
        problem="\"May kilala ba kayong tubero?\"",
        sub="Pinost mo sa GC. Puro seen.",
        answer="Nasa Tarbaho na ung tubero, electrician, aircon tech dito sa inyo. May presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "\"May kilala ba kayong tubero?\"\n\n"
            "Pinost mo sa GC. Puro seen.\n\n"
            "Sa Tarbaho andun na mga tubero, electrician, aircon tech at iba pa dito sa Angono, Antipolo at Taytay. "
            "May presyo at reviews na, di mo na kailangan magtanong-tanong.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h13-pasko", side="household", stage="problem", season="Nov-Dec", icon="calendar",
        problem="Darating na mga bisita sa Pasko.",
        sub="Sira pa ilaw, tulo pa gripo, di pa napipinturahan.",
        answer="Ipaayos mo na bago mag-24. Post mo sa Tarbaho, mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Darating na mga bisita sa Pasko.\n\n"
            "Sira pa ilaw, tulo pa gripo, di pa napipinturahan ung sala.\n\n"
            "Ipaayos mo na bago mag-24. Sa Tarbaho post mo lang kailangan mo, mga malapit sayo dito sa Angono, "
            "Antipolo at Taytay ung mag-ooffer ng presyo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h15-baha", side="household", stage="problem", season="Jun-Oct", icon="drop",
        problem="Bumaha na naman?",
        sub="Barado kanal, basa pader, andaming lilinisin.",
        answer="Sa Tarbaho may tubero at taga-linis na malapit sayo pag tapos ng baha.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Bumaha na naman?\n\n"
            "Barado kanal, basa pader, andaming lilinisin.\n\n"
            "Sa Tarbaho may tubero para sa kanal, taga-linis ng bahay, at gagawa ng sira pag tapos ng baha, "
            "dito lang sa Angono, Antipolo at Taytay. Kita mo presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Baha #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h7-roof", side="household", stage="problem", season="Jun-Oct", icon="roofing",
        problem="May timba na naman sa sala?",
        sub="Tulo ung bubong. Tapos may bagyo pa daw next week.",
        answer="Post mo sa Tarbaho ung sira sa yero o alulod. Mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "May timba na naman sa sala?\n\n"
            "Tulo ung bubong. Tapos may bagyo pa daw next week.\n\n"
            "Sa Tarbaho post mo ung sira, yero man o alulod o waterproofing. Lagay mo picture at budget mo. "
            "Mga malapit sayo dito sa Angono, Antipolo at Taytay ung mag-ooffer, ikaw pipili.\n\n"
            "I-download bago pa umulan. 👉 {link}\n\n#Tarbaho #Bubong #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h5-breaker", side="household", stage="problem", season="all", icon="electrical",
        problem="Nag-trip na naman ung breaker?",
        sub="Akala mo brownout, kayo lang pala.",
        answer="Post mo sa Tarbaho. Mga electrician na malapit sayo mag-ooffer ng presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nag-trip na naman ung breaker?\n\n"
            "Akala mo brownout, kayo lang pala. Ayaw mo naman galawin, delikado.\n\n"
            "Sa Tarbaho post mo lang ung problema tsaka kung san ka sa Angono, Antipolo o Taytay. "
            "Mga electrician na malapit ung mag-ooffer, ikaw pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h8-anay", side="household", stage="unaware", season="Jun-Oct", icon="termite",
        problem="Andaming gamu-gamo pag umuulan?",
        sub="Galing yan sa anay. Baka nasa kisame o pinto nyo na.",
        answer="Ipa-check mo na sa pest control. Sa Tarbaho, may malapit sayo, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Andaming gamu-gamo pag umuulan?\n\n"
            "Galing yan sa anay. Baka nasa kisame, hamba ng pinto o cabinet nyo na.\n\n"
            "Ipa-check mo na bago lumala. Sa Tarbaho may pest control dito sa Angono, Antipolo at Taytay, "
            "kita mo agad presyo at reviews. Anay, ipis, daga, lamok.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #PestControl #Anay #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h9-pakisuyo", side="household", stage="unaware", season="all", icon="errand",
        problem="Ubos Sabado mo sa pila?",
        sub="Bills, grocery, padala. Wala ka nang pahinga.",
        answer="Sa Tarbaho may pwede kang pakisuyuan dito sa inyo: bills, grocery, PasaBuy, padala.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ubos Sabado mo sa pila?\n\n"
            "Bayad kuryente at tubig, grocery, padala ng parcel. Wala ka nang pahinga.\n\n"
            "Sa Tarbaho may pwede kang pakisuyuan dito sa Angono, Antipolo at Taytay: bills, grocery, PasaBuy, "
            "padala, kuha ng documents. Kita mo presyo nila.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #PasaBuy #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h4-cleaning", side="household", stage="problem", season="all", icon="house-clean",
        problem="Pagod galing work, makalat pa bahay.",
        sub="Labahin, hugasin, linisin. Di matapos-tapos.",
        answer="Sa Tarbaho may taga-linis na malapit sayo. Kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Pagod galing work, makalat pa bahay.\n\n"
            "Labahin, hugasin, linisin. Di matapos-tapos.\n\n"
            "Sa Tarbaho may taga-linis dito sa Angono, Antipolo at Taytay para sa general cleaning. "
            "Kita mo presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Linis #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h6-mechanic", side="household", stage="problem", season="all", icon="mechanic",
        problem="Ayaw mag-start ng sasakyan?",
        sub="May lakad ka pa. Ung talyer, malayo at puno.",
        answer="Sa Tarbaho may mekaniko na malapit sayo. Kita mo agad presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ayaw mag-start ng sasakyan?\n\n"
            "May lakad ka pa. Ung talyer, malayo at puno.\n\n"
            "Sa Tarbaho may mekaniko dito sa Angono, Antipolo at Taytay. Kita mo presyo, reviews at gaano kalapit. "
            "Chat muna kayo sa app.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Mekaniko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h10-it", side="household", stage="problem", season="all", icon="it",
        problem="Ang bagal ng WiFi?",
        sub="May online class anak mo. May meeting ka bukas.",
        answer="Sa Tarbaho may IT na malapit sayo: WiFi, laptop, printer, CCTV. Kita mo presyo bago mag-chat.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ang bagal ng WiFi?\n\n"
            "May online class anak mo. May meeting ka bukas.\n\n"
            "Sa Tarbaho may IT dito sa Angono, Antipolo at Taytay para sa WiFi, laptop, PC, printer at CCTV. "
            "Kita mo presyo at reviews nila.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #IT #CCTV #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h11-tiles", side="household", stage="unaware", season="all", icon="tiles",
        problem="Ilang taon mo nang tinitiis ung CR?",
        sub="Basag tiles, may tagas. Lagi nlng saka na.",
        answer="Post mo sa Tarbaho, lagay mo picture at budget. Mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "Ilang taon mo nang tinitiis ung CR?\n\n"
            "Basag tiles, may tagas. Lagi nlng \"saka na\" kasi di mo alam magkano aabutin.\n\n"
            "Sa Tarbaho post mo gusto mo ipagawa, tiles man o tubo. Lagay mo picture at budget. "
            "Mga malapit sayo dito sa Angono, Antipolo at Taytay ung mag-ooffer, ikaw pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Renovation #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h17-lipat", side="household", stage="problem", season="May-Jun", icon="box",
        problem="Lilipat kayo ng bahay?",
        sub="Kabit aircon at ilaw, ayos gripo, linis bago pumasok.",
        answer="Sa Tarbaho andun na lahat: aircon tech, electrician, tubero, taga-linis. Malapit lang sayo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Lilipat kayo ng bahay?\n\n"
            "Kabit aircon at ilaw, ayos gripo, linis bago pumasok.\n\n"
            "Sa Tarbaho isang app nlng: aircon tech, electrician, tubero, karpintero, taga-linis, dito lang sa "
            "Angono, Antipolo at Taytay. Kita mo presyo bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #LipatBahay #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h18-why", side="household", stage="product", season="all", icon="search",
        problem="Bakit Tarbaho, hindi FB group?",
        sub="Kita mo presyo agad. Malapit lang. Chat sa app. Bayad pag tapos.",
        answer="Angono, Antipolo at Taytay lang. Pwede ka mag-post, sila mag-ooffer, ikaw pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Bakit Tarbaho, hindi FB group?\n\n"
            "Sa group \"PM for price\" lagi. Dito kita mo na presyo bago ka mag-message.\n\n"
            "Malapit lang sila, Angono, Antipolo at Taytay. Pwede ka rin mag-post, sila mag-ooffer, ikaw pipili. "
            "Chat muna sa app, ung number lalabas lang pag pumili ka na. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h19-now", side="household", stage="most", season="all", icon="done",
        problem="Lowest offer na malapit sayo.",
        sub="Post, compare, pili. Bayad pag tapos.",
        answer="Aircon, tubero, electrician, construction, linis, pakisuyo at iba pa. Libre mag-post ngayon.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Lowest offer na malapit sayo.\n\n"
            "Post mo kailangan mo, compare mo ung offers, pili ka. Bayad pag tapos na.\n\n"
            "Aircon, tubero, electrician, construction, linis, pakisuyo at iba pa, dito sa Angono, Antipolo at Taytay.\n\n"
            "Download Tarbaho ngayon. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h20-pasukan", side="household", stage="problem", season="May-Jun", icon="bulb",
        problem="Pasukan na next week.",
        sub="Sira pa ilaw, electric fan at WiFi.",
        answer="Ipaayos mo na bago first day. Sa Tarbaho, mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Pasukan na next week.\n\n"
            "Sira pa ilaw, electric fan at WiFi.\n\n"
            "Ipaayos mo na bago first day. Sa Tarbaho post mo lang, mga electrician at IT dito sa Angono, "
            "Antipolo at Taytay ung mag-ooffer ng presyo. Ikaw pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasukan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h21-bagyo", side="household", stage="unaware", season="Jun-Oct", icon="alert",
        problem="May bagyo daw ngayong linggo.",
        sub="Kamusta yero at alulod nyo? Wag mo na hintayin tumulo.",
        answer="Ipa-check mo na. Sa Tarbaho may roofing dito sa inyo, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "May bagyo daw ngayong linggo.\n\n"
            "Kamusta yero at alulod nyo? Wag mo na hintayin tumulo sa sala.\n\n"
            "Ipa-check mo na habang maaraw pa. Sa Tarbaho may roofing at construction dito sa Angono, Antipolo "
            "at Taytay, may presyo at reviews. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Bagyo #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h22-number", side="household", stage="product", season="all", icon="cyber-lock",
        problem="Ayaw mo i-post number mo?",
        sub="Di lahat ng nagme-message legit.",
        answer="Sa Tarbaho chat muna sa app. Lalabas lang number pag pumili ka na ng offer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ayaw mo i-post number mo?\n\n"
            "Tama ka, di lahat ng nagme-message legit.\n\n"
            "Sa Tarbaho chat muna kayo sa app. Ung number mo lalabas lang pag pumili ka na ng offer. "
            "Angono, Antipolo at Taytay lang.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h23-tubig", side="household", stage="unaware", season="all", icon="pipe",
        problem="Bakit ang taas ng bill sa tubig?",
        sub="Baka may tagas na di mo nakikita.",
        answer="Ipa-check mo sa tubero sa Tarbaho. Kita mo presyo bago ka mag-message.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Bakit ang taas ng bill sa tubig?\n\n"
            "Baka may tagas na di mo nakikita. Sa ilalim ng lababo, sa CR, sa tubo sa pader.\n\n"
            "Ipa-check mo bago lumaki pa. Sa Tarbaho may tubero dito sa Angono, Antipolo at Taytay. "
            "Kita mo presyo at reviews bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h24-bayad", side="household", stage="most", season="all", icon="money",
        problem="Bayad pag tapos na.",
        sub="Libre mag-post ngayon. Ikaw pipili.",
        answer="Aircon, tubero, electrician, construction, linis at pakisuyo dito sa Angono, Antipolo at Taytay.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Bayad pag tapos na.\n\n"
            "Libre mag-post ngayon. Mga malapit sayo ung mag-ooffer, ikaw pipili. Babayaran mo lang pag tapos na "
            "ung gawa, sa presyong napag-usapan nyo.\n\n"
            "Aircon, tubero, electrician, construction, linis at pakisuyo, dito sa Angono, Antipolo at Taytay.\n\n"
            "Download Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h25-cctv", side="household", stage="unaware", season="all", icon="gate",
        problem="Sino ung nasa gate kagabi?",
        sub="Kung may CCTV ka, alam mo sana.",
        answer="Sa Tarbaho may IT dito sa inyo na nagkakabit ng CCTV. Kita mo presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Sino ung nasa gate kagabi?\n\n"
            "Kung may CCTV ka, alam mo sana.\n\n"
            "Sa Tarbaho may IT dito sa Angono, Antipolo at Taytay na nagkakabit ng CCTV at nag-aayos ng WiFi. "
            "Kita mo presyo at reviews bago ka mag-message.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #CCTV #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h26-carwash", side="household", stage="problem", season="all", icon="car-wash",
        problem="Ang dumi na naman ng kotse?",
        sub="Wala kang oras pumila sa carwash.",
        answer="Sa Tarbaho may malapit sayo na pupunta sa bahay nyo para linisin kotse mo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ang dumi na naman ng kotse?\n\n"
            "Wala kang oras pumila sa carwash pag Linggo.\n\n"
            "Sa Tarbaho may taga-Angono, Antipolo at Taytay na pumupunta sa bahay para maglinis ng sasakyan. "
            "Kita mo agad presyo. Bayad pag tapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #CarWash #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h27-mama", side="household", stage="solution", season="all", icon="home",
        problem="Si Mama mag-isa sa bahay. Sira gripo.",
        sub="Di nya alam sino tatawagan. Malayo ka pa.",
        answer="Ikaw na mag-post para sa kanya sa Tarbaho. Mga tubero dun sa kanila mag-ooffer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Si Mama mag-isa sa bahay. Sira gripo.\n\n"
            "Di nya alam sino tatawagan. Malayo ka pa.\n\n"
            "Sa Tarbaho ikaw na mag-post para sa kanya. Mga tubero dun sa kanila sa Angono, Antipolo o Taytay "
            "ung mag-ooffer. Chat muna sa app, kita mo presyo at reviews. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h28-kisame", side="household", stage="problem", season="Jun-Oct", icon="home-repair",
        problem="May mantsa na naman sa kisame?",
        sub="Tulo yan galing bubong. Lalaki pa yan ngayong tag-ulan.",
        answer="Post mo sa Tarbaho, may picture. Mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "May mantsa na naman sa kisame?\n\n"
            "Tulo yan galing bubong. Lalaki pa yan ngayong tag-ulan.\n\n"
            "Sa Tarbaho post mo picture ng kisame tapos budget mo. Mga karpintero at roofing dito sa Angono, "
            "Antipolo at Taytay ung mag-ooffer, ikaw pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #TagUlan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h29-xmas-lights", side="household", stage="problem", season="Nov-Dec", icon="bolt",
        problem="Patay-sindi na naman Christmas lights?",
        sub="Baka di lang ilaw. Baka saksakan na.",
        answer="Ipa-check mo bago Noche Buena. Mga electrician na malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Patay-sindi na naman Christmas lights?\n\n"
            "Baka di lang ilaw. Baka ung saksakan o wiring na.\n\n"
            "Ipa-check mo bago Noche Buena. Sa Tarbaho post mo lang, mga electrician dito sa Angono, Antipolo "
            "at Taytay ung mag-ooffer ng presyo. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h30-damo", side="household", stage="problem", season="Jun-Oct", icon="garden",
        problem="Ang taas na ng damo sa bakuran?",
        sub="Tag-ulan kasi. Isang linggo lang, kumapal na ulit.",
        answer="Sa Tarbaho may malapit sayo na pwede maglinis ng bakuran. Kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ang taas na ng damo sa bakuran?\n\n"
            "Tag-ulan kasi. Isang linggo lang, kumapal na ulit. Tapos may lamok pa.\n\n"
            "Sa Tarbaho may taga-Angono, Antipolo at Taytay para sa gardening at linis ng bakuran. "
            "Kita mo agad presyo. Bayad pag tapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h31-printer", side="household", stage="problem", season="all", icon="it",
        problem="Ayaw mag-print, deadline bukas.",
        sub="Project ng anak. Ung printer, blinking lang.",
        answer="Sa Tarbaho may IT na malapit sayo: printer, laptop, WiFi. Kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Ayaw mag-print, deadline bukas.\n\n"
            "Project ng anak. Ung printer, blinking lang.\n\n"
            "Sa Tarbaho may IT dito sa Angono, Antipolo at Taytay para sa printer, laptop at WiFi. "
            "Kita mo agad presyo at reviews. Bayad pag tapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h32-magkano", side="household", stage="product", season="Mar-May", icon="checklist",
        problem="Magkano ba talaga linis ng aircon?",
        sub="Sa FB, \"PM for price\" lagi sagot.",
        answer="Sa Tarbaho nakalagay na presyo sa profile nila. Compare mo muna bago ka mag-message.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Magkano ba talaga linis ng aircon?\n\n"
            "Sa FB, \"PM for price\" lagi sagot.\n\n"
            "Sa Tarbaho nakalagay na presyo sa profile ng mga aircon tech dito sa Angono, Antipolo at Taytay. "
            "Compare mo muna bago ka mag-message. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h33-lababo", side="household", stage="problem", season="all", icon="sink",
        problem="Amoy kanal na naman ung lababo?",
        sub="Di yan madadaan sa zonrox lang.",
        answer="Sa Tarbaho may tubero na malapit sayo. Kita mo agad presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Amoy kanal na naman ung lababo?\n\n"
            "Di yan madadaan sa zonrox lang. Baka barado na tubo o sira ung trap.\n\n"
            "Sa Tarbaho may tubero dito sa Angono, Antipolo at Taytay. Kita mo presyo at reviews bago ka "
            "mag-message. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h34-pasko-linis", side="household", stage="solution", season="Nov-Dec", icon="house-clean",
        problem="General cleaning bago mag-Pasko?",
        sub="Kisame, bintana, kurtina. Andami.",
        answer="Sa Tarbaho may taga-linis na malapit sayo para sa general cleaning. Kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "General cleaning bago mag-Pasko?\n\n"
            "Kisame, bintana, kurtina, ilalim ng ref. Di mo kaya lahat yan mag-isa.\n\n"
            "Sa Tarbaho may taga-linis dito sa Angono, Antipolo at Taytay para sa general cleaning. "
            "Kita mo agad presyo. Bayad pag tapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h35-gate", side="household", stage="problem", season="all", icon="weld",
        problem="Kumakalampag na ung gate?",
        sub="Kalawang na, lumalaylay pa.",
        answer="Post mo sa Tarbaho, may picture. Mga welder na malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "Kumakalampag na ung gate?\n\n"
            "Kalawang na, lumalaylay pa. Hirap na isara pag gabi.\n\n"
            "Sa Tarbaho post mo picture ng gate tapos budget mo. Mga welder dito sa Angono, Antipolo at "
            "Taytay ung mag-ooffer, ikaw pipili. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Welding #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h36-saksakan", side="household", stage="unaware", season="all", icon="electrical",
        problem="Mainit ung saksakan?",
        sub="Di normal yan. Delikado yan.",
        answer="Ipa-check mo sa electrician na malapit sayo. Sa Tarbaho, may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mainit ung saksakan?\n\n"
            "Di normal yan, lalo na pag nakasaksak aircon, ref o plantsa. Wag mo na hintayin umusok.\n\n"
            "Ipa-check mo sa electrician. Sa Tarbaho may electrician dito sa Angono, Antipolo at Taytay, "
            "may presyo at reviews. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h37-lubog", side="household", stage="problem", season="Jun-Oct", icon="mechanic",
        problem="Nalubog kotse sa baha?",
        sub="Ayaw na mag-start, basa makina.",
        answer="Sa Tarbaho may mekaniko na malapit sayo. Kita mo presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nalubog kotse sa baha?\n\n"
            "Ayaw na mag-start, basa makina. Di mo pa madala sa talyer.\n\n"
            "Sa Tarbaho may mekaniko dito sa Angono, Antipolo at Taytay. Kita mo presyo at reviews bago ka "
            "mag-message. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Baha #Mekaniko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h38-offers", side="household", stage="product", season="all", icon="megaphone",
        problem="Isang post, ilang offer.",
        sub="Di mo na kailangan mag-message isa-isa.",
        answer="Post mo sa Tarbaho kailangan mo. Mga malapit sayo mag-ooffer, ikaw pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Isang post, ilang offer.\n\n"
            "Di mo na kailangan mag-message isa-isa sa sampung page.\n\n"
            "Sa Tarbaho post mo kailangan mo, may picture at budget. Mga malapit sayo dito sa Angono, Antipolo "
            "at Taytay ung mag-ooffer ng presyo. Compare mo, pili ka, bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h39-bitak", side="household", stage="unaware", season="all", icon="masonry",
        problem="May bitak na ung pader?",
        sub="Maliit pa ngayon. Lalaki pa yan.",
        answer="Ipa-check mo sa mason sa Tarbaho. Kita mo presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "May bitak na ung pader?\n\n"
            "Maliit pa ngayon. Lalaki pa yan, lalo na pag tag-ulan at pinapasok ng tubig.\n\n"
            "Ipa-check mo habang maaga. Sa Tarbaho may mason dito sa Angono, Antipolo at Taytay, may presyo at "
            "reviews. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h40-pasabuy", side="household", stage="problem", season="all", icon="bag",
        problem="Wala kang oras mamalengke?",
        sub="Ubos lunch break mo sa pila.",
        answer="Sa Tarbaho may pwede kang pakisuyuan dito sa inyo para sa PasaBuy at grocery.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Wala kang oras mamalengke?\n\n"
            "Ubos lunch break mo sa pila. Pag-uwi mo, sarado na.\n\n"
            "Sa Tarbaho may pwede kang pakisuyuan dito sa Angono, Antipolo at Taytay: PasaBuy, grocery, "
            "bayad bills. Kita mo agad presyo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #PasaBuy #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h41-wfh", side="household", stage="problem", season="all", icon="chip",
        problem="Nag-lag na naman sa meeting?",
        sub="Mahina WiFi sa kwarto, patay sa sala.",
        answer="Sa Tarbaho may IT na malapit sayo para sa WiFi at router. Kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nag-lag na naman sa meeting?\n\n"
            "WFH ka pa naman. Mahina WiFi sa kwarto, patay sa sala.\n\n"
            "Sa Tarbaho may IT dito sa Angono, Antipolo at Taytay para sa WiFi, router at extender. "
            "Kita mo agad presyo at reviews. Bayad pag tapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #WFH #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h42-pasko-now", side="household", stage="most", season="Nov-Dec", icon="checklist",
        problem="Pasko na. Ipaayos mo na.",
        sub="Libre mag-post ngayon.",
        answer="Ilaw, gripo, pintura, linis. Mga malapit sayo mag-ooffer. Bayad pag tapos.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Pasko na. Ipaayos mo na.\n\n"
            "Libre mag-post ngayon. Ilaw, gripo, pintura, general cleaning.\n\n"
            "Mga malapit sayo dito sa Angono, Antipolo at Taytay ung mag-ooffer, ikaw pipili. "
            "Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h43-ingay", side="household", stage="problem", season="Mar-May", icon="aircon",
        problem="Maingay na ung aircon?",
        sub="Parang may kumakalampag sa loob.",
        answer="Ipa-check mo sa aircon tech na malapit sayo. Sa Tarbaho, kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Maingay na ung aircon?\n\n"
            "Parang may kumakalampag sa loob. Tapos humihina pa lamig.\n\n"
            "Ipa-check mo bago tuluyang masira sa gitna ng summer. Sa Tarbaho may aircon tech dito sa Angono, "
            "Antipolo at Taytay. Kita mo agad presyo at reviews.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h44-lola", side="household", stage="unaware", season="all", icon="toilet",
        problem="Madulas ung CR ni Lola?",
        sub="Lagyan mo na ng hawakan bago pa sya madulas.",
        answer="Sa Tarbaho may karpintero at tile setter na malapit sayo. Kita mo presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Madulas ung CR ni Lola?\n\n"
            "Lagyan mo na ng hawakan at anti-slip na tiles bago pa sya madulas.\n\n"
            "Sa Tarbaho may karpintero at tile setter dito sa Angono, Antipolo at Taytay. Kita mo presyo "
            "at reviews bago ka mag-message. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h45-budget", side="household", stage="product", season="all", icon="wallet",
        problem="₱1,500 lang budget mo?",
        sub="Post mo. Sila mag-ooffer.",
        answer="Sa Tarbaho lagay mo budget mo. Compare mo offers, bayad pag tapos na ung gawa.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "₱1,500 lang budget mo?\n\n"
            "Post mo. Sila mag-ooffer.\n\n"
            "Sa Tarbaho lagay mo kailangan mo tapos budget mo. Mga malapit sayo dito sa Angono, Antipolo at "
            "Taytay ung mag-ooffer ng presyo. Compare mo, pili ka, bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h46-yero", side="household", stage="problem", season="Jun-Oct", icon="roofing",
        problem="Natanggal yero nung bagyo?",
        sub="Bukas pa ung butas. Uulan na naman.",
        answer="Post mo sa Tarbaho, may picture. Mga roofing na malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "Natanggal yero nung bagyo?\n\n"
            "Bukas pa ung butas. Uulan na naman mamaya.\n\n"
            "Sa Tarbaho post mo picture ng bubong tapos budget mo. Mga roofing at construction dito sa Angono, "
            "Antipolo at Taytay ung mag-ooffer, ikaw pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Bagyo #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h47-shower", side="household", stage="problem", season="all", icon="drop",
        problem="Mahina tubig sa shower?",
        sub="Patak-patak lang. Late ka na.",
        answer="Ipa-check mo sa tubero na malapit sayo. Sa Tarbaho, kita mo agad presyo.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Mahina tubig sa shower?\n\n"
            "Patak-patak lang. Late ka na sa work.\n\n"
            "Baka barado shower head o may problema sa tubo. Sa Tarbaho may tubero dito sa Angono, Antipolo "
            "at Taytay. Kita mo agad presyo at reviews. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h48-reviews", side="household", stage="product", season="all", icon="people",
        problem="Kita mo muna reviews.",
        sub="Bago ka mag-message, alam mo na.",
        answer="Sa Tarbaho may presyo, reviews at picture ng gawa sa bawat profile. Ikaw pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Kita mo muna reviews.\n\n"
            "Bago ka mag-message alam mo na kung magkano, gaano kalapit, at ano sabi ng ibang nagpagawa.\n\n"
            "Sa Tarbaho may presyo, reviews at picture ng gawa sa bawat profile, dito sa Angono, Antipolo at "
            "Taytay. Ikaw pipili. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h49-pullout", side="household", stage="problem", season="May-Jun", icon="aircon",
        problem="Lilipat, kasama aircon?",
        sub="Pull-out dito, kabit dun.",
        answer="Sa Tarbaho mga aircon tech na malapit sayo mag-ooffer. Ikaw pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Lilipat, kasama aircon?\n\n"
            "Pull-out dito, kabit dun. Baka kailangan pa i-check freon.\n\n"
            "Sa Tarbaho post mo lang. Mga aircon tech dito sa Angono, Antipolo at Taytay ung mag-ooffer ng "
            "presyo, ikaw pipili. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #LipatBahay #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h50-bills", side="household", stage="unaware", season="all", icon="document",
        problem="Nakalimutan mo due date?",
        sub="Busy lang. Pero may multa talaga.",
        answer="Sa Tarbaho may pwede kang pakisuyuan magbayad ng bills.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nakalimutan mo due date?\n\n"
            "Busy lang. Pero may multa talaga, minsan putol pa.\n\n"
            "Sa Tarbaho may pwede kang pakisuyuan dito sa Angono, Antipolo at Taytay: bayad bills, padala, "
            "documents. Kita mo agad presyo.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h51-basa-saksakan", side="household", stage="unaware", season="Jun-Oct", icon="bolt",
        problem="Nabasa ung saksakan?",
        sub="Wag mo munang buksan ung breaker.",
        answer="Ipa-check mo muna sa electrician na malapit sayo. Sa Tarbaho may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Nabasa ung saksakan?\n\n"
            "Wag mo munang buksan ung breaker. Delikado.\n\n"
            "Ipa-check mo muna sa electrician. Sa Tarbaho may electrician dito sa Angono, Antipolo at Taytay, "
            "may presyo at reviews. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Baha #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h52-sabado", side="household", stage="solution", season="all", icon="calendar",
        problem="Sabado lang free mo?",
        sub="Dun mo na ipagawa lahat.",
        answer="Post mo sa Tarbaho, sabihin mo Sabado ka free. Mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Sabado lang free mo?\n\n"
            "Dun mo na ipagawa lahat: gripo, ilaw, aircon, linis.\n\n"
            "Sa Tarbaho post mo kailangan mo, sabihin mo na Sabado ka free. Mga malapit sayo dito sa Angono, "
            "Antipolo at Taytay ung mag-ooffer, ikaw pipili.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h53-gripo", side="household", stage="problem", season="all", icon="plumbing",
        problem="Tulo nang tulo ung gripo?",
        sub="Sayang tubig, sayang bill.",
        answer="Sa Tarbaho may tubero dito sa inyo. Presyo at reviews, kita agad.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Tulo nang tulo ung gripo?\n\n"
            "Sayang tubig, sayang bill. Rinig mo pa gabi-gabi.\n\n"
            "Sa Tarbaho may tubero dito sa Angono, Antipolo at Taytay. Kita mo agad presyo at reviews. "
            "Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h54-pipili", side="household", stage="most", season="all", icon="done",
        problem="Ikaw pipili.",
        sub="Hindi ung ahente. Ikaw.",
        answer="Libre mag-post sa Tarbaho ngayon. Mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho ngayon",
        caption=(
            "Ikaw pipili.\n\n"
            "Hindi ung ahente. Ikaw. Kita mo presyo, reviews at gaano kalapit bago ka pumili.\n\n"
            "Libre mag-post sa Tarbaho ngayon. Mga malapit sayo dito sa Angono, Antipolo at Taytay ung "
            "mag-ooffer. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h55-bunso", side="household", stage="solution", season="May-Jun", icon="home-repair",
        problem="Kwarto ni bunso?",
        sub="Pintura, ilaw, cabinet.",
        answer="Post mo ung kwarto, may picture at budget. Mga malapit sayo mag-ooffer.",
        cta="Download Tarbaho. Ikaw ang pipili.",
        caption=(
            "Kwarto ni bunso?\n\n"
            "Pintura, ilaw, cabinet, bago mag-pasukan.\n\n"
            "Sa Tarbaho post mo gusto mo ipagawa, may picture at budget. Mga malapit sayo dito sa Angono, "
            "Antipolo at Taytay ung mag-ooffer, ikaw pipili. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h56-kulob", side="household", stage="problem", season="Mar-May", icon="aircon",
        problem="Amoy kulob ung aircon?",
        sub="Kailangan na linisin yan.",
        answer="Sa Tarbaho may aircon tech na malapit sayo. Kita mo agad presyo ng cleaning.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Amoy kulob ung aircon?\n\n"
            "Kailangan na linisin yan, lalo na buong araw nakabukas ngayong summer.\n\n"
            "Sa Tarbaho may aircon tech dito sa Angono, Antipolo at Taytay para sa general cleaning o chemical wash. "
            "Kita mo agad presyo. Bayad pag tapos.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h57-ceiling-fan", side="household", stage="solution", season="Mar-May", icon="drill",
        problem="Pakabit ng ceiling fan?",
        sub="Nabili na, nasa kahon pa. Wala pang magkakabit.",
        answer="Sa Tarbaho mga electrician na malapit sayo mag-ooffer. Ikaw pipili.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Pakabit ng ceiling fan?\n\n"
            "Nabili na, nasa kahon pa. Wala pang magkakabit.\n\n"
            "Sa Tarbaho post mo lang. Mga electrician dito sa Angono, Antipolo at Taytay ung mag-ooffer ng "
            "presyo, ikaw pipili. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h58-patong", side="household", stage="product", season="all", icon="receipt",
        problem="Walang patong ng ahente.",
        sub="Diretso ka na sa gagawa.",
        answer="Sa Tarbaho kita mo presyo, chat sa app, bayad pag tapos na ung gawa.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Walang patong ng ahente.\n\n"
            "Diretso ka na sa gagawa na malapit sayo.\n\n"
            "Sa Tarbaho kita mo muna presyo, chat muna sa app bago ibigay number, tapos bayad pag tapos na "
            "ung gawa. Angono, Antipolo at Taytay.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h59-poso", side="household", stage="problem", season="Jun-Oct", icon="toilet",
        problem="Puno na poso negro?",
        sub="Amoy na sa buong bahay.",
        answer="Ipa-check mo sa tubero na malapit sayo. Sa Tarbaho may presyo at reviews.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Puno na poso negro?\n\n"
            "Amoy na sa buong bahay. Umaapaw pa pag malakas ulan.\n\n"
            "Sa Tarbaho may tubero dito sa Angono, Antipolo at Taytay. Kita mo presyo at reviews bago ka "
            "mag-message. Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="h60-malapit", side="household", stage="product", season="all", icon="pin",
        problem="Malapit lang sila.",
        sub="Taga-dito lang din.",
        answer="Sa Tarbaho, Angono, Antipolo at Taytay lang. Kita mo kung gaano kalapit.",
        cta="Download Tarbaho. Libre.",
        caption=(
            "Malapit lang sila.\n\n"
            "Taga-dito lang din, kaya mabilis makarating.\n\n"
            "Sa Tarbaho, Angono, Antipolo at Taytay lang. Kita mo kung gaano kalapit, presyo at reviews. "
            "Bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    # ======================= Skilled workers =======================
    dict(
        id="w2-between", side="worker", stage="problem", season="all", icon="foreman",
        problem="Wala pang project. Antay ng tawag ni Maestro.",
        sub="Isang linggo na, wala pa rin. Tuloy gastos sa bahay.",
        answer="Habang nag-aantay, may mga bahay dito sa inyo na need ng karpintero, mason, tubero.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Wala pang project. Antay ng tawag ni Maestro.\n\n"
            "Isang linggo na, wala pa rin. Tuloy gastos sa bahay.\n\n"
            "Habang nag-aantay, may mga bahay dito sa Angono, Antipolo at Taytay na nagpo-post ng kailangan nila: "
            "karpintero, mason, tiles, welding, tubero, electrician. Kita mo sa Requests, offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w7-gas", side="worker", stage="problem", season="all", icon="gas",
        problem="Mahal gas, malayo pa customer.",
        sub="Laki napupunta sa biyahe. Kulang na naiuuwi.",
        answer="Sa Tarbaho ikaw pipili kung gaano kalayo ka lang. Malapit lang ung customer.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Mahal gas, malayo pa customer.\n\n"
            "Laki napupunta sa biyahe. Kulang na naiuuwi.\n\n"
            "Sa Tarbaho ikaw magsasabi kung hanggang san ka lang pupunta. Ung makikita mo, mga customer na "
            "malapit sayo dito sa Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Sideline #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w8-bilihin", side="worker", stage="problem", season="all", icon="cart",
        problem="Tumaas bilihin. Ung kita, hindi.",
        sub="Bigas, gas, kuryente, tumaas lahat. Ung arawan mo, hindi.",
        answer="Sa Tarbaho dagdag customer dito sa inyo. Ikaw magsasabi ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Tumaas bilihin. Ung kita, hindi.\n\n"
            "Bigas, gas, kuryente, tumaas lahat. Ung arawan mo, hindi.\n\n"
            "Sa Tarbaho dagdag customer dito sa inyo, ikaw magsasabi ng presyo mo. Mga taga-Angono, Antipolo "
            "at Taytay ung magme-message sayo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w1-extra", side="worker", stage="problem", season="all", icon="wallet",
        problem="Kailangan mo ng extra?",
        sub="Magaling ka, masipag ka. Kulang pa rin.",
        answer="Sa Tarbaho ung mga bahay dito sa inyo ung hahanap sayo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Kailangan mo ng extra?\n\n"
            "Magaling ka. Pero ung customer mo puro kakilala lang. Pag walang tumawag, walang kita.\n\n"
            "Sa Tarbaho gawa ka ng profile, libre ngayon: serbisyo mo, presyo mo, tsaka mga bayan na pinupuntahan mo. "
            "Mga taga-Angono, Antipolo at Taytay ung magme-message sayo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Sideline #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w12-aircon-season", side="worker", stage="solution", season="Mar-May", icon="aircon",
        problem="Aircon tech ka? Peak season na.",
        sub="Cleaning, pull-out, freon, walang lamig. Sunod-sunod nagpapagawa.",
        answer="Sa Tarbaho kita mo mga request dito sa inyo. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Aircon tech ka? Peak season na.\n\n"
            "General cleaning, pull-out, chemical wash, walang lamig. Sunod-sunod nagpapagawa ngayong summer.\n\n"
            "Sa Tarbaho kita mo mga request dito sa Angono, Antipolo at Taytay. Offer ka ng presyo mo. "
            "Pag pinili ka, sila na magme-message.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #AirconTech #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w10-pasko", side="worker", stage="solution", season="Nov-Dec", icon="calendar",
        problem="Pandagdag pang-Noche Buena?",
        sub="Andaming bahay nagpapaayos bago Pasko.",
        answer="Nasa Tarbaho mga request nila: pintura, ilaw, gripo, linis. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Pandagdag pang-Noche Buena?\n\n"
            "Andaming bahay nagpapaayos bago Pasko: pintura, ilaw, gripo, linis.\n\n"
            "Sa Tarbaho kita mo mga request nila dito sa Angono, Antipolo at Taytay. Offer ka ng presyo mo. "
            "Pag pinili ka, sila na magme-message.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w3-waiting", side="worker", stage="solution", season="all", icon="megaphone",
        problem="Pagod ka na mag-antay ng tawag?",
        sub="Ung susunod mong customer, baka nasa kabilang kanto lang.",
        answer="Sa Tarbaho kita mo mga request na malapit sayo. Ikaw mag-ooffer ng presyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Pagod ka na mag-antay ng tawag?\n\n"
            "Ung susunod mong customer, baka nasa kabilang kanto lang. Di ka lang nila kilala.\n\n"
            "Sa Tarbaho may Requests: mga bahay na malapit sayo na may ipapagawa, kasama budget nila. "
            "Offer ka ng presyo mo.\n\n"
            "Libre i-download. 👉 {link}\n\n#Tarbaho #Sideline #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w5-welder", side="worker", stage="solution", season="all", icon="weld",
        problem="Welder ka? Gate, grills, railings?",
        sub="May mga bahay na naghahanap, di ka lang nila makita.",
        answer="Sa Tarbaho lagay mo gawa mo at presyo mo. Ung mga malapit sayo ung magme-message.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Welder ka? Gate, grills, railings, trusses?\n\n"
            "Andaming bahay na need nyan. Di ka lang nila kilala.\n\n"
            "Sa Tarbaho lagay mo picture ng mga natapos mo, presyo mo, tsaka mga bayan na pinupuntahan mo. "
            "Mga taga-Angono, Antipolo at Taytay ung magme-message sayo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Welding #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w6-pakisuyo", side="worker", stage="unaware", season="all", icon="truck",
        problem="May motor ka at bakanteng oras?",
        sub="Sa pagitan ng lakad mo, pwede ka na kumita.",
        answer="Sa Tarbaho tanggap ka ng pakisuyo dito sa inyo: bills, grocery, PasaBuy, padala.",
        cta="I-download ang Tarbaho.",
        caption=(
            "May motor ka at bakanteng oras?\n\n"
            "Andaming nanay at nagwo-work na walang oras pumila sa bayad center o mag-grocery.\n\n"
            "Sa Tarbaho tanggap ka ng pakisuyo dito sa Angono, Antipolo at Taytay: bills, grocery, PasaBuy, padala. "
            "Ikaw magsasabi ng presyo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #PasaBuy #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w4-unknown", side="worker", stage="unaware", season="all", icon="medal",
        problem="10 taon ka na. Kapitbahay lang nakakakilala sayo.",
        sub="Maganda gawa mo, pero walang nakakakita.",
        answer="Sa Tarbaho makikita gawa mo: picture, reviews, ilang taon ka na.",
        cta="I-download ang Tarbaho.",
        caption=(
            "10 taon ka na. Kapitbahay lang nakakakilala sayo.\n\n"
            "Maganda gawa mo, pero walang nakakakita.\n\n"
            "Sa Tarbaho lagay mo picture ng mga natapos mo, reviews ng mga customer mo, tsaka ilang taon ka na. "
            "Mas maraming bahay dito sa Angono, Antipolo at Taytay ung makakakita sayo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #SkilledWorker #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w9-seen", side="worker", stage="product", season="all", icon="money",
        problem="Nagtanong ng presyo, tapos nawala.",
        sub="Sayang oras mo kaka-reply.",
        answer="Sa Tarbaho kita mo na budget ng customer bago ka pa mag-offer.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Nagtanong ng presyo, tapos nawala.\n\n"
            "Sayang oras mo kaka-reply.\n\n"
            "Sa Tarbaho, nagpo-post ung customer ng kailangan nila, kasama ung budget. "
            "Kita mo na bago ka pa mag-offer.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w11-now", side="worker", stage="most", season="all", icon="done",
        problem="Libre mag-setup ngayon. Malapit lang.",
        sub="Gawa ka na ng profile para makita ka ng mga bahay dito sa inyo.",
        answer="Aircon, construction, tubero, electrician, linis, pakisuyo. Angono, Antipolo at Taytay.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Libre mag-setup ngayon. Malapit lang.\n\n"
            "Gawa ka na ng profile para makita ka ng mga bahay dito sa inyo.\n\n"
            "Aircon, construction, tubero, electrician, linis, pakisuyo at iba pa, dito sa Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w13-ulan", side="worker", stage="problem", season="Jun-Oct", icon="rebar",
        problem="Tag-ulan. Walang buhos, walang arawan.",
        sub="Tigil site, tuloy bayarin.",
        answer="Nasa Tarbaho mga bahay na may tulo at barado. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Tag-ulan. Walang buhos, walang arawan.\n\n"
            "Tigil site, tuloy bayarin sa bahay.\n\n"
            "Pag tag-ulan andaming bahay dito sa Angono, Antipolo at Taytay na may tulo sa bubong, barado "
            "na alulod, basang kisame. Nasa Tarbaho mga request nila. Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w14-chat", side="worker", stage="product", season="all", icon="chat",
        problem="Tawag nang tawag, di naman tuloy?",
        sub="Sayang load, sayang biyahe.",
        answer="Sa Tarbaho kita mo muna request, picture at budget. Chat muna sa app bago ka pumunta.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Tawag nang tawag, di naman tuloy?\n\n"
            "Sayang load, sayang biyahe.\n\n"
            "Sa Tarbaho kita mo muna ung request: ano sira, may picture, magkano budget. "
            "Chat muna kayo sa app bago ka pumunta.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w15-suki", side="worker", stage="solution", season="all", icon="people",
        problem="Ung suki mo, galing lang sa sabi-sabi?",
        sub="Matagal bago kumalat pangalan mo.",
        answer="Sa Tarbaho kita ng mga bahay dito sa inyo ung reviews at gawa mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Ung suki mo, galing lang sa sabi-sabi?\n\n"
            "Matagal bago kumalat pangalan mo. Isang bahay, isang kwento.\n\n"
            "Sa Tarbaho kita ng mga taga-Angono, Antipolo at Taytay ung reviews at picture ng gawa mo. "
            "Bawat natapos mo, nadadagdag sa profile mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Suki #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w16-profile", side="worker", stage="most", season="all", icon="profile-card",
        problem="Gawa ka na ng profile. Libre ngayon.",
        sub="Serbisyo, presyo, bayan. Selfie at number. Tapos na.",
        answer="Makikita ka na ng mga bahay sa Angono, Antipolo at Taytay.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Gawa ka na ng profile. Libre ngayon.\n\n"
            "Serbisyo mo, presyo mo, mga bayan na pinupuntahan mo. Selfie at number. Tapos na.\n\n"
            "Makikita ka na ng mga bahay dito sa Angono, Antipolo at Taytay, tapos kita mo na rin mga request nila.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w17-tesda", side="worker", stage="product", season="all", icon="document",
        problem="May TESDA ka? Ipakita mo.",
        sub="Pinaghirapan mo yan, dapat kita ng customer.",
        answer="Sa Tarbaho may badge ung TESDA, ID at past project mo sa profile.",
        cta="I-download ang Tarbaho.",
        caption=(
            "May TESDA ka? Ipakita mo.\n\n"
            "Pinaghirapan mo yan, dapat kita ng customer.\n\n"
            "Sa Tarbaho send mo picture ng TESDA certificate, ID o past project mo. Pag na-check ng team, "
            "may badge ka sa profile na kita ng mga taga-Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #TESDA #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w18-pest", side="worker", stage="solution", season="Jun-Oct", icon="spray",
        problem="Pest control ka? Tag-ulan na.",
        sub="Anay, ipis, lamok. Sunod-sunod nagpapa-spray.",
        answer="Sa Tarbaho makikita ka ng mga bahay dito sa inyo. Lagay mo presyo mo, offer ka sa mga request.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Pest control ka? Tag-ulan na.\n\n"
            "Lumalabas na gamu-gamo, andami na namang lamok. Sunod-sunod nagpapa-spray sa anay, ipis at lamok.\n\n"
            "Sa Tarbaho makikita ka ng mga bahay dito sa Angono, Antipolo at Taytay. Lagay mo presyo mo "
            "tapos offer ka sa mga request nila.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #PestControl #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w19-weekend", side="worker", stage="unaware", season="all", icon="calendar",
        problem="Sabado't Linggo, nakatambay ka lang?",
        sub="Ung dalawang araw na yun, pwede dagdag kita.",
        answer="Sa Tarbaho ikaw pipili kung kelan ka free. Offer ka sa mga request dito sa inyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Sabado't Linggo, nakatambay ka lang?\n\n"
            "Ung dalawang araw na yun, pwede dagdag kita.\n\n"
            "Sa Tarbaho ikaw pipili kung kelan ka free. Kita mo mga request dito sa Angono, Antipolo at Taytay, "
            "offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Sideline #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w20-tiles", side="worker", stage="solution", season="all", icon="tiles",
        problem="Tile setter ka? Andaming CR na sira.",
        sub="Basag tiles, may tagas. Walang mahanap na gagawa.",
        answer="Sa Tarbaho kita mo mga request dito sa inyo, may picture at budget. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Tile setter ka? Andaming CR na sira.\n\n"
            "Basag tiles, may tagas. Gusto na ipaayos, walang mahanap.\n\n"
            "Sa Tarbaho kita mo mga request dito sa Angono, Antipolo at Taytay, may picture at budget. "
            "Offer ka ng presyo mo, pakyaw man o per square meter.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w21-kabit-ilaw", side="worker", stage="solution", season="Nov-Dec", icon="electrical",
        problem="Electrician ka? Kabit-ilaw na.",
        sub="Christmas lights, saksakan, breaker. Sunod-sunod yan pag Disyembre.",
        answer="Nasa Tarbaho mga request dito sa inyo. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Electrician ka? Kabit-ilaw na.\n\n"
            "Christmas lights, saksakan, breaker na nagti-trip. Sunod-sunod yan pag Disyembre.\n\n"
            "Nasa Tarbaho mga request dito sa Angono, Antipolo at Taytay. Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Electrician #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w22-helper", side="worker", stage="unaware", season="all", icon="worker",
        problem="\"Helper lang\" daw?",
        sub="Pero ikaw unang dumarating sa site, ikaw huling umuuwi.",
        answer="Sa Tarbaho may sarili kang profile at presyo. Buhat, hakot, linis ng site, dito sa inyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "\"Helper lang\" daw?\n\n"
            "Pero ikaw unang dumarating sa site, ikaw huling umuuwi.\n\n"
            "Sa Tarbaho may sarili kang profile at sariling presyo. Buhat, hakot, linis ng site, para sa mga "
            "bahay dito sa Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w23-mekaniko", side="worker", stage="solution", season="all", icon="mechanic",
        problem="Marunong ka sa makina?",
        sub="May mga kotse dito sa inyo na ayaw mag-start. Di ka lang nila kilala.",
        answer="Sa Tarbaho makikita ka ng mga malapit sayo. Lagay mo presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Marunong ka sa makina?\n\n"
            "May mga kotse dito sa inyo na ayaw mag-start. Di ka lang nila kilala.\n\n"
            "Sa Tarbaho makikita ka ng mga taga-Angono, Antipolo at Taytay. Lagay mo presyo mo "
            "tsaka serbisyo mo: tune-up, change oil, battery, preno.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Mekaniko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w24-presyo", side="worker", stage="product", season="all", icon="edit",
        problem="Ikaw magsasabi ng presyo mo.",
        sub="Walang ahente na nagtatakda. Walang patong sa customer.",
        answer="Sa Tarbaho lagay mo average price mo. Gabay lang yan, kayo ng customer magkakasundo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Ikaw magsasabi ng presyo mo.\n\n"
            "Walang ahente na nagtatakda. Walang patong sa customer.\n\n"
            "Sa Tarbaho lagay mo average price mo kada serbisyo. Gabay lang yan, kayo ng customer magkakasundo "
            "sa final. Sa Requests, ikaw rin mag-ooffer ng presyo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w25-ate-linis", side="worker", stage="solution", season="all", icon="clean",
        problem="Ate, sanay ka maglinis ng bahay?",
        sub="May mga pamilya na naghahanap ng tulong pag Sabado.",
        answer="Sa Tarbaho gawa ka ng profile, lagay mo presyo mo. Ikaw pipili ng oras.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Ate, sanay ka maglinis ng bahay?\n\n"
            "May mga pamilya na naghahanap ng tulong pag Sabado.\n\n"
            "Sa Tarbaho gawa ka ng profile, libre ngayon, tapos lagay mo presyo mo. Ikaw pipili ng oras "
            "tsaka ng bayan: Angono, Antipolo o Taytay.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w26-baha", side="worker", stage="solution", season="Jun-Oct", icon="drop",
        problem="Pag tapos ng baha, andaming aayusin.",
        sub="Barado, basang kisame, sirang saksakan.",
        answer="Nasa Tarbaho mga bahay na binaha, may picture at budget. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Pag tapos ng baha, andaming aayusin.\n\n"
            "Barado, basang kisame, sirang saksakan. Kailangan ka ng mga bahay.\n\n"
            "Nasa Tarbaho mga request dito sa Angono, Antipolo at Taytay, may picture at budget. "
            "Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Baha #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w27-foreman", side="worker", stage="solution", season="all", icon="construction",
        problem="Foreman ka? Kaya mo renovation.",
        sub="Extension, second floor, kusina. May mga nagpapagawa.",
        answer="Nasa Tarbaho mga request, may picture at budget. Arawan o pakyaw, ikaw bahala.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Foreman ka? Kaya mo renovation.\n\n"
            "Extension, second floor, kusina. May mga bahay na nagpapagawa.\n\n"
            "Nasa Tarbaho mga request dito sa Angono, Antipolo at Taytay, may picture at budget. "
            "Offer ka ng presyo mo, arawan man o pakyaw.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w28-why", side="worker", stage="product", season="all", icon="search",
        problem="Bakit Tarbaho, hindi FB group?",
        sub="Malapit lang. Kita mo budget bago pumunta.",
        answer="Angono, Antipolo at Taytay lang. Chat muna sa app, number pag pinili ka na.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Bakit Tarbaho, hindi FB group?\n\n"
            "Sa group, post ka nang post, natatabunan ka lang.\n\n"
            "Dito malapit lang, Angono, Antipolo at Taytay. Kita mo request, picture at budget bago ka pumunta. "
            "Ikaw magsasabi ng presyo mo. Chat muna sa app, number pag pinili ka na.\n\n"
            "I-download ang Tarbaho. Libre. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w29-uwi", side="worker", stage="unaware", season="all", icon="briefcase",
        problem="Umuwi ka galing abroad?",
        sub="May skills ka, wala pang suki dito.",
        answer="Sa Tarbaho gawa ka ng profile. Makikita ka ng mga bahay dito sa inyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Umuwi ka galing abroad?\n\n"
            "May skills ka, wala pang suki dito.\n\n"
            "Sa Tarbaho gawa ka ng profile, libre ngayon: serbisyo mo, presyo mo, mga bayan mo. Makikita ka ng "
            "mga bahay dito sa Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #OFW #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w30-tag-ulan-now", side="worker", stage="most", season="Jun-Oct", icon="roofing",
        problem="Tag-ulan na. Mag-profile ka na.",
        sub="Andami na nagpapaayos ng bubong.",
        answer="Libre mag-setup ng profile ngayon. Angono, Antipolo at Taytay.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Tag-ulan na. Mag-profile ka na.\n\n"
            "Andami na nagpapaayos ng bubong, alulod at kisame.\n\n"
            "Sa Tarbaho libre mag-setup ng profile ngayon. Mga taga-Angono, Antipolo at Taytay ung magme-message, "
            "tapos kita mo rin mga request nila.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #TagUlan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w31-tubero", side="worker", stage="solution", season="Jun-Oct", icon="plumbing",
        problem="Tubero ka? Tag-ulan na.",
        sub="CR, lababo, kanal. Barado lahat.",
        answer="Nasa Tarbaho mga request dito sa inyo, may picture at budget. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Tubero ka? Tag-ulan na.\n\n"
            "CR, lababo, kanal. Barado lahat, sunod-sunod nagpapaayos.\n\n"
            "Nasa Tarbaho mga request dito sa Angono, Antipolo at Taytay, may picture at budget. "
            "Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Tubero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w32-bayarin", side="worker", stage="problem", season="all", icon="wallet",
        problem="Andaming bayarin?",
        sub="Hulog, tuition, kuryente. Sabay-sabay pa.",
        answer="Sa Tarbaho dagdag raket dito sa inyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Andaming bayarin?\n\n"
            "Hulog sa motor, tuition, kuryente. Sabay-sabay pa.\n\n"
            "Sa Tarbaho dagdag customer dito sa inyo. Mga taga-Angono, Antipolo at Taytay ung "
            "magme-message, ikaw magsasabi ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w33-karpintero", side="worker", stage="solution", season="all", icon="carpentry",
        problem="Karpintero ka? Andaming sira.",
        sub="Pinto, cabinet, kisame.",
        answer="Nasa Tarbaho mga bahay na may sirang pinto at cabinet. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Karpintero ka?\n\n"
            "Pinto na ayaw sumara, cabinet na bumigay, kisame na lumulubog. Andaming sira dito sa inyo.\n\n"
            "Nasa Tarbaho mga request dito sa Angono, Antipolo at Taytay, may picture at budget. "
            "Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Karpintero #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w34-reviews", side="worker", stage="product", season="all", icon="level-3",
        problem="Bawat natapos mo, may review.",
        sub="Nadadagdag sa profile mo.",
        answer="Sa Tarbaho nasa profile mo lahat ng review mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Bawat natapos mo, may review.\n\n"
            "Nadadagdag sa profile mo, kasama picture ng gawa mo at ilang taon ka na.\n\n"
            "Sa Tarbaho kita ng mga taga-Angono, Antipolo at Taytay ung reviews mo bago sila mag-message.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w35-summer-now", side="worker", stage="most", season="Mar-May", icon="thermometer",
        problem="Summer na. Mag-profile ka na.",
        sub="Init na, andami na nagpapalinis ng aircon.",
        answer="Libre mag-setup ngayon. Gawa ka na bago peak.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Summer na. Mag-profile ka na.\n\n"
            "Init na. Andami na nagpapalinis ng aircon, nagpapakabit, nagpapa-check ng freon.\n\n"
            "Sa Tarbaho libre mag-setup ng profile ngayon. Mga taga-Angono, Antipolo at Taytay ung magme-message, "
            "tapos kita mo rin mga request nila.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Aircon #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w36-nakalampas", side="worker", stage="unaware", season="all", icon="pin",
        problem="Ilang customer na lumampas sayo?",
        sub="Malapit lang sila, di ka lang nila nakita.",
        answer="Sa Tarbaho makikita ka ng mga bahay dito sa inyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Ilang customer na lumampas sayo?\n\n"
            "Malapit lang sila, di ka lang nila nakita, kaya iba natawagan.\n\n"
            "Sa Tarbaho makikita ka ng mga taga-Angono, Antipolo at Taytay, kasama presyo mo at reviews.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w37-garden", side="worker", stage="solution", season="Jun-Oct", icon="garden",
        problem="Marunong ka mag-garden?",
        sub="Tag-ulan, tubo nang tubo damo.",
        answer="Nasa Tarbaho mga bahay na nagpapalinis ng bakuran. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Marunong ka mag-garden?\n\n"
            "Tag-ulan, tubo nang tubo damo sa mga bakuran.\n\n"
            "Nasa Tarbaho mga bahay dito sa Angono, Antipolo at Taytay na nagpapalinis ng bakuran. "
            "Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w38-notif", side="worker", stage="product", season="all", icon="chat-alert",
        problem="Customer na lalapit sayo.",
        sub="Di mo na kailangan mag-post araw-araw.",
        answer="Sa Tarbaho may notif ka pag may nag-message.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Customer na lalapit sayo.\n\n"
            "Di mo na kailangan mag-post araw-araw sa FB group.\n\n"
            "Sa Tarbaho may profile ka na kita ng mga taga-Angono, Antipolo at Taytay. Pag may "
            "nag-message, may notif ka.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w39-disyembre", side="worker", stage="problem", season="Nov-Dec", icon="building",
        problem="Disyembre. Tigil site.",
        sub="Pero tuloy Noche Buena.",
        answer="Sa Tarbaho may mga bahay na nagpapaayos bago Pasko. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Disyembre. Tigil site.\n\n"
            "Pero tuloy Noche Buena, regalo, bayarin.\n\n"
            "Sa Tarbaho may mga bahay dito sa Angono, Antipolo at Taytay na nagpapaayos bago Pasko: "
            "pintura, ilaw, gripo, kisame. Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w40-it", side="worker", stage="solution", season="all", icon="it",
        problem="IT ka? Andaming mahina WiFi.",
        sub="WFH, online class. Kailangan ka nila.",
        answer="Nasa Tarbaho mga request dito sa inyo: WiFi, laptop, printer, CCTV. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "IT ka? Andaming mahina WiFi.\n\n"
            "WFH, online class, CCTV. Kailangan ka ng mga bahay dito sa inyo.\n\n"
            "Nasa Tarbaho mga request dito sa Angono, Antipolo at Taytay: WiFi, laptop, printer, "
            "CCTV. Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #IT #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w41-umaga", side="worker", stage="solution", season="all", icon="bag",
        problem="Bakante ka pag umaga?",
        sub="Grocery, PasaBuy, bayad bills.",
        answer="Sa Tarbaho tanggap ka ng pakisuyo dito sa inyo. Ikaw magsasabi ng presyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Bakante ka pag umaga?\n\n"
            "Grocery, PasaBuy, bayad bills. May mga bahay na need ng tulong.\n\n"
            "Sa Tarbaho tanggap ka ng pakisuyo dito sa Angono, Antipolo o Taytay. Ikaw magsasabi ng presyo mo "
            "tsaka oras mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Pakisuyo #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w42-tatlong-bayan", side="worker", stage="most", season="all", icon="people",
        problem="Isang profile, tatlong bayan.",
        sub="Angono, Antipolo, Taytay.",
        answer="Ikaw pipili kung san ka lang pupunta. Sila magme-message.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Isang profile, tatlong bayan.\n\n"
            "Angono, Antipolo, Taytay. Ikaw pipili kung san ka lang pupunta.\n\n"
            "Libre mag-setup ng profile ngayon. Sila magme-message, tapos kita mo rin "
            "mga request nila.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w43-carwash", side="worker", stage="solution", season="all", icon="car-wash",
        problem="May car wash kit ka?",
        sub="Linis ng kotse sa bahay nila mismo.",
        answer="Sa Tarbaho makikita ka ng mga bahay dito sa inyo. Lagay mo presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "May car wash kit ka?\n\n"
            "Andaming may kotse na walang oras pumila sa carwash. Ikaw na pupunta sa bahay nila.\n\n"
            "Sa Tarbaho makikita ka ng mga taga-Angono, Antipolo at Taytay. Lagay mo presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #CarWash #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w44-photos", side="worker", stage="product", season="all", icon="photos",
        problem="Picture ng gawa mo, nasa profile.",
        sub="Di mo na kailangan ikwento.",
        answer="Sa Tarbaho hanggang 6 na picture ng gawa mo. Kita nila bago mag-message.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Picture ng gawa mo, nasa profile.\n\n"
            "Di mo na kailangan ikwento, kita na nila.\n\n"
            "Sa Tarbaho lagay ka hanggang 6 na picture ng mga natapos mo. Kita ng mga taga-Angono, "
            "Antipolo at Taytay bago sila mag-message.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w45-tapos", side="worker", stage="problem", season="all", icon="foreman",
        problem="Tapos na ung project.",
        sub="San ung susunod?",
        answer="Sa Tarbaho kita mo mga request dito sa inyo habang nag-aantay ng susunod na site.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Tapos na ung project.\n\n"
            "San ung susunod? Kelan ung susunod na tawag?\n\n"
            "Habang nag-aantay, may mga bahay dito sa Angono, Antipolo at Taytay na may ipapagawa. "
            "Nasa Tarbaho mga request nila. Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Construction #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w46-id-badge", side="worker", stage="product", season="all", icon="person",
        problem="May ID badge ka na?",
        sub="Kita yan sa profile mo.",
        answer="Sa Tarbaho send mo picture ng ID mo. Pag na-check, may badge ka sa profile.",
        cta="I-download ang Tarbaho.",
        caption=(
            "May ID badge ka na?\n\n"
            "Kita yan ng mga customer sa profile mo.\n\n"
            "Sa Tarbaho send mo picture ng government ID mo. Pag na-check ng team, may badge ka sa profile. "
            "Pwede rin TESDA at past project.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w47-mason", side="worker", stage="solution", season="all", icon="masonry",
        problem="Mason ka?",
        sub="Bakod, pader, bitak.",
        answer="Nasa Tarbaho mga bahay na may ipapagawang pader. Arawan o pakyaw, ikaw bahala.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Mason ka?\n\n"
            "Bakod, pader, bitak, sahig. Andaming bahay dito sa inyo na may ipapagawa.\n\n"
            "Nasa Tarbaho mga request dito sa Angono, Antipolo at Taytay, may picture at budget. "
            "Offer ka ng presyo mo, arawan man o pakyaw.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Mason #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w48-libre", side="worker", stage="most", season="all", icon="level-4",
        problem="Libre mag-profile ngayon.",
        sub="Ilang minuto lang setup.",
        answer="Gawa ka na ng profile sa Tarbaho. Angono, Antipolo at Taytay.",
        cta="I-download ang Tarbaho ngayon",
        caption=(
            "Libre mag-profile ngayon.\n\n"
            "Libre mag-sign up at mag-setup ngayon. Ilang minuto lang.\n\n"
            "Gawa ka na ng profile sa Tarbaho para makita ka ng mga taga-Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho ngayon. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w49-biyahe", side="worker", stage="problem", season="all", icon="wheel",
        problem="Isang oras na biyahe.",
        sub="Para sa ₱500 na gawa.",
        answer="Sa Tarbaho ikaw pipili kung gaano kalayo. Malapit lang ung customer.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Isang oras na biyahe.\n\n"
            "Para sa ₱500 na gawa. Kalahati, napunta lang sa gas.\n\n"
            "Sa Tarbaho ikaw pipili kung gaano kalayo ka lang pupunta. Mga customer dito sa Angono, Antipolo "
            "at Taytay na malapit sayo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w50-tuition", side="worker", stage="problem", season="May-Jun", icon="document",
        problem="Para sa tuition ni anak.",
        sub="Bawat raket, laking tulong.",
        answer="Sa Tarbaho dagdag customer dito sa inyo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Para sa tuition ni anak.\n\n"
            "Pasukan na. Bawat raket, laking tulong.\n\n"
            "Sa Tarbaho dagdag customer dito sa Angono, Antipolo at Taytay. Sila magme-message, "
            "ikaw magsasabi ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Pasukan #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w51-suki-mo", side="worker", stage="solution", season="all", icon="phone",
        problem="Sayo ba talaga suki mo?",
        sub="O sa ahente?",
        answer="Sa Tarbaho ikaw mismo kausap ng customer.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Sayo ba talaga suki mo?\n\n"
            "O sa ahente na kumukuha ng parte?\n\n"
            "Sa Tarbaho ikaw mismo kausap ng customer. Chat sa app, ikaw magsasabi ng presyo. "
            "Angono, Antipolo at Taytay.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="w52-linis-season", side="worker", stage="solution", season="Nov-Dec", icon="house-clean",
        problem="Linis season na, Pasko na.",
        sub="General cleaning bago Pasko.",
        answer="Nasa Tarbaho mga bahay na nagpapalinis. Offer ka ng presyo mo.",
        cta="I-download ang Tarbaho.",
        caption=(
            "Linis season na, Pasko na.\n\n"
            "General cleaning bago Pasko: kisame, bintana, kurtina, CR.\n\n"
            "Nasa Tarbaho mga bahay dito sa Angono, Antipolo at Taytay na nagpapalinis. "
            "Offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n#Tarbaho #Pasko #Angono #Antipolo #Taytay"
        ),
    ),
    # ---- First post: early access (the app is not on Google Play yet; the CTA is the form) ----
    # Voice: plain, the way a worker texts a kasama on Facebook. The hooks and the answer line are the owner's,
    # word for word; no jokes of our own.
    dict(
        id="w53-foreman", side="worker", stage="problem", season="all", icon="foreman",
        problem="Antagal ng tawag ni Foreman?",
        sub="Tapos na ang project, wala pang kasunod.",
        answer="Dito sa Tarbaho, makakahanap ka ng sarili mong customer sa presyong gusto mo.",
        cta="Mag-sign up: early access",
        towns="Only available in Angono, Antipolo and Taytay for now",
        caption=(
            "Antagal ng tawag ni Foreman?\n\n"
            "Tapos na ung project, wala pang kasunod.\n\n"
            "Dito sa Tarbaho, makakahanap ka ng sarili mong customer sa presyong gusto mo.\n\n"
            "Di pa bukas ung app. Pag gusto mo mauna, fill up mo lang to:\n"
            "👉 [GOOGLE FORM LINK]\n\n"
            "Lagay mo ung Gmail na gamit mo sa Play Store, dun namin isesend ung early access. "
            "Naka-iPhone? Sa TestFlight ka na diretso: https://testflight.apple.com/join/1CNn1Ywt\n\n"
            "Angono, Antipolo at Taytay muna.\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Construction #Aircon"
        ),
    ),
    dict(
        id="w54-project", side="worker", stage="problem", season="all", icon="construction",
        problem="Patapos na naman ang project, ano nang kasunod?",
        sub="Wag mo na hintayin ung tawag.",
        answer="Dito sa Tarbaho, makakahanap ka ng sarili mong customer sa presyong gusto mo.",
        cta="Mag-sign up: early access",
        towns="Only available in Angono, Antipolo and Taytay for now",
        caption=(
            "Patapos na naman ang project, ano nang kasunod?\n\n"
            "Wag mo na hintayin ung tawag.\n\n"
            "Dito sa Tarbaho, makakahanap ka ng sarili mong customer sa presyong gusto mo.\n\n"
            "Di pa bukas ung app pero pwede ka na mauna. Fill up mo lang to:\n"
            "👉 [GOOGLE FORM LINK]\n\n"
            "Gmail na gamit mo sa Play Store ung ilagay mo. Naka-iPhone? Sa TestFlight ka na diretso: https://testflight.apple.com/join/1CNn1Ywt\n\n"
            "Angono, Antipolo at Taytay muna.\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Construction #Aircon"
        ),
    ),
]
