# Carousels: 4 square slides each, uploaded in order as one post. build.py renders out/carousel-<id>-<n>.png.
# kind: "problem" (white, big question), "cost" (ink, what it costs them), "how" (white, numbered steps),
# "cta" (purple, the download line). Same copy rules as posts.py: households in English, workers in Tagalog.

CAROUSELS = [
    # "How it works": one image per side (a single slide: no count, ends on the download line).
    dict(
        id="how-household", side="household", single=True,
        slides=[dict(kind="how",
                     lead="Hirap humanap ng gagawa sa bahay?",
                     title="Ganito lang sa Tarbaho:",
                     steps=["I-post ang kailangan mo, may picture at budget",
                            "Mga skilled worker na malapit ang mag-o-offer ng presyo",
                            "Ikumpara ang offers at reviews, saka pumili",
                            "Bayad nang diretso pagkatapos ng trabaho"],
                     cta="Download Tarbaho. Libre.")],
        caption=(
            "Hirap humanap ng gagawa sa bahay? Ganito lang sa Tarbaho: 👇\n\n"
            "1️⃣ I-post ang kailangan mo: picture, kung saan ka, at budget mo.\n"
            "2️⃣ Mga skilled worker sa Angono, Antipolo at Taytay ang mag-o-offer ng presyo.\n"
            "3️⃣ Ikumpara ang offers at reviews, saka pumili.\n"
            "4️⃣ Bayad nang diretso pagkatapos ng trabaho, sa presyong pinagkasunduan.\n\n"
            "Walang patong. Libre i-download. 👉 {link}\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Rizal"
        ),
    ),
    dict(
        id="how-worker", side="worker", single=True,
        slides=[dict(kind="how",
                     lead="Gusto mo ng dagdag na customer?",
                     title="Ganito gumagana ang Tarbaho:",
                     steps=["Gumawa ng libreng profile: serbisyo, presyo, at mga bayan mo",
                            "Ang mga household na malapit ang magme-message sa'yo",
                            "Sa Requests, mag-alok ng sarili mong presyo",
                            "Pag pinili ka, chat muna sa app, tapos gawa na"],
                     cta="I-download ang Tarbaho.")],
        caption=(
            "Gusto mo ng dagdag na customer? Ganito gumagana ang Tarbaho: 👇\n\n"
            "1️⃣ Gumawa ng libreng profile: ang serbisyo mo, ang presyo mo, at ang mga bayan na pinupuntahan mo.\n"
            "2️⃣ Ang mga household sa Angono, Antipolo at Taytay ang magme-message sa'yo.\n"
            "3️⃣ Sa Requests, makikita mo ang mga bahay na may kailangang ipagawa. Mag-alok ng sarili mong presyo.\n"
            "4️⃣ Pag pinili ka, chat muna sa app, tapos gawa na. Bayad pagkatapos, sa presyong napagkasunduan.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n"
            "#Tarbaho #Sideline #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="household", side="household",
        slides=[
            dict(kind="problem", icon="home-repair",
                 title="May sira na naman sa bahay?",
                 body="Aircon, gripo, breaker, kisame. Laging sa pinaka-busy mong araw."),
            dict(kind="cost", icon="receipt",
                 title="Tatlong araw kang nag-antay ng reply.",
                 body="Tapos ang ₱800, naging ₱2,000 pagkatapos ng trabaho."),
            dict(kind="how",
                 title="Sa Tarbaho, ganito lang:",
                 steps=["I-post ang sira at ang budget mo",
                        "Mga malapit sa'yo ang mag-o-offer ng presyo",
                        "Pumili, at bayad pagkatapos ng trabaho"]),
            dict(kind="cta", title="Download Tarbaho.",
                 body="Libre. Walang patong. Ikaw ang pipili."),
        ],
        caption=(
            "May sira na naman sa bahay? 🏠\n\n"
            "Swipe para makita kung paano mas madali sa Tarbaho. 👉\n\n"
            "I-post ang kailangan at ang budget mo. Mga skilled worker sa Angono, Antipolo at Taytay ang mag-o-offer. "
            "Pumili, at bayad pagkatapos ng trabaho.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Rizal"
        ),
    ),
    dict(
        id="worker", side="worker",
        slides=[
            dict(kind="problem", icon="toolbox",
                 title="Marunong ka. Pero walang tumatawag?",
                 body="Aircon, tubo, kuryente, construction, paglilinis. Kaya mo lahat 'yan."),
            dict(kind="cost", icon="wallet",
                 title="Bawat araw na walang customer, walang kita.",
                 body="At kung may customer man, may patong pa ang ahente."),
            dict(kind="how",
                 title="Sa Tarbaho, ganito lang:",
                 steps=["Gumawa ng libreng profile: serbisyo, presyo, at mga bayan mo",
                        "Ang mga household na malapit ang magme-message sa'yo",
                        "Tingnan ang Requests at mag-alok ng sarili mong presyo"]),
            dict(kind="cta", title="I-download ang Tarbaho.",
                 body="Ikaw ang bahala sa presyo mo."),
        ],
        caption=(
            "Marunong ka. Pero walang tumatawag? 🧰\n\n"
            "Swipe para makita kung paano ka mahahanap ng mga customer sa bayan mo. 👉\n\n"
            "Sa Tarbaho, gumawa ng libreng profile. Ang mga household sa Angono, Antipolo at Taytay ang magme-message "
            "sa'yo, at makikita mo ang mga Request nila para makapag-alok ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n"
            "#Tarbaho #Sideline #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    # First post: how a worker sets up the profile. The app is not out yet, so it ends on the early-access form,
    # never the store. Voice: a kasama sa site joking around, short and plain.
    dict(
        id="setup-worker", side="worker",
        slides=[
            dict(kind="problem", icon="worker",
                 title="Ilang minuto lang ang setup.",
                 body="Mas mabilis pa sa tawag ni Foreman."),
            dict(kind="how", lead="Hakbang 1 hanggang 3",
                 title="Sign in, tapos trade mo.",
                 steps=["English o Taglish",
                        "Mobile number mo, tapos SMS code",
                        "Skilled worker, tapos trade at serbisyo mo"]),
            dict(kind="how", lead="Hakbang 4 hanggang 6", start=4,
                 title="Presyo, bayan, taon.",
                 steps=["Presyo mo: mula ₱… hanggang ₱…",
                        "Mga bayan na pinupuntahan mo",
                        "Ilang taon ka na sa linya mo"]),
            dict(kind="how", lead="Hakbang 7 hanggang 9", start=7,
                 title="Selfie, number, Game!",
                 steps=["Selfie: kahit hindi artistahin, basta malinaw",
                        "I-confirm ang number sa SMS",
                        "Silipin ang card, mangako, Game!"]),
            dict(kind="problem", icon="medal",
                 title="Gusto mong mas makita?",
                 body="Picture ng gawa mo (hanggang 6) at badges: ID, TESDA, past project. "
                      "Hindi required, pero mas mataas ka sa search."),
            dict(kind="problem", icon="chat",
                 title="Live ka na.",
                 body="Kita mo ang requests sa malapit. Mag-offer ng presyo mo. Number mo, pag pinili ka lang."),
            dict(kind="cta", title="Mag-sign up: early access.",
                 body="Hindi pa bukas ang app. Link sa caption.",
                 towns="Only available in Angono, Antipolo and Taytay for now"),
        ],
        caption=(
            "Ilang minuto lang ang setup sa Tarbaho. Mas mabilis pa sa tawag ni Foreman. "
            "(I-swipe)\n\n"
            "1. English o Taglish\n"
            "2. Mobile number mo, tapos SMS code\n"
            "3. \"Skilled worker\", tapos trade at serbisyo mo\n"
            "4. Presyo mo: mula ₱… hanggang ₱…, o \"depende sa trabaho\"\n"
            "5. Mga bayan na pinupuntahan mo\n"
            "6. Ilang taon ka na sa linya mo\n"
            "7. Selfie (kahit hindi artistahin, basta malinaw)\n"
            "8. I-confirm ang number sa SMS\n"
            "9. Silipin ang card mo, mangako, tapos \"Game!\"\n\n"
            "Dito sa Tarbaho, makakahanap ka ng sarili mong customer sa presyong gusto mo.\n\n"
            "Hindi pa bukas ang app. Gusto mong mauna? Sagutan ang form:\n"
            "👉 [GOOGLE FORM LINK]\n\n"
            "Gmail na gamit mo sa Play Store ang ilagay. Naka-iPhone? Pwede rin via TestFlight.\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Construction #Aircon"
        ),
    ),
]
