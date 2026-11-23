# Builds the Facebook images and POSTS.md from posts.py.
#   python marketing/facebook/build.py            all posts
#   python marketing/facebook/build.py h1-aircon  one post
# Each post: out/<id>-feed.png (1080x1080, feed) and out/<id>-story.png (1080x1920, story/reel cover).
# Renders HTML with Edge (or Chrome) headless, using the app's own font, colors and icons.
import html, os, pathlib, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent.parent
OUT = HERE / "out"
WORK = HERE / "work"
ICONS = REPO / "SideGigPh.Maui" / "wwwroot" / "images" / "icons"
FONT = (REPO / "SideGigPh.Maui" / "wwwroot" / "fonts" / "dm-sans.woff2").as_uri()
LINK = "[Google Play link]"  # put the store link here once it's public

sys.path.insert(0, str(HERE))
from carousels import CAROUSELS  # noqa: E402
from posts import POSTS  # noqa: E402

# Chrome first: Edge headless sometimes exits before writing the screenshot.
BROWSERS = [
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
]

# The Folded T logo (AppTheme.Logo), 24-unit box.
LOGO = ('<svg viewBox="0 0 24 24" fill="{c}"><path d="M4.08 3.6H13.2L11.76 7.92H2.64Z"/>'
        '<path fill-opacity="0.78" d="M13.2 3.6H21.36L19.92 7.92H11.76Z"/>'
        '<path fill-opacity="0.62" d="M10.32 7.92H15.12L11.76 20.4H6.96Z"/>'
        '<path d="M10.32 7.92H15.12L13.68 10.8Z"/></svg>')

CSS = """
@font-face { font-family: 'DM Sans'; src: url('%(font)s') format('woff2'); font-weight: 100 1000; }
* { box-sizing: border-box; margin: 0; padding: 0; }
html, body { width: %(w)dpx; height: %(h)dpx; overflow: hidden; }
body { font-family: 'DM Sans', sans-serif; background: #ffffff; color: #222222; display: flex; flex-direction: column; }
.top { flex: 1; padding: %(pad)dpx %(pad)dpx 0; display: flex; flex-direction: column; position: relative; }
.brand { display: flex; align-items: center; gap: 14px; font-weight: 800; font-size: 40px; color: #5B2A86; letter-spacing: -0.5px; }
.brand svg { width: 56px; height: 56px; }
.tag { margin-left: auto; font-size: 26px; font-weight: 700; color: #ffffff; background: %(tagbg)s;
       padding: 10px 22px; border-radius: 999px; letter-spacing: 0.3px; }
.art { position: %(artpos)s; right: %(artright)s; top: %(arttop)dpx; width: %(art)dpx; height: %(art)dpx;
       border-radius: 50%%; background: #F3ECF9; display: flex; align-items: center; justify-content: center;
       flex: none; margin: %(artmargin)s; }
.art svg { width: 64%%; height: 64%%; }
.copy { margin-top: auto; padding-bottom: %(gap)dpx; }
.problem { font-size: %(pf)dpx; font-weight: 800; line-height: 1.02; letter-spacing: -2px; max-width: %(pw)dpx; }
.sub { margin-top: 22px; font-size: %(sf)dpx; line-height: 1.3; color: #6a6a6a; max-width: %(sw)dpx; }
.bottom { background: #5B2A86; color: #ffffff; padding: %(bpad)dpx %(pad)dpx; display: flex; flex-direction: column; gap: %(bgap)dpx; }
.arrow { font-size: 26px; font-weight: 700; letter-spacing: 2px; text-transform: uppercase; opacity: 0.75; }
.answer { font-size: %(af)dpx; line-height: 1.28; font-weight: 500; }
.row { display: flex; align-items: center; gap: 22px; flex-wrap: wrap; }
.cta { background: #ffffff; color: #5B2A86; font-weight: 800; font-size: %(cf)dpx; padding: 18px 30px; border-radius: 14px; }
.towns { font-size: 26px; opacity: 0.85; }
"""

SIZES = {
    "feed": dict(w=1080, h=1080, pad=72, artpos="absolute", artmargin="0", artright="72px", art=300, arttop=170, pf=96, pw=600, sf=36, sw=700, gap=48,
                 bpad=52, bgap=24, af=38, cf=32),
    "story": dict(w=1080, h=1920, pad=84, artpos="static", artmargin="90px auto 40px", artright="auto", art=400, arttop=0, pf=128, pw=900, sf=46, sw=880, gap=80,
                  bpad=96, bgap=40, af=50, cf=40),
}

ARROW = {"household": "Ang solusyon", "worker": "Ang sagot"}
TAG = {"household": ("Para sa bahay", "#222222"), "worker": ("Para sa skilled workers", "#118C4F")}


def icon_svg(name):
    return (ICONS / f"{name}.svg").read_text(encoding="utf-8")


def headline_size(text, base):
    """Long problems get a smaller headline, so they stay clear of the picture and the purple panel."""
    n = len(text)
    scale = 1.0 if n <= 18 else 0.86 if n <= 26 else 0.74 if n <= 34 else 0.64
    return round(base * scale)


def page(post, size):
    s = dict(SIZES[size])
    s["pf"] = headline_size(post["problem"], s["pf"])
    tag, tagbg = TAG[post["side"]]
    css = CSS % dict(s, font=FONT, tagbg=tagbg)
    e = html.escape
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>{css}</style></head><body>
<div class="top">
  <div class="brand">{LOGO.format(c="#5B2A86")}Tarbaho<span class="tag">{e(tag)}</span></div>
  <div class="art">{icon_svg(post["icon"])}</div>
  <div class="copy">
    <div class="problem">{e(post["problem"])}</div>
    <div class="sub">{e(post["sub"])}</div>
  </div>
</div>
<div class="bottom">
  <div class="arrow">{e(ARROW[post["side"]])} &rarr;</div>
  <div class="answer">{e(post["answer"])}</div>
  <div class="row"><span class="cta">{e(post["cta"])}</span><span class="towns">Angono · Antipolo · Taytay</span></div>
</div>
</body></html>"""


SLIDE_CSS = """
@font-face { font-family: 'DM Sans'; src: url('%(font)s') format('woff2'); font-weight: 100 1000; }
* { box-sizing: border-box; margin: 0; padding: 0; }
html, body { width: 1080px; height: 1080px; overflow: hidden; }
body { font-family: 'DM Sans', sans-serif; background: %(bg)s; color: %(fg)s; padding: 80px;
       display: flex; flex-direction: column; }
.head { display: flex; align-items: center; gap: 14px; font-weight: 800; font-size: 36px; color: %(brand)s; }
.head svg { width: 50px; height: 50px; }
.count { margin-left: auto; font-size: 28px; font-weight: 700; opacity: 0.6; color: %(fg)s; }
.art { width: 220px; height: 220px; border-radius: 50%%; background: #F3ECF9; display: flex;
       align-items: center; justify-content: center; margin-top: 70px; }
.art svg { width: 64%%; height: 64%%; }
.biglogo { margin-top: 70px; }
.biglogo svg { width: 180px; height: 180px; }
.main { margin-top: auto; }
.title { font-size: %(tf)dpx; font-weight: 800; line-height: 1.05; letter-spacing: -1.5px; }
.body { margin-top: 26px; font-size: 40px; line-height: 1.3; opacity: 0.85; }
.steps { margin-top: 44px; display: flex; flex-direction: column; gap: 30px; }
.step { display: flex; gap: 24px; align-items: center; font-size: 38px; line-height: 1.25; }
.num { flex: none; width: 64px; height: 64px; border-radius: 50%%; background: #5B2A86; color: #ffffff;
       font-weight: 800; font-size: 34px; display: flex; align-items: center; justify-content: center; }
.towns { margin-top: 40px; font-size: 28px; opacity: 0.85; }
.lead { font-size: 34px; font-weight: 700; color: #5B2A86; margin-bottom: 18px; }
.single { margin-top: 48px; display: flex; align-items: center; gap: 24px; flex-wrap: wrap; }
.single .dl { background: #5B2A86; color: #ffffff; font-weight: 800; font-size: 32px; padding: 18px 30px;
              border-radius: 14px; }
.single .towns { margin-top: 0; opacity: 0.7; }
.swipe { margin-top: 40px; font-size: 28px; font-weight: 700; color: %(brand)s; }
"""

SLIDE_LOOK = {  # background, text, brand color
    "problem": ("#ffffff", "#222222", "#5B2A86"),
    "cost": ("#222222", "#ffffff", "#ffffff"),
    "how": ("#ffffff", "#222222", "#5B2A86"),
    "cta": ("#5B2A86", "#ffffff", "#ffffff"),
}


def slide(carousel, i):
    sl = carousel["slides"][i]
    total = len(carousel["slides"])
    bg, fg, brand = SLIDE_LOOK[sl["kind"]]
    e = html.escape
    tf = 110 if sl["kind"] == "cta" else headline_size(sl["title"], 92)
    css = SLIDE_CSS % dict(font=FONT, bg=bg, fg=fg, brand=brand, tf=tf)
    art = f'<div class="art">{icon_svg(sl["icon"])}</div>' if sl.get("icon") else ""
    body = f'<div class="body">{e(sl["body"])}</div>' if sl.get("body") else ""
    steps = ""
    if sl.get("steps"):
        steps = '<div class="steps">' + "".join(
            f'<div class="step"><span class="num">{n}</span><span>{e(t)}</span></div>'
            for n, t in enumerate(sl["steps"], 1)) + "</div>"
    lead = f'<div class="lead">{e(sl["lead"])}</div>' if sl.get("lead") else ""
    count = f'<span class="count">{i + 1}/{total}</span>' if total > 1 else ""
    if sl["kind"] == "cta":
        art = f'<div class="biglogo">{LOGO.format(c="#ffffff")}</div>'
        extra = '<div class="towns">Angono · Antipolo · Taytay</div>'
    elif total == 1:  # a single image ends on the download line instead of "Swipe"
        extra = (f'<div class="single"><span class="dl">{e(sl["cta"])}</span>'
                 '<span class="towns">Angono · Antipolo · Taytay</span></div>')
    else:
        extra = '<div class="swipe">Swipe &rarr;</div>'
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>{css}</style></head><body>
<div class="head">{LOGO.format(c=brand)}Tarbaho{count}</div>
{art}
<div class="main">{lead}<div class="title">{e(sl["title"])}</div>{body}{steps}{extra}</div>
</body></html>"""


COVER_ICONS = ["aircon", "plumbing", "electrical", "construction", "house-clean", "driver", "mechanic", "pest-control"]


def cover():
    """The page's cover photo, 1640x624. Phones crop the sides, so everything sits in the middle 1200px."""
    icons = "".join(f'<span class="i">{icon_svg(n)}</span>' for n in COVER_ICONS)
    return f"""<!doctype html><html><head><meta charset="utf-8"><style>
@font-face {{ font-family: 'DM Sans'; src: url('{FONT}') format('woff2'); font-weight: 100 1000; }}
* {{ box-sizing: border-box; margin: 0; padding: 0; }}
html, body {{ width: 1640px; height: 624px; overflow: hidden; }}
body {{ font-family: 'DM Sans', sans-serif; background: #5B2A86; color: #ffffff; display: flex;
       align-items: center; justify-content: center; }}
.box {{ width: 1080px; display: flex; flex-direction: column; align-items: center; gap: 26px; text-align: center; }}
.brand {{ display: flex; align-items: center; gap: 18px; font-size: 64px; font-weight: 800; letter-spacing: -1px; }}
.brand svg {{ width: 84px; height: 84px; }}
.lines {{ font-size: 35px; font-weight: 700; line-height: 1.3; }}
.lines .g {{ color: #CFF5E1; }}
.icons {{ display: flex; gap: 22px; margin-top: 8px; }}
.i {{ width: 92px; height: 92px; border-radius: 50%; background: #F3ECF9; display: flex; align-items: center;
      justify-content: center; }}
.i svg {{ width: 62%; height: 62%; }}
.towns {{ font-size: 26px; opacity: 0.85; }}
</style></head><body><div class="box">
<div class="brand">{LOGO.format(c="#ffffff")}Tarbaho</div>
<div class="lines">May sira sa bahay? Mga skilled worker na malapit sa'yo.<br>
<span class="g">Skilled worker ka? Walang komisyon, sa'yo ang buong bayad.</span></div>
<div class="icons">{icons}</div>
<div class="towns">Angono · Antipolo · Taytay</div>
</div></body></html>"""


def render(html_path, png_path, w, h):
    browser = next((b for b in BROWSERS if os.path.exists(b)), None)
    if not browser:
        sys.exit("No Chrome or Edge found")
    profile = WORK / f"profile-{png_path.stem}"
    if png_path.exists():
        png_path.unlink()
    subprocess.run([browser, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                    f"--user-data-dir={profile}", f"--window-size={w},{h}", "--virtual-time-budget=3000",
                    f"--screenshot={png_path}", html_path.as_uri()],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
    if not png_path.exists():
        sys.exit(f"No image written for {png_path.name}")


CHANGELOG = [
    "09:40 first set: 5 household and 4 skilled-worker posts, feed + story images",
    "09:45 long headlines shrink by length, so they stay clear of the picture and the purple panel",
    "09:50 a 4-slide carousel for each side: problem, what it costs, how Tarbaho works, download",
    "09:55 four more household posts: car mechanic, roof leaks, solar, errands (bills, PasaBuy, parcels)",
    "09:58 stories: the picture sits above the text in the layout, so a 3-line headline can no longer run into it",
    "10:05 four more posts: IT and CR tiles for households, welders and errand runners for skilled workers; feed headlines narrower so they never touch the picture",
    "10:15 a \"how it works\" image for each side: the problem, 4 steps, the download line",
    "10:25 copy check (no banned words or price promises); a 2-week posting plan, workers first",
    "10:35 the page's cover photo (1640x624), both sides' lines and the trades",
    "10:45 clean rebuild of all 45 images; out/overview.png shows them all on one sheet (overview.py)",
    "11:15 everything for households in English (posts, carousel, how it works, cover line); skilled workers stay in Tagalog",
    "12:00 rewrite in local lingo (households Taglish, workers Tagalog trade words: Maestro, arawan, walang lamig); "
    "\"errands\" is now \"pakisuyo\"; every post tagged by awareness stage and season; 12 new posts (inflation, gas, "
    "baha, Pasko, lipat-bahay, GC, why Tarbaho, komisyon, aircon season, download now); plan built from seasons",
    "13:00 5 new posts (pasukan, bagyo check, number stays private, tag-ulan walang arawan, chat before the biyahe); "
    "tighter lines on lipat-bahay, why Tarbaho and welder",
    "13:20 4 new posts (hidden leak and the water bill, pay after the work, suki from sabi-sabi, make your profile now); shorter answers on GC, pakisuyo and Maestro",
    "13:40 4 new posts (CCTV at the gate, carwash at home, TESDA badge, solar season); tighter overprice, ten-years and aircon-season lines; the CTA button that wrapped on stories (overprice, roof, tiles) is now one line",
    "14:00 4 new posts (posting for Mama, stain on the kisame, weekend sideline, tile setter); tighter aircon cleaning, gas and extra lines",
    "14:20 4 new posts (Christmas lights, damo sa bakuran, kabit-ilaw season, \"helper lang\"); tighter Pasko, breaker and carwash lines",
    "14:40 4 new posts (printer before a deadline, \"PM for price\", mekaniko, you set your price); tighter GC, pasukan and water-bill lines",
    "15:00 4 new posts (amoy kanal sa lababo, Pasko general cleaning, Ate sa linis, after the baha); tighter Mama, bakuran and tile-setter lines",
    "15:20 4 new posts (kalampag ng gate, mainit na saksakan, foreman renovation, why Tarbaho for workers); aircon cleaning no longer says \"book\"; tighter Pasko cleaning and mekaniko lines",
    "15:40 4 new posts (kotse sa baha, one post many offers, uwi galing abroad, tag-ulan profile now); tighter aircon, waiting and baha answers",
    "16:00 4 new posts (bitak sa pader, PasaBuy, tubero sa tag-ulan, sabay-sabay na bayarin); tighter tag-ulan, tiles and roof answers",
    "16:20 4 new posts (WFH WiFi, Pasko ipaayos na, karpintero, reviews on your profile); tighter inflation, Ate sa linis and foreman lines",
    "16:40 4 new posts (maingay na aircon, CR ni Lola, summer profile now, customers who passed you by); tighter plumbing, pasukan and saksakan answers",
    "17:00 4 new posts (₱1,500 budget, yero sa bagyo, garden sa tag-ulan, customers message you first); tighter kisame, Pasko and solar answers",
    "17:20 4 new posts (mahinang shower, reviews first, Disyembre tigil ang site, IT for WFH homes); tighter cleaning, lababo and bilihin lines",
    "17:40 4 new posts (aircon pull-out when moving, due date, pakisuyo tuwing umaga, one profile three towns); tighter IT, gate and suki lines",
    "18:00 4 new posts (basang saksakan after baha, Sabado lang free, car wash kit, work photos on the profile); shorter number, bills and solar-tech lines",
    "18:20 4 new posts (tulo ng gripo, ikaw ang pipili, tapos na ang project, ID badge); shorter printer line; bayarin no longer repeats the bilihin answer",
    "18:40 4 new posts (kwarto ni bunso, amoy kulob na aircon, mason, libreng profile); tighter overprice and extra lines",
    "19:00 4 new posts (ceiling fan, walang patong, isang oras na biyahe, tuition ni anak); tighter bagyo answer; after-baha answer no longer copies the others",
    "19:20 4 new posts (poso negro, malapit lang sila, sa'yo ba ang suki mo, linis season); no two posts share an answer line any more (7 rewritten)",    "23 Nov: IT retired, Driver (On-Call) added: the four IT posts are now Driver posts (airport, Tagaytay, check-up, drivers); CCTV is the electrician's; the cover shows driver and pest control",
]


STAGES = [
    ("unaware", "1. Unaware", "They don't see a problem yet. Show them one."),
    ("problem", "2. Problem aware", "They feel it but don't know a better way. Make it sting, hint at the fix."),
    ("solution", "3. Solution aware",
     "They use the usual fixes (kakilala, GC, FB group, poster sa poste). Show why Tarbaho beats those."),
    ("product", "4. Product aware", "They know Tarbaho but aren't sold. Give the difference."),
    ("most", "5. Most aware", "Ready. Just the offer and \"download now\"."),
]

SEASONS = [  # posting window, which season tags run in it
    ("Now to December", ["Nov-Dec", "all"]),
    ("January to February", ["all"]),
    ("March to May (init, aircon)", ["Mar-May", "all"]),
    ("May to June (enrollment, lipat-bahay)", ["May-Jun"]),
    ("June to October (ulan, baha, bagyo)", ["Jun-Oct", "all"]),
]


def slide_name(carousel, i):
    return carousel["id"] if carousel.get("single") else f"carousel-{carousel['id']}-{i + 1}"


def markdown(posts):
    lines = ["# Tarbaho Facebook posts", "",
             "Problem first, then Tarbaho as the answer. Each post: the feed image (1080x1080), the story image "
             "(1080x1920) and the caption to paste. Replace `[Google Play link]` with the store link before posting.", ""]
    for side, title in (("household", "For households (Taglish)"), ("worker", "For skilled workers (Tagalog)")):
        lines += [f"## {title}", ""]
        for key, name, why in STAGES:
            group = [p for p in posts if p["side"] == side and p["stage"] == key]
            if not group:
                continue
            lines += [f"### {name}", "", f"_{why}_", ""]
            for p in group:
                lines += [f"#### {p['problem']}", "",
                          f"When: {p['season'] if p['season'] != 'all' else 'any time'} · "
                          f"Images: `out/{p['id']}-feed.png`, `out/{p['id']}-story.png`", "",
                          "```", p["caption"].format(link=LINK), "```", ""]
    lines += ["## Page cover photo", "",
              "`out/page-cover.png` (1640x624): the Facebook page's cover. Phones show only the middle, so the "
              "text stays there. The page's profile picture: `branding/tarbaho-facebook-purple.png`.", ""]
    lines += ["## Posting plan", "",
              "One post a day, around 7 to 9 PM. Pin both \"how it works\" images first. Lead with skilled workers "
              "for the first two weeks, so households who download find people. Within each side, walk down the "
              "stages (unaware first, \"download now\" last), and repeat the cycle. Share each post to the Angono, "
              "Antipolo and Taytay community and buy-and-sell groups (follow each group's rules).", ""]
    for window, tags in SEASONS:
        lines += [f"**{window}**", ""]
        for side, title in (("worker", "Skilled workers"), ("household", "Households")):
            order = [p for key, _, _ in STAGES for p in posts
                     if p["side"] == side and p["stage"] == key and p["season"] in tags]
            if order:
                lines += [f"- {title}: " + " → ".join(p["problem"] for p in order)]
        lines += [""]
    lines += ["## How it works", "", "One image each, good as a pinned post.", ""]
    for c in (c for c in CAROUSELS if c.get("single")):
        lines += [f"### {c['slides'][0]['lead']}", "", f"Image: `out/{slide_name(c, 0)}.png`", "",
                  "```", c["caption"].format(link=LINK), "```", ""]
    lines += ["## Carousels", "", "Upload the slides in order as one post (Photo/video, pick all of them).", ""]
    for c in (c for c in CAROUSELS if not c.get("single")):
        files = ", ".join(f"`out/{slide_name(c, i)}.png`" for i in range(len(c["slides"])))
        lines += [f"### {c['slides'][0]['title']}", "", f"Slides: {files}", "",
                  "```", c["caption"].format(link=LINK), "```", ""]
    lines += ["## Changelog", ""] + [f"- {c}" for c in CHANGELOG] + [""]
    (HERE / "POSTS.md").write_text("\n".join(lines), encoding="utf-8")


def main():
    only = set(sys.argv[1:])
    OUT.mkdir(exist_ok=True)
    WORK.mkdir(exist_ok=True)
    for post in POSTS:
        if only and post["id"] not in only:
            continue
        for size, s in SIZES.items():
            h = WORK / f"{post['id']}-{size}.html"
            h.write_text(page(post, size), encoding="utf-8")
            render(h, OUT / f"{post['id']}-{size}.png", s["w"], s["h"])
            print("built", f"out/{post['id']}-{size}.png")
    for c in CAROUSELS:
        if only and c["id"] not in only and f"carousel-{c['id']}" not in only:
            continue
        for i in range(len(c["slides"])):
            name = slide_name(c, i)
            h = WORK / f"{name}.html"
            h.write_text(slide(c, i), encoding="utf-8")
            render(h, OUT / f"{name}.png", 1080, 1080)
            print("built", f"out/{name}.png")
    if not only or "cover" in only:
        h = WORK / "cover.html"
        h.write_text(cover(), encoding="utf-8")
        render(h, OUT / "page-cover.png", 1640, 624)
        print("built out/page-cover.png")
    markdown(POSTS)
    print("wrote POSTS.md")


if __name__ == "__main__":
    main()
