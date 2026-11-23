# Carousels: 4 square slides each, uploaded in order as one post. build.py renders out/carousel-<id>-<n>.png.
# kind: "problem" (white, big question), "cost" (ink, what it costs them), "how" (white, numbered steps),
# "cta" (purple, the download line). Same voice and rules as posts.py: casual Facebook Taglish for both sides.

CAROUSELS = [
    # "How it works": one image per side (a single slide: no count, ends on the download line).
    dict(
        id="how-household", side="household", single=True,
        slides=[dict(kind="how",
                     lead="Hirap maghanap ng gagawa sa bahay?",
                     title="Ganito lang sa Tarbaho:",
                     steps=["Post mo kailangan mo, may picture at budget",
                            "Mga malapit sayo mag-ooffer ng presyo",
                            "Compare mo offers at reviews, tapos pili ka",
                            "Bayad mo diretso sa kanya pag tapos na"],
                     cta="Download Tarbaho. Libre.")],
        caption=(
            "Hirap maghanap ng gagawa sa bahay? Ganito lang sa Tarbaho:\n\n"
            "1. Post mo kailangan mo: picture, kung san ka, tsaka budget mo.\n"
            "2. Mga malapit sayo dito sa Angono, Antipolo at Taytay ung mag-ooffer ng presyo.\n"
            "3. Compare mo offers at reviews, tapos pili ka.\n"
            "4. Bayad mo diretso sa kanya pag tapos na ung gawa, sa presyong napag-usapan nyo.\n\n"
            "Walang patong ng ahente. Libre i-download. 👉 {link}\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Rizal"
        ),
    ),
    dict(
        id="how-worker", side="worker", single=True,
        slides=[dict(kind="how",
                     lead="Gusto mo ng dagdag na customer?",
                     title="Ganito sa Tarbaho:",
                     steps=["Gawa ka ng profile: serbisyo, presyo, bayan mo",
                            "Sila na magme-message sayo",
                            "Sa Requests, offer ka ng sarili mong presyo",
                            "Pag pinili ka, chat muna sa app, tapos gawa na"],
                     cta="I-download ang Tarbaho.")],
        caption=(
            "Gusto mo ng dagdag na customer? Ganito sa Tarbaho:\n\n"
            "1. Gawa ka ng profile, libre ngayon: serbisyo mo, presyo mo, tsaka mga bayan na pinupuntahan mo.\n"
            "2. Mga taga-Angono, Antipolo at Taytay ung magme-message sayo.\n"
            "3. Sa Requests, kita mo mga bahay na may ipapagawa. Offer ka ng sarili mong presyo.\n"
            "4. Pag pinili ka, chat muna sa app, tapos gawa na. Bayad pag tapos, sa presyong napag-usapan nyo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n"
            "#Tarbaho #Sideline #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    dict(
        id="household", side="household",
        slides=[
            dict(kind="problem", icon="home-repair",
                 title="May sira na naman sa bahay?",
                 body="Aircon, gripo, breaker, kisame. Lagi pa sa pinaka-busy mong araw."),
            dict(kind="cost", icon="receipt",
                 title="Tatlong araw bago nag-reply.",
                 body="Tapos ung ₱800, naging ₱2,000 pag tapos na."),
            dict(kind="how",
                 title="Sa Tarbaho, ganito lang:",
                 steps=["Post mo ung sira tsaka budget mo",
                        "Mga malapit sayo mag-ooffer ng presyo",
                        "Pili ka, bayad pag tapos na ung gawa"]),
            dict(kind="cta", title="Download Tarbaho.",
                 body="Walang patong ng ahente. Ikaw pipili."),
        ],
        caption=(
            "May sira na naman sa bahay?\n\n"
            "Swipe mo lang para makita paano sa Tarbaho.\n\n"
            "Post mo kailangan mo tsaka budget mo. Mga malapit sayo dito sa Angono, Antipolo at Taytay ung "
            "mag-ooffer. Pili ka, bayad pag tapos na ung gawa.\n\n"
            "Download Tarbaho. Libre. 👉 {link}\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Rizal"
        ),
    ),
    dict(
        id="worker", side="worker",
        slides=[
            dict(kind="problem", icon="toolbox",
                 title="Marunong ka, pero walang tumatawag?",
                 body="Aircon, tubo, kuryente, construction, linis. Kaya mo lahat yan."),
            dict(kind="cost", icon="wallet",
                 title="Pag walang customer, walang kita.",
                 body="Tapos pag meron, may patong pa ung ahente."),
            dict(kind="how",
                 title="Sa Tarbaho, ganito lang:",
                 steps=["Gawa ka ng profile: serbisyo, presyo, bayan mo",
                        "Sila na magme-message sayo",
                        "Tingnan mo Requests, offer ka ng presyo mo"]),
            dict(kind="cta", title="I-download ang Tarbaho.",
                 body="Ikaw bahala sa presyo mo."),
        ],
        caption=(
            "Marunong ka, pero walang tumatawag?\n\n"
            "Swipe mo lang para makita paano ka mahahanap ng customer dito sa inyo.\n\n"
            "Sa Tarbaho gawa ka ng profile, libre ngayon. Mga taga-Angono, Antipolo at Taytay ung magme-message "
            "sayo, tapos kita mo rin mga request nila para maka-offer ka ng presyo mo.\n\n"
            "I-download ang Tarbaho. 👉 {link}\n\n"
            "#Tarbaho #Sideline #Raket #Angono #Antipolo #Taytay"
        ),
    ),
    # First post: how a worker sets up the profile. The app is not out yet, so it ends on the early-access form,
    # never the store. Voice: plain Facebook Taglish, just the steps, no jokes.
    dict(
        id="setup-worker", side="worker",
        slides=[
            dict(kind="problem", icon="worker",
                 title="Ganito mag-setup ng profile.",
                 body="Pag bukas na ung app. Ilang minuto lang."),
            dict(kind="how", lead="Step 1 to 3",
                 title="Sign in, tapos trade mo.",
                 steps=["English o Taglish",
                        "Number mo, tapos ung SMS code",
                        "Skilled worker, tapos trade at services mo"]),
            dict(kind="how", lead="Step 4 to 6", start=4,
                 title="Presyo, bayan, taon.",
                 steps=["Presyo mo, from ₱… to ₱…",
                        "Mga bayan na pinupuntahan mo",
                        "Ilang taon ka na sa trabaho"]),
            dict(kind="how", lead="Step 7 to 9", start=7,
                 title="Selfie, number, Game!",
                 steps=["Selfie mo, dapat kita mukha",
                        "Confirm mo ung number sa SMS",
                        "Check mo ung card, tick ung promises, Game!"]),
            dict(kind="problem", icon="medal",
                 title="Gusto mo mas makita?",
                 body="Lagay ka pics ng gawa mo (hanggang 6) at badges: ID, TESDA, past project. "
                      "Di required pero mas mataas ka sa search."),
            dict(kind="problem", icon="chat",
                 title="Live ka na.",
                 body="Kita mo ung requests sa malapit. Offer ka ng presyo mo. "
                      "Number mo, pag pinili ka lang."),
            dict(kind="cta", title="Mag-sign up: early access.",
                 body="Di pa bukas ung app. Link nasa caption.",
                 towns="Only available in Angono, Antipolo and Taytay for now"),
        ],
        caption=(
            "Ganito mag-setup ng profile sa Tarbaho pag bukas na ung app. Swipe mo lang\n\n"
            "1. English o Taglish\n"
            "2. Number mo, tapos ung SMS code\n"
            "3. Pili ka ng \"Skilled worker\", tapos trade at services mo\n"
            "4. Presyo mo, from ₱… to ₱… o \"depende sa trabaho\"\n"
            "5. Mga bayan na pinupuntahan mo\n"
            "6. Ilang taon ka na sa trabaho\n"
            "7. Selfie mo, dapat kita mukha\n"
            "8. Confirm mo ung number sa SMS\n"
            "9. Check mo ung card mo, tick ung promises, tapos \"Game!\"\n\n"
            "Dito sa Tarbaho, makakahanap ka ng sarili mong customer sa presyong gusto mo.\n\n"
            "Di pa bukas ung app. Pag gusto mo mauna, fill up mo lang to:\n"
            "👉 [GOOGLE FORM LINK]\n\n"
            "Gmail na gamit mo sa Play Store ung ilagay mo. Naka-iPhone? Sa TestFlight ka na diretso: https://testflight.apple.com/join/1CNn1Ywt\n\n"
            "#Tarbaho #Angono #Antipolo #Taytay #Construction #Aircon"
        ),
    ),
]
