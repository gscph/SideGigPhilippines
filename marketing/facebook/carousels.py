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
                            "Sa'yo ang buong bayad. Walang komisyon."],
                     cta="I-download ang Tarbaho. Walang komisyon.")],
        caption=(
            "Gusto mo ng dagdag na customer? Ganito gumagana ang Tarbaho: 👇\n\n"
            "1️⃣ Gumawa ng libreng profile: ang serbisyo mo, ang presyo mo, at ang mga bayan na pinupuntahan mo.\n"
            "2️⃣ Ang mga household sa Angono, Antipolo at Taytay ang magme-message sa'yo.\n"
            "3️⃣ Sa Requests, makikita mo ang mga bahay na may kailangang ipagawa. Mag-alok ng sarili mong presyo.\n"
            "4️⃣ Sa'yo ang buong bayad. Walang komisyon.\n\n"
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
                 body="Walang komisyon. Sa'yo ang buong bayad."),
        ],
        caption=(
            "Marunong ka. Pero walang tumatawag? 🧰\n\n"
            "Swipe para makita kung paano ka mahahanap ng mga customer sa bayan mo. 👉\n\n"
            "Sa Tarbaho, gumawa ng libreng profile. Ang mga household sa Angono, Antipolo at Taytay ang magme-message "
            "sa'yo, at makikita mo ang mga Request nila para makapag-alok ka ng presyo mo.\n\n"
            "Walang komisyon. Sa'yo ang buong bayad. 👉 {link}\n\n"
            "#Tarbaho #Sideline #Raket #Angono #Antipolo #Taytay"
        ),
    ),
]
