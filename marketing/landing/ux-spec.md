# Tarbaho landing page: UX spec

> **Superseded by the built page (23 Nov 2026).** The live wording is `SideGigPh.FrontDoor/Localization/Copy.cs` and the layout `SideGigPh.FrontDoor/Components/Landing/`; change those, not this brief. Since this brief: Pest Control replaced Solar, a worker's price is one number, no line may say "0% commission" / "walang komisyon" (it could later look like bait-and-switch), the form asks Android or iPhone first (iPhone gets the public TestFlight link at once), legal links are on `api.tarbaho.com`, and sign-ups are kept in Azure SQL (`dbo.EarlyAccessSignups`).

Owner: UX. Copy: Marketing (fills the slots below; word limits are hard caps per language, Taglish may run +20%).
Stack: Blazor WebAssembly, MudBlazor 9, mobile-first. Most visitors: Facebook in-app browser on Android, 360px wide, slow data.
Goal: Early Access email sign-ups. Everything above the form earns the scroll to it; everything below answers doubts and sends people back up.

## 0. Look and rules (from the app, unchanged)

- 60/30/10: white surfaces, ink `#222222` text and icons, purple `#5B2A86` only for the main button, selected chips, links and small accents. Light tint `#E9DFF3` for soft section backgrounds and icon circles (same as the cover photo blobs).
- Fixed exceptions: gold `#FFB400` stars only, green `#118C4F` money only ("pay the worker directly"). No blue anywhere on this page (blue = verified, and nobody is verified yet).
- DM Sans (self-hosted woff2 from `SideGigPh.Maui/wwwroot/fonts/`, `font-display: swap`). Big bold headings, short lines.
- Flat: no shadows except the email card (`Elevation="2"` equivalent, the one raised thing on the page). 8px buttons, 12px fields and tiles, hairline `#ebebeb` dividers.
- Pictures are the app's own SVGs from `SideGigPh.Maui/wwwroot/images/icons/` (copy them into the site's `wwwroot/images/icons/`), shown with `MudImage` in a 56px tint circle. No emoji in the page (FB post copy may keep them).
- Theme through a `MudTheme` built with the same values as `AppTheme` (Primary purple, Dark ink, Success green, Warning gold, Secondary muted gray). No hex in components.
- Layout: `MudContainer MaxWidth="MaxWidth.Large"`; 16px side gutter at 360px, 24px at ≥960px. Section padding `py-10` mobile, `py-16` desktop. Never a horizontal scroll.
- Word style: workers are Kuya / Ate / Manong / Manang. Service words, never job/hire/employment/salary/"minimum wage". Never vouch for a worker.

## Section order (top to bottom)

| # | Section | Background | Job |
|---|---|---|---|
| H | Sticky header | white + hairline | brand, language, jump to form |
| 1 | Hero | soft tint blob on white | say what it is in 5 seconds, one button to the form |
| 2 | Two sides (household / worker) | white | "this is for me" |
| 3 | Our promises | tint `#F6F1FA` band | the reasons: pay the worker directly, own price, private number, pay after |
| 4 | **Early Access** (centrepiece) | purple band, white card | the sign-up |
| 5 | Trades and towns | white | what and where, honest launch area |
| 6 | Safety and trust | tint band | what we check, how reviews work |
| 7 | FAQ | white | short answers to doubts |
| 8 | Repeat CTA (slim) | white, hairline top | one line + button scrolling to #4 |
| F | Footer | ink-on-white, small | legal, contact, stores soon |

The form sits 4th of 8 blocks: roughly the 45–55% scroll point on a phone. Sections 1 and 8 each have a button that scrolls to it, and so does the header.

## H. Sticky header

- Height 56px mobile, 64px desktop. White, 1px hairline bottom. `position: sticky; top: 0` (MudAppBar `Fixed="false"` + a gap class `app-sticky`, or `MudAppBar Elevation="0"` fixed with a top spacer).
- Left: logo mark (the crew SVG path in a 32px purple rounded square, as in the FB posts) + "Tarbaho" wordmark in purple, `Typo.h6` bold. Tapping it scrolls to top.
- Right, mobile: language toggle `EN | Taglish` (a 2-option `MudToggleGroup`, compact, outlined, 32px high), then a small filled purple button "Early access" (2 words max) that scrolls to #4. Hide this button while section 4 is on screen (IntersectionObserver via a tiny JS interop), so it never points at itself.
- Right, ≥960px: same, plus text links "How it works", "Trades", "FAQ" (anchor scroll) before the toggle.
- Language: default from `navigator.language` (`fil`/`tl` → Taglish, else EN), remembered in `localStorage` (wrapped in try/catch). Switching never reloads or loses typed form input; it swaps `LocalizedText` pairs in place and sets `<html lang>` (`en` / `fil`).
- Scroll offset: anchor targets get `scroll-margin-top: 72px` so the sticky bar never covers a heading.

## 1. Hero

Purpose: in 5 seconds, "a free app to find a nearby Kuya/Ate for repairs, or to get your own clients. Coming soon."
- 360px: stack, left-aligned. Eyebrow chip "Coming soon · Angono" (`MudChip` outlined, small). H1 (`Typo.h3`, ~32px, bold, max 3 lines). Sub (`Typo.body1`, muted). Primary `MudButton Variant.Filled Color.Primary FullWidth` "Get early access" → #4. Below it, the two store badges side by side (see Store badges). Then a horizontal icon rail of 6 trades (scrollable, `MudStack Row` + `overflow-x:auto`, 64px tiles: icon + 1-word label).
- ≥960px: two columns 6/6 (`MudGrid`). Left: same text stack, button not full width. Right: a phone frame showing a real app screen (static PNG exported from the app or the cover-photo phone mock, ≤ 80 KB WebP, `loading="eager"`, width/height set). No people photos.
- Icons (rail): `aircon.svg`, `construction.svg`, `plumbing.svg`, `electrical.svg`, `errand.svg`, `mechanic.svg` (+ `more.svg` as the last tile "More" → #5).
- Slots: eyebrow ≤4 words; H1 ≤10 words; sub ≤22 words; button ≤3 words; rail labels 1 word each.

### Store badges (used in hero, #4 success state and footer)
- Two `MudButton Variant.Outlined Disabled="true"` look-alikes, 44px high: Android robot / Apple icon (`Icons.Custom.Brands` or MudBlazor `Icons.Material.Filled.Android` / `Icons.Custom.Brands.Apple`), text "Android" / "iOS" + small second line "Soon" / "Malapit na". 50% opacity, `aria-disabled="true"`, not focusable links. Do not use official Google Play / App Store badge artwork until the listings exist (store brand rules).

## 2. Two sides

Purpose: each visitor finds their side fast. Anchor `#how`.
- A `MudToggleGroup` at the top: "I need work done" / "I'm a Kuya / Ate" (default: household). Content below swaps with no layout jump (both panels keep the same min-height). The pick pre-selects the role in the email form (#4) unless the visitor changed it there.
- Each panel: 3 numbered steps (`MudStack`, number in a 32px purple circle like the cover photo, icon, title, one line).
  - Household: `search.svg` find / `chat.svg` chat in the app / `money.svg` pay after the job.
  - Worker: `profile-card.svg` make your profile / `megaphone.svg` get requests near you / `wallet.svg` keep the full pay.
- 360px: steps stacked. ≥960px: 3 steps in one row (`MudGrid` 4/4/4).
- Slots: toggle labels ≤4 words; step title ≤5 words; step line ≤14 words.

## 3. Our promises

Purpose: the four reasons, said once, plainly. Tint band.
- 4 tiles, `MudPaper Outlined` 12px radius, icon circle + title + 1 line. 360px: 1 column (2×2 is too tight for Taglish). ≥960px: 4 in a row.
- Tiles and icons: pay the worker directly `money.svg` (title in green); own price `receipt.svg`; number stays private until picked `cyber-lock.svg`; nearby, pay after the work `pin.svg`.
- Slots: section heading ≤6 words; tile title ≤5 words; tile line ≤16 words.
- No numbers except "₱".

## 4. Early Access (the centrepiece)

Anchor `#early-access`. Full-width purple band (`Color.Primary` background, white text), the white form card raised on top.
- 360px: heading (white, `Typo.h4`, ≤8 words) and sub (≤20 words, says what they get: first to set up / first requests, a note when the app opens). Then the card full width, 20px padding. Below the card, the store badges in white outline.
- ≥960px: two columns 5/7: left heading, sub and 3 short bullets with check icons (`done.svg` on white circles; ≤8 words each: e.g. first access, free, unsubscribe anytime); right the card, max 480px wide.
- One `<h2>` here; the card is a real `<form>` (`EditForm`) so the phone keyboard's Go/Enter submits.

### Form fields (in order)
1. **I am a** (optional): `MudToggleGroup` single-select, 2 tiles with icons: `home.svg` "Household" / "Nagpapagawa", `worker.svg` "Kuya / Ate (worker)". Starts empty unless set by #2. Not required; no error if skipped.
2. **Email**: `MudTextField Variant.Filled` class `app-field`, label inside "Email" (Gmail hint as helper text: "Use the Gmail on your Play Store so we can send the invite", ≤14 words). `InputType.Email`, `inputmode="email"`, `autocomplete="email"`, `autocapitalize="off"`, `spellcheck="false"`, `enterkeyhint="send"`. Trim spaces, lowercase the domain.
3. **Button**: `MudButton Variant.Filled Color.Primary FullWidth`, 48px, "Join early access" (≤3 words). Left icon none (keep it clean).
4. **Privacy line** under the button, `Typo.caption`, muted, ≤25 words: we only use the email to tell you when Tarbaho opens, no spam, unsubscribe anytime, with "Privacy Policy" linked (opens in new tab).
- Hidden honeypot field (`tabindex=-1`, off-screen, `aria-hidden`) for bots; a submit with it filled shows success but sends nothing.
- No phone number, no name, no town on this form (less to type, less personal data; RA 10173). The app asks those later.

### Validation (on submit, then live once the field was touched)
- Empty: "Type your email." / "Ilagay ang email mo."
- Bad shape (no `@`, no dot after it, spaces inside): "That email doesn't look right. Check it?" / "Mukhang mali ang email. Pakicheck?"
- Common typo (`gmial.com`, `gmail.con`, `yaho.com`): a tappable suggestion under the field, not an error: "Did you mean …@gmail.com?" / "…@gmail.com ba?". Tap fills it.
- Too long (>254 chars): treat as bad shape.
- Messages appear under the field (`Error` + `ErrorText`), never in a popup, and never clear what was typed.

### States
| State | What shows | Notes |
|---|---|---|
| Idle | form as above, button enabled | |
| Sending | button disabled, `MudProgressCircular` 18px inside it, label "Sending…" | guard `if (_busy) return;` against double taps; inputs read-only, not cleared |
| Success | card content replaced (same height, no jump) by `done.svg` in a green circle, heading ≤6 words, line ≤20 words (we'll email when it opens; iPhone: TestFlight), store badges, a "Share with a friend" `MudButton Outlined` (Web Share API, fallback copy link) | focus moves to the success heading (`tabindex=-1`), announced via `aria-live="polite"` |
| Already on list | same as success, line says "You're already on the list. We'll email you." | server answers the same 200 for new and known emails (no account guessing); the app shows this variant only if the server says so for that browser's own previous sign-up (stored flag) |
| Error (server/5xx/timeout 15 s) | `MudAlert Severity.Normal` above the button: "Didn't go through. Try again?" + button stays enabled | email kept; one automatic retry is not done (send once) |
| Too many tries (429) | same alert: "Too many tries. Wait a minute and try again." | |
| Offline (`navigator.onLine` false or fetch never answered) | alert: "No internet. We'll keep your email here, tap again when you're back." | listen for `online` and re-enable; never "something broke" |

- Success is remembered in `localStorage` (try/catch): on a return visit the card opens in the success state with a small "Use another email" text button.
- Analytics (if any): only "form_view", "role_picked", "submit_ok", "submit_error". Never the email.

### Focus, keyboard, accessibility
- Visible labels (no placeholder-only). Field `aria-describedby` = helper + error ids. Errors use `role="alert"`; the first invalid field gets focus on submit.
- When the header/hero/repeat button scrolls here, focus goes to the email field only on desktop; on mobile scroll only (opening the keyboard by surprise hides the card). Use `scrollIntoView({block:"center"})` so the button stays above the keyboard.
- Tap targets ≥44px; toggle tiles ≥48px. Contrast: white on purple and ink on white both pass AA; muted caption ≥4.5:1 (`#6a6a6a` on white).
- Works with JS-loaded Blazor only after boot: pre-render (or a static `index.html` fallback) shows the heading and a "Loading…" card skeleton, never a dead form.

## 5. Trades and towns

Anchor `#trades`. Purpose: "do you have my kind of work, and is it near me".
- Trades: grid of tiles, icon + label (same look as the app's ChoiceTiles). 360px: 3 columns; ≥960px: 6 columns. Order as in the app: Aircon, Construction, Errands, Driver (On-Call), Car Mechanic, Pest Control. Under Construction, a wrap of small outlined chips (`MudChipSet` read-only): Foreman, Carpentry, Electrical, Plumbing, Masonry, Tiles, Welding, Roofing, Steelman, Helper.
- Icons: `aircon.svg`, `construction.svg`, `errand.svg`, `it.svg`, `mechanic.svg`, `pest-control.svg`; chips may carry `foreman.svg`, `carpentry.svg`, `electrical.svg`, `plumbing.svg`, `masonry.svg`, `tiles.svg`, `weld.svg`, `roofing.svg`, `rebar.svg`, `worker.svg` at 20px.
- Towns: 3 rows with `pin.svg`: Angono "first" (purple outlined chip "Starting here"), Antipolo and Taytay "next". No map image (weight, and it implies coverage we don't have).
- Slots: heading ≤6 words; trade labels as in the app; towns line ≤14 words.
- Trade list is copied by hand for now (the site doesn't call `GET config`); note in the code which file to keep in step with: `SideGigPh.AzureSQL/Sql/004-config.sql`.

## 6. Safety and trust

Purpose: answer "is this safe" without overclaiming. Tint band.
- 3 rows (360px stacked, ≥960px 3 columns), icon + title + 1–2 lines:
  - `profile-card.svg` optional badges: government ID, TESDA, past project, checked by a person.
  - `cyber-lock.svg` private number until a pick; chat inside the app.
  - `flag.svg` report and block on every profile and chat.
- Reviews line (one sentence, no stars graphic with numbers): reviews come after the work, both sides.
- Slots: heading ≤6 words; titles ≤5 words; lines ≤20 words. Never "verified workers", "trusted", "guaranteed".

## 7. FAQ

Anchor `#faq`. `MudExpansionPanels` (one open at a time, all closed at first), 5–6 questions max:
is it free / when does it open, Android and iPhone / do you take a cut / how do I pay the worker / which towns / what happens to my email.
- Slots: question ≤10 words; answer ≤40 words. Answers link back to #4 where natural ("Join the list").

## 8. Repeat CTA (slim)

- One row, 72px tall on desktop; 360px: stacked, 16px padding. Text ≤10 words ("Be first when Tarbaho opens in Angono" type) + `MudButton Filled Primary` "Get early access" → `#early-access`.
- If the visitor already signed up (stored flag), the line changes to a thank-you and the button becomes "Share" (Web Share API).

## F. Footer

- White, hairline top, `Typo.body2` muted, links in ink, underline on hover/focus.
- Row 1: logo mark + "Tarbaho", one line ≤14 words (a free local directory in Rizal).
- Row 2 links: [Privacy Policy](https://api.tarbaho.com/api/legal/privacy) · [Terms](https://api.tarbaho.com/api/legal/terms) · Facebook Page · Contact (email `mailto:`, address from the owner). Open legal pages in a new tab.
- Row 3: store badges (disabled, "soon"), language toggle repeated, "© 2026 Tarbaho".
- 360px: rows stacked, centered; ≥960px: logo left, links centre, badges right.

## Performance

- Budget on Android/4G: first meaningful paint < 2 s, total first load < 1.5 MB. Blazor WASM is heavy, so: pre-render the static HTML (or keep the hero + #4 heading in `index.html` like the app's pre-Blazor splash), enable trimming + Brotli, lazy-load nothing above the fold except the font.
- Images: SVG icons inline-able (each < 2 KB); at most one raster (the hero phone screen), WebP, explicit size, `fetchpriority="high"`; everything below the fold `loading="lazy"`.
- No carousels, autoplaying video, chat widgets or third-party trackers by default. If the Meta Pixel is added for the FB ads, load it after consent/idle and say so in the privacy policy.
- Test in the Facebook in-app browser (Android) at 360×640 and with "Data saver"; the form must work there (no `target=_blank` traps on submit, no popups).

## Trust rules (block a design that breaks them)

- No stock photos of people, no AI-made "workers" or "happy customers". Icons, app screens, and later real photos with consent only.
- No fake counters ("1,200 workers joined"), no countdowns, no "only 50 spots", no fake testimonials or star ratings. Before real data exists, show no numbers at all except ₱.
- Don't call anyone "verified"; badges are optional and checked by a person, said plainly.
- No "download now" and no live store badges until the listings exist.
- The email is used only for the launch invite (and what the privacy line says). Collecting it needs the privacy policy to name this list; `docs/legal/privacy-policy.md` §3.6 covers web sign-ups.
