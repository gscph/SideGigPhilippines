# Search ranking: test plan

How to check search ranking by hand on a phone, using the sample workers from
`SideGigPh.AzureSQL/Sql/006-seed-search-ranking.sql`. How the ranking works: [search-ranking.md](search-ranking.md).

Every order below is also checked automatically by `SearchRankingSeedTests`, which runs this seed on a test
database. If a test here disagrees with the app, the cause is the setup (cache, town, budget, script not run), a
deploy that isn't current, or a real bug. It isn't a typo in this page.

## Setup (once)

1. In the Azure portal → SQL database → **Query editor**, run these if they haven't been run yet:
   the schema scripts through `008-schema-service-prices.sql` and `004-config.sql` (the jobs' price guides), then
   `006-seed-search-ranking.sql` (the whole file each time). The seed stops with a message if the price guides are missing.
   The seed prints `Search ranking seed added: 45 workers, … reviews, … chats.`
2. Deploy the API with the new ranking (Actions → deploy-api).
3. Wait **one minute** (or restart the Function App). The API keeps a trade's workers 30 s and the app keeps a
   results page 60 s.
4. On the phone, sign in as a **Seeker**. Open Search: Home → search box.

**How to search for a job.** Type the words in the table into the search box; each one picks the trade *and* the
job. Tapping a trade icon instead searches the **whole trade** (no job filter). For scenarios S4 and S6–S8 that gives the
same order, because every sample worker in those trades lists the same single job. S1, S2, S3 and S5 are all
Construction jobs, so type the words (or pick the job) to see one scenario on its own.
**Town and budget** are the two chips under the search box. Unless a test says otherwise, use
**Town: Taytay** and the **first budget choice** ("Tipid": the job's lowest to average price, e.g. ₱755–₱1,000 for
aircon cleaning, ₱1,000–₱2,000 for a roof leak).

| Scenario | Type this | Trade → job |
|---|---|---|
| S1 | `tagas tubo` or `barado lababo` / `pa-weld gate` or `hinang rehas` / `steelman rebar` | Construction → Plumbing / Welding / Steelman |
| S2 | `walang kuryente` | Construction → Electrical |
| S3 | `repair furniture` | Construction → Carpentry |
| S4 | `linis ng bahay` | Errands → House cleaning |
| S5 | `pakabit tiles` | Construction → Tile setting |
| S6 | `makina kotse` | Car Mechanic → Engine |
| S7 | `inverter` | Solar Panel Technician → Inverter or battery check |
| S8 | `linis solar panel` | Solar → Panel cleaning |
| S9 | `linis aircon` / `pakabit aircon` | Aircon → Cleaning / Installation |

Every sample worker's name starts with its scenario ("S4 …"), so you can always tell which test a card belongs
to. In each scenario the workers are identical except for the one thing being tested (the ordinary worker: Taytay,
rate **Average price** (the job's average from its price guide), 1–3 years, no reviews, no chats, joined 90 days ago,
active yesterday).

## Scenarios with the sample workers

### S1 The job is a hard filter (Construction)
| Search | Expect | Proves |
|---|---|---|
| `tagas tubo` | **S1 Tubero at Welder**, **S1 Tubero Specialist**. *S1 Welder Specialist* is **not shown**, even though it's the best construction worker (10+ years, 20 five-star reviews, cheapest). | No score gets you past the job filter. |
| `pa-weld gate` | **S1 Welder Specialist**, **S1 Tubero at Welder** | A worker listing two jobs shows for both. |
| `steelman rebar` | "No skilled workers for this job yet" | No fallback to other construction workers. |
| Tap the **Construction** icon | All 3 | A whole-trade search has no job filter. |

### S2 Town: your town first, nearby after (Construction → Electrical)
| Search | Expect | Proves |
|---|---|---|
| `walang kuryente`, Town **Taytay** | Goes to Taytay: **S2 Taytay at Antipolo**, **S2 Taytay Baguhan**. Other towns nearby: **S2 Antipolo Star**, **S2 Angono Kuryente**. | *Antipolo Star* (10+ yrs, 30 five-star reviews, fast replies) is far better but never jumps above a Taytay worker. |
| Same, Town **Antipolo** | Goes to Antipolo: **S2 Antipolo Star**, **S2 Taytay at Antipolo**. Nearby: **S2 Angono Kuryente**, **S2 Taytay Baguhan**. | Groups follow the Seeker's town. A worker who goes to several towns is "in town" for each. |
| Either | *S2 Binangonan Lang* **never** shows | A worker only in a town we don't serve is left out. |

### S3 Reviews: stars and how many (Construction → Carpentry)
Expect: **S3 Steady 4.8 (40 reviews)** → **S3 Lucky 5.0 (1 review)** → **S3 Bago (no reviews)** → **S3 Poor 2.0 (10 reviews)**.
Proves: one lucky 5★ can't beat 40 good reviews; a new worker starts from a fair 4★ guess, so bad reviews sink below "no reviews yet". Open each profile: the stars and counts match.

### S4 Responsiveness (Errands)
Expect:
1. **S4 Mabilis Sumagot (10 min)**: answered 10 of 10 chats within 10 minutes
2. **S4 Matagal Sumagot (5 hrs)**: answered 10 of 10, slowly
3. **S4 Bago (no chats yet)**, **S4 Kaka-message (2 hrs ago)**, **S4 Tawag Lang (calls only)**: these score the same, so they're shown A–Z
4. **S4 Hindi Sumasagot (1 of 10)**: last

Proves: answering beats not answering, and fast beats slow. A chat under 24 hours old isn't held against the worker yet. Chats where the Seeker only tapped Call don't count (they may have talked on the phone).

### S5 Activity (Construction → Tile setting)
Expect: **S5 Active Today** → **1 Week Ago** → **1 Month Ago** → **3 Months Ago**. Proves the activity points halve every week away.

### S6 Tenure (Car Mechanic)
Expect: **S6 Joined 1 Year Ago** → **3 Months Ago** → **3 Days Ago**. All three were active today. Tenure is a small part (6 of 100 points), so it only decides ties like this one.

### S7 Budget (Solar inverter check)
Roof leak repair costs ₱1,000 (lowest), ₱2,000 (average) or ₱3,500 (above average). Four workers use the rates the
app offers now; the two "Old App" workers still carry a price tier from app versions before price guides
(₱500–1,000 reads as ₱750, ₱5,000 and up as ₱5,000).

| Budget chip | Expect |
|---|---|
| ₱1,000–₱2,000 (first choice) | **S7 Average Price**, **S7 Lowest Price**, **S7 Old App, Cheap Tier** (all fit: green "Fits your budget" chip, A–Z) → **S7 Depends on the Job** → **S7 Above Average** → **S7 Old App, Top Tier** |
| ₱2,000–₱3,500 (second choice) | **S7 Above Average**, **S7 Average Price**, **S7 Lowest Price**, **S7 Old App, Cheap Tier** (all fit) → **S7 Old App, Top Tier** → **S7 Depends on the Job** |
| Typed ₱1,000–₱1,500 (the "custom" tile) | **S7 Lowest Price**, **S7 Old App, Cheap Tier** → **S7 Average Price** → **S7 Depends on the Job** → **S7 Above Average**, **S7 Old App, Top Tier** |
| Not sure | All six A–Z: price doesn't change the order, and no "Fits your budget" chips |

Proves: in budget first. A price a little over the top of the budget keeps some points and stays ahead of "Depends
on the job"; double the budget or more earns none. Listings from old app versions are priced and ranked too.

### S8 Experience (Solar)
Expect: **S8 10+ Years** (purple award icon) → **3 to 10** → **1 to 3** → **Under 1 Year**.

### S9 Everything together, with paging (Aircon)
`linis aircon`, Taytay, ₱755–₱1,000 (first choice). "Goes to Taytay (7)", then "Other towns nearby (2)":

| # | Worker | Why it's here |
|---|---|---|
| 1 | S9 Mang Jun Aircon | Strong on everything |
| 2 | S9 Mahal Aircon (top price) | Above-average price (₱1,500, half over budget), but 20 great reviews, fast replies, 10+ years |
| 3 | S9 Kuya Ben Cool | Lowest price, good reviews, answers most chats (also goes to Antipolo) |
| 4 | S9 Tulog Aircon (inactive) | 30 good reviews and 10+ years outweigh 45 days away and 10 of 12 chats ignored |
| 5 | S9 Quote Aircon | "Depends on the job" |
| 6 | S9 Bagong Aircon Tech | New: fits budget, neutral starting points |
| 7 | S9 RJ Aircon (2 reviews) | Above-average price, little experience |
| nearby | S9 Antipolo Aircon Pro, S9 Angono Aircon | Other towns |

- **Paging:** the first screen loads 4. Scroll to the end and the next 4 load, then the last one. Nobody shows twice
  and the group titles count all 7 + 2 from the start.
- `pakabit aircon` (installation): **S9 Install Lang**, then **S9 Mahal Aircon**. *Install Lang* never shows for cleaning.
- **Look at rows 2 and 4.** This is where the weights are judged. Reviews and experience currently outweigh price
  and responsiveness. If an expensive or unresponsive worker this high feels wrong, the weights need tuning
  (see the end of this page).

### S10 Nobody in your town
`repair furniture` (Construction → Carpentry) with Town **Angono** (every S3 worker is in Taytay): expect the note "No skilled
workers in Angono yet, so here are skilled workers from nearby towns", and all four under "Other towns nearby" in
the S3 order.

## Scenarios with real accounts (two phones)

Use a second account as a **Skilled Worker** (Phone B) next to your Seeker (Phone A). Publish B under **Errands**
offering **House cleaning** and **Grocery**, in **Taytay**, rate **Average price**, 1–3 years, so it lines up against S4.

| # | Do | Expect | Proves |
|---|---|---|---|
| L1 | Publish B, wait a minute, search `linis ng bahay` on A | B lands in the middle: just below *S4 Bago / Kaka-message / Tawag Lang* (they've been on the app 90 days, B for 0), well above *S4 Hindi Sumasagot* | New workers aren't buried (cold start) |
| L2 | On A, open B and **Send** the first message. On B, **reply within 5 minutes**. Wait a minute, search again | B moves up past *S4 Bago / Kaka-message / Tawag Lang*, still below the two repliers | A reply counts right away |
| L3 | With a third account (or after a Start over), message B and **don't reply** | No change for 24 hours. After that, B drops | An unanswered chat is held against the worker only after a day |
| L4 | On A, tap **Call** on B's profile (no text) and don't reply on B | No change, even after a day | Call-only chats don't count |
| L5 | 24 h after A's first message, rate B 5★ from A | B rises a little (one review moves the stars only slightly; see S3) | Reviews feed ranking |
| L6 | Leave B unopened for a week | B slowly drops below the "active yesterday" workers. Opening B's Inbox brings it back within a minute | Activity counts (stamped when the worker opens their Inbox) |
| L7 | On B, change the rate to **Above average** and republish | B loses the "Fits your budget" chip and sinks | Budget fit |
| L8 | On B, remove House cleaning from its jobs (keep Grocery) and republish | B disappears from `linis ng bahay` and shows for `pa-grocery` | Job filter on real listings |

## Things that look like bugs but aren't

- **A change doesn't show right away.** The API keeps a trade's workers 30 s and the app keeps a page 60 s. Wait a minute.
- **Workers with equal scores are listed A–Z**, so results don't jump around between searches.
- **The seed's times move with the clock.** "Active yesterday" is relative to when you ran the script. Re-run it to reset.
- **You can't chat with sample workers.** They aren't real accounts, so nobody replies.

## Cleanup

Set `@RemoveOnly = 1` at the top of `006-seed-search-ranking.sql` and run it. That removes every `seed-s2-` row,
including chats real Seekers started with sample workers and blocks or reports made about them. Real users and listings aren't touched.

## Tuning notes (what S9 shows)

With today's weights, a worker with many good reviews and 10+ years stays near the top even when they charge
above average (*Mahal*: 10 of 20 budget points, 2nd) or have stopped answering (*Tulog*: 4.5 of 20 responsiveness points,
0.2 of 14 activity points). Things to decide after testing:

- Raise budget fit or responsiveness, or lower review weight.
- Make inactivity a penalty past a limit (e.g. not seen in 30 days = pushed below everyone active), the way
  marketplaces hide listings from hosts who never respond.
