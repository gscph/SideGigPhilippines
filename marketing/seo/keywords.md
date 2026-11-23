# Search keywords for tarbaho.com (Philippines)

What people in Rizal type into Google, grouped by who is searching. Use these words **in the page's own text**
(title, description, headings, FAQ answers, picture alt text). Google ignores a hidden "keywords" tag, so a word only
counts when a visitor can read it on the page.

Some searches here are for things no job in the app lists yet (CCTV, laptop repair, wifi setup, car aircon, septic
tank / poso negro). They are here as ideas only: put a word on the page only when `004-config.sql` has a job for it.

Copy rules still hold: service words, never "job", "hire", "hiring", "employment", "salary" or "minimum wage"; never
"0% commission" or "walang komisyon"; never vouch for a worker.

## 1. Households: the service + the town (the main target)

People search for a service and add where they are, or "near me". Each line pairs with **Angono**, **Antipolo**,
**Taytay** or **Rizal** ("aircon cleaning Angono", "tubero Antipolo", "pest control Taytay Rizal").

| Trade | English searches | Tagalog / Taglish searches |
|---|---|---|
| Aircon | aircon cleaning, aircon repair, aircon cleaning near me, split type aircon cleaning, window type aircon cleaning, aircon freon recharge, aircon installation, aircon PMS, aircon leak, aircon technician near me | linis aircon, pa-linis ng aircon, sirang aircon, tumutulo ang aircon, pa-freon, pakabit aircon, aircon cleaning price |
| Plumbing | plumber near me, plumber Antipolo, leaking faucet repair, clogged drain, septic tank, water pump repair | tubero, tubero near me, barado lababo, barado CR, tumutulo gripo, poso negro |
| Electrical | electrician near me, electrical repair, breaker tripping, outlet repair, wiring | electrician sa Angono, pa-wiring, sira saksakan, nag-tritrip breaker |
| Carpentry | carpenter near me, cabinet maker, door repair, ceiling repair | karpintero, karpintero near me, pagawa kabinet, sira kisame |
| Masonry / tiles | mason, tile installer, house repair, renovation | mason, pa-tiles, pagawa bahay, renovation Rizal, foreman |
| Roofing | roof repair, roof leak, gutter repair | tumutulo bubong, ayos bubong, alulod, yero |
| Welding | welder near me, gate repair, steel grill | welder, pa-weld gate, rehas |
| Pest control | pest control Rizal, termite treatment, pest control price, pest control near me | anay, pa-spray anay, ipis, daga, lamok misting, surot, langgam |
| Errands | house cleaning service, PasaBuy, grocery delivery, bills payment, parcel pickup, car wash at home | linis bahay, pa-grocery, pasabuy, pahatid papeles, pa-pickup parcel, linis kotse, hardinero |
| Car mechanic | mechanic near me, home service mechanic, car aircon, suspension, engine repair | mekaniko, home service mekaniko, sira makina |
| IT | computer repair, laptop repair, wifi setup, CCTV installation, IT support | computer repair Antipolo, pagawa laptop, pakabit CCTV |

General: home service Rizal, home repair Antipolo, handyman near me, skilled workers near me, services near me
Angono, "kuya" + service ("aircon kuya").

## 2. Skilled workers looking for customers

| English | Tagalog / Taglish |
|---|---|
| more customers aircon technician, get customers plumber, sideline Rizal, extra income Antipolo, list my services, home service business | sideline sa Angono, raket, dagdag kita, dagdag kliyente, extra para sa pamilya |

Do not write "job", "hiring" or "trabaho for you" on the page: Tarbaho is a directory, not an employer.

## 3. Brand

tarbaho, tarbaho app, tarbaho.com, tarbaho rizal, tarbaho angono, sidegig ph.

## Where the words go

| Place | What it says |
|---|---|
| `<title>` (index.html, `Copy.Meta.Title`) | Tarbaho + the three biggest services + the launch town |
| `<meta name="description">` (index.html) | the services in English and Tagalog + the three towns |
| The page before Blazor loads (index.html `fd-boot`) | one heading and a line naming services and towns |
| FAQ "What can I find on Tarbaho?" | every trade and its common jobs, English with Tagalog words |
| JSON-LD (index.html) | Organization + WebSite: area served = Angono, Antipolo, Taytay |

## Next ideas (not done)

- One page per town and per trade ("Aircon cleaning in Angono") ranks far better than one page for everything. The
  site is one page today; adding pages means real routes and a fuller sitemap.
- A Google Business Profile as a service-area business (Angono, Antipolo, Taytay) puts Tarbaho on Maps searches.
- Links from the Facebook Page, Rizal community groups and barangay pages to www.tarbaho.com.
