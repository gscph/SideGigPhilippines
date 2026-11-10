# SideGigPh — Tradesmen Open Directory (Philippines)

## What This Is

A high-density open local service directory (Carousell/Craigslist style) connecting urban households with skilled tradesmen and home-service pros. The categories are Aircon, Plumbing, Electrical, Roofing & Gutter, Carpentry, Welding, Masonry & Tiling, Errands/PasaBuy, Solar Panel Cleaning and Home Service Massage. They live in `ReferenceData.Categories` / `ServicesByCategory`. **Category and service IDs are stable; only labels change.** Each category's services are ordered most-requested first and end with an "Other / Iba pa" option. Neutral "Yellow Pages" model — direct consumer-to-tradesman contact, zero transaction commissions.

**Target region (supported towns, Rizal):** Angono, Antipolo, Binangonan, Taytay. The list lives in `ReferenceData.Cities`.

**Audience:** many Professionals (tradesmen) are not confident English readers. The app is **English / Taglish**, and the user picks one on the very first screen.

## Tech Stack

- **Frontend:** .NET MAUI Blazor Hybrid — shared Razor Class Library for Android and iOS
- **Target platforms:** Android + iOS only (Windows and Mac Catalyst are not supported)
- **UI:** Mobile-first. Component library: **MudBlazor** (9.x). Providers (`MudThemeProvider`, `MudPopoverProvider`) sit in `Components/Layout/MainLayout.razor`; its CSS/JS load from `wwwroot/index.html`
- **Auth (MVP):** Firebase Authentication — Phone/SMS OTP + Google login
- **Database (MVP):** Cloud Firestore — provider listings, categories, geo coordinates
- **Storage (MVP):** Firebase Storage — ID verification cards, work portfolio photos
- **Future migration:** Azure Functions + PostgreSQL/PostGIS for spatial queries (`ST_DWithin`)

## Architecture — Ports & Adapters

All external infrastructure is decoupled behind C# interfaces (Dependency Inversion). This enables zero-downtime migration from Firebase to Azure/SQL.

### Key Ports (Interfaces)

**`IIdentityProvider`** — Auth abstraction
- `SignUpWithPhoneAsync(string phoneNumber)`
- `VerifyPhoneOtpAsync(string verificationId, string userEnteredCode)`
- `SignInWithGoogleAsync()`
- `GetCurrentUserAsync()`

**`ITradesmanRepository`** — Data + spatial search abstraction
- `AddProviderAsync(TradesmanRegistrationReq req)`
- `SearchProvidersAsync(GeoSearchFilter filter)`
- `GetProviderByIdAsync(string providerId)`
- `UpdatePayoutStatusAsync(string providerId, string status)`

> **Naming:** new and user-facing code uses **"Professional"** for the side-gig service provider (aircon tech, plumber — and also non-trade pros like web developers or video editors), never "tradesman". `ITradesmanRepository` / `TradesmanRegistrationReq` predate this rule; prefer the neutral form. `IProfessionalProfileRepository` holds profile lookups and `SearchAsync` (backed by `Dev/StubProfessionalProfileRepository` sample data until Firestore).

**`IOnboardingStore`**: the onboarding answers, saved on the device with no account (`Local/FileOnboardingStore`). It writes one JSON file in `FileSystem.AppDataDirectory`, using a source-generated serializer and an atomic write. There is no SQLite yet; add it when local data needs querying. Don't use the WebView's `localStorage`.

**`IContactLauncher`**: Call / Text a pro through the phone's apps (`Local/DeviceContactLauncher`, MAUI `PhoneDialer` / `Sms`). Android needs the `tel` / `smsto` `<queries>` in `AndroidManifest.xml`.

**`IInquiryRepository`** (`Dev/StubInquiryRepository`): an `Inquiry` is recorded when a Seeker's Call/Text opens the phone app (with the text they sent). The pro reads them as their Inbox (`GetForProfessionalAsync`); the Seeker's Inbox is the same records grouped by pro (`GetForSeekerAsync`). **`IReviewRepository`** (`Dev/StubReviewRepository`): Seekers write one review per pro they contacted (`SaveAsync` replaces); `GetSummariesAsync` feeds search ranking. Both stubs persist to a JSON file on the phone (`dev-inquiries.json`, `dev-reviews.json`) and add SAMPLE rows only in Debug (`Dev/DevSamples`), so a Release build never shows invented activity.

**`Local/JsonFile<T>`**: the one place that reads/writes an app-private JSON file (atomic temp-file write, corrupt file reads as missing, source-generated `JsonTypeInfo`). `FileOnboardingStore` and the stubs use it.

**`LocalAccount`** (`Services/`, singleton): the person on this phone, whichever side: app language (`SetLanguageAsync`, raises `Changed` for the layouts) and **Start over** (`StartOverAsync`): deletes their photos, published listing, inquiries and reviews (`DeleteForPersonAsync` on both repositories), clears `IOnboardingStore` (`ClearAsync`) and makes both workspaces `Forget()`, so `/` restarts onboarding at the language screen. With auth it becomes sign-out / delete account.

Until auth, the people on this phone have fixed ids: the pro's listing is `ListingMapper.OwnListingId` (`"own"`), the Seeker is `SeekerWorkspace.LocalSeekerId` (`"me"`). The auth gate replaces both with the user's uid.

**`IPhotoPicker`** (`Local/DevicePhotoPicker`, MAUI `MediaPicker.PickPhotosAsync`, resized to 1280px) and **`IPhotoStore`** (`Local/FilePhotoStore`): photo id = SHA-256 of the bytes (anti-fraud rule 2), files in `AppDataDirectory/photos`, shown in the WebView as data URLs. Gallery-only, no camera, so Android 13+ needs no permission (the manifest has `READ_EXTERNAL_STORAGE` `maxSdkVersion=32` for older phones; iOS has `NSPhotoLibraryUsageDescription`).

**`IProfessionalProfileRepository`**: `PublishAsync` returns `PublishResult.PhoneTaken` for a number that already has a listing (anti-fraud rule 1); `GetByIdAsync` serves the public profile and Inbox; `RemoveAsync` takes a listing out of search (Start over). The stub keeps the published listing in memory only, so it shows in this phone's Seeker search but is lost on restart.

### App shell (shared by both sides, after the interview)

- **`Components/Layout/AppShell`**, Slack style: `AppTopBar` (brand on the left, the user's `PersonAvatar` on the right linking to their page, optional green status dot), the page in the middle, `AppBottomNav` icon tabs (`AppTab`: href, icon, label, badge, `MatchPrefix` to keep a tab lit on its sub-pages). Fixed parts use the `app-*` gap classes in `app.css`; a page's main action sits last with `app-sticky-action` so it stays just above the tabs.
- Each side has a thin layout that loads its workspace (non-matching users go back to `/`) and hands `AppShell` its tabs: **`Professional/ProLayout`** and **`Seeker/SeekerLayout`**. Pages opt in with `@layout`. Full-screen steps (`/professional/publish`, `/reviews/rate/{id}`) use `InterviewStep` instead, outside the shell.
- Routes live in `ProRoutes` / `SeekerRoutes`; link through them, never a literal path.

### Pro side (`Components/Professional/`)

- **`ProWorkspace`** (`Services/`, singleton) holds the pro's draft, photos, inquiries and reviews and the actions on them (add/remove photos, change own photo, publish, mark inquiries seen); it raises `Changed` so the shell re-renders.
- Tabs: Home `/professional` (`SetupCard` checklist until published, collapsible `HomeSection`s, + button adds work photos), Inbox `/professional/inquiries` ("new" badge), Reviews `/professional/reviews`, Me `/professional/me` (`ProMePage`, also the avatar: own photo, Edit listing, Publish, a link to work photos, card preview, then the shared `MePage` parts). Work photos live under Me at `/professional/me/photos` (up to 12), so the Me tab stays lit.
- Profile and work photos live on `ProfessionalDraft` (`ProfilePhotoId`, `WorkPhotoIds`, `PublishedAt`, `InquiriesSeenAt`) and show on `ProCard` (avatar photo + the newest 3 work photos).

### Seeker side (`Components/Seeker/`, `Search/`, `Inbox/`)

- **`SeekerWorkspace`** (`Services/`, singleton): the current search (saved into `SeekerAnswers`, so it survives Back and restarts), Inbox threads, the Seeker's reviews, and Call/Text (opens the phone app via `IContactLauncher`, then records the `Inquiry`). The pre-written message comes from their answers (job, town, urgency).
- Tabs: Home `/seeker` (DoorDash/Grab style: dense pill search box, the town `FilterMenu` under it, `CategoryRail` of trade icons in two sideways-scrolling rows, then last search, recent messages, pros to rate), Search `/search`, Inbox `/inbox` (+ thread `/inbox/{proId}`), Reviews `/reviews` (badge = contacted pros not yet rated; write at `/reviews/rate/{proId}`), Me `/me` (`SeekerMePage`, also the avatar: name shown on reviews, then the shared `MePage` parts). The Seeker side has 5 tabs.
- **Flow:** search → tap a result → public profile `/pro/{id}` (facts, jobs, work photos, reviews, pinned **Contact**) → thread `/inbox/{id}` with a ready message: **Send text** opens SMS, **Call** opens the dialer; both are logged in the thread and on the pro's Inbox. Replies come to the Seeker's phone, outside the app. Only contacted pros can be reviewed.
- **Search relevance (`Services/Search/`)** — keep it explainable, every weight is a named constant:
  - `SearchText`: normalises typed words (lower case, accents off, English/Tagalog filler words dropped) and scores word matches (exact 1, typed prefix 0.8, longer form 0.7).
  - `SearchIntentParser`: free text → trade + (when one clearly fits) job, matched against both languages' category/job labels, category hints and `TradeWords` synonyms (tubero, kuryente, hinang, masahe…). Generic job words (sira, repair, install…) count half toward the trade; a tie or weak match returns no trade plus the closest ones, and the UI asks "which kind of pro?". Add a synonym to `TradeWords` when users type a word we don't catch.
  - `SearchRanker`: only pros listing the exact job (falls back to the whole trade, flagged, when nobody does); grouped "goes to your town" then "other towns"; scored by budget fit, stars (smoothed toward 4.0 so one review can't beat many), experience and photos.
  - `ProfessionalSearch` fetches the trade's pros from all towns plus their review summaries, then ranks.

### Onboarding & Initial Navigation (interview-first, auth after)

Full spec and research rationale: [`docs/onboarding.md`](docs/onboarding.md).

- **`/` = `Onboarding/LaunchRouter`** (no UI). It applies the saved language, then routes: `/start` (language) → `/start/role` (Seeker / Professional fork) → the saved interview step → `/seeker` (Seeker home) or `/professional` (Professional). Right after the Seeker interview the loader lands on `/search` (the payoff).
- **Interviews:** `/start/seeker/{step}` covers job → service → town → when → budget, then the labor-illusion loader, then results. `/start/pro/{step}` covers trades → jobs → towns → experience → rate → monthly earnings goal → name → number → preview. There is one URL per step, so Android back walks the steps. Answers are saved as the user moves forward.
- **Money is the hook on both sides.** Seeker budget and pro rate share one price scale (`ReferenceData.PriceTiers`), which drives the "Pasok sa budget mo" badge and the sort order. The pro's private monthly goal becomes "≈ N jobs a week" (`GoalPayoff`, `Money.JobsPerWeek`).
- **No login before or during onboarding.** Auth will go *after the interview and before the first benefit* (`SeekerInterview.FinishAsync` / `ProfessionalInterview.FinishAsync`). **That workflow is still being designed by the owner. Don't wire auth into the flow until it's decided.**

### Auth

- **`AuthPage`** (route **`/verify`**, `Components/Auth/`): **Phone OTP + Google** sign-in via `IIdentityProvider`. It isn't linked from the flow yet.
- **Adapter:** native Firebase SDK (Plugin.Firebase) in `Services/Firebase/` (`#if ANDROID || IOS`). Firebase init and `GetCurrentUserAsync` are wrapped so a missing config doesn't crash launch — the app loads and sign-in fails gracefully.
- **Auth error policy:** the adapter never reduces an auth-infra exception to a bare message. It logs the full exception (type + stack) to Logcat/Output and returns an `AuthResult` with a decoded reason. For Google sign-in it parses the Google Play status code out of the exception message (which is just `"7:"`, `"10: …"`) — no dependency on the Play-services binding — and maps it to human text (`7` NETWORK_ERROR, `10` DEVELOPER_ERROR, `12501` SIGN_IN_CANCELLED, …) so failures are legible instead of an opaque "7". `SignUpWithPhoneAsync` logs then rethrows (fail fast).
- **Post-auth routing** (`AuthPage.razor.cs` → `IProfessionalProfileRepository.HasProfileAsync`): if a profile exists, go to `/professional`. Otherwise go to `/`, where the launch router resumes onboarding.
- **Packages (Android/iOS only):** `Plugin.Firebase.Auth` 3.1.1 + `Plugin.Firebase.Auth.Google` 3.1.2 (the **modular** Auth packages — NOT the bundled `Plugin.Firebase`, which drags in Crashlytics and crashes at launch in MAUI). Google sign-in uses `CrossFirebaseAuthGoogle.Current` and needs `FirebaseAuthGoogleImplementation.Initialize(webClientId)` at startup — `GoogleWebClientId` in `Services/Firebase/FirebaseRegistration.cs` is the web OAuth client (client_type 3) from `google-services.json`. Android also **requires** `MainActivity.OnActivityResult` to forward to `FirebaseAuthGoogleImplementation.HandleActivityResultAsync(...)`; without it `SignInWithGoogleAsync()` hangs forever (no exception, stuck on "connecting"). The app's debug/release **SHA-1 (and SHA-256 for phone auth) must be registered in the Firebase console** and `google-services.json` re-downloaded — otherwise Google sign-in returns `DEVELOPER_ERROR` (status 10) and phone verification fails app-attestation.
- **Firebase config (not committed; required before an Android/iOS run):** `Platforms/Android/google-services.json`, `Platforms/iOS/GoogleService-Info.plist`, plus a Google OAuth client (Android SHA-1; iOS reversed-client-id URL scheme). The csproj references the config files with `Condition="Exists(...)"` so the build works without them.

### Anti-Fraud Rules

These are hard business rules — never bypass them:
1. **1 Phone = 1 GCash = 1 Registration** — unique constraint, no duplicates
2. **Duplicate image detection** — SHA-256 or perceptual hash on portfolio uploads, reject duplicates

## Target Solution Structure

```
SideGigPh.slnx
├── SideGigPh.Maui/          #  Class Library for Business Logic only
│   ├── Components/            # UI views — one folder per feature
│   │   ├── Search/           # e.g. Search.razor + Search.razor.cs (code-behind)
│   │   │   ├── Search.razor #landing page of search
│   │   │   └── Search.razor.cs
│   │   ├── Registration/
│   │   └── Shared/           # Shared components 
│   │   └── Layout/           # Shared layout / shell components
│   ├── Models/                # Data contracts only no DTOs
│   └── Services/              # Interfaces (IIdentityProvider, ITradesmanRepository)
│
├── SideGigPh.BusinessLogic/            # Native app shell (Android/iOS via BlazorWebView)
│   └── BusinessLogic1.cs         # DI container — binds adapters
```

The current codebase is a single `SideGigPh` project. It will be restructured into the above layout.

## Coding Standards

- **C# / .NET 10**, nullable enabled, implicit usings
- **Interfaces first** — define the port, then implement the adapter
- **No vendor lock-in** — Firebase, Azure, etc. must sit behind an interface
- **Mobile-first UI** — every component must work on phone-width viewports
- **Reusable, composable components** — build small, single-responsibility Razor components and compose them; never create one giant `.razor` page that does everything. Extract repeated or self-contained UI into its own component with `[Parameter]`-driven inputs and `EventCallback` outputs
- **Component folder structure** — group components by feature under `Components/<Feature>/`, and always use code-behind: each component is a `<Name>.razor` markup file paired with a `<Name>.razor.cs` partial class for its logic. Shared shell/layout components live under `Components/Layout/`
- **Generic vs feature components** — a component used by two or more features and not tied to any single feature is a *generic* component and belongs in `SideGigPh.Shared` (e.g. `SideGigPh.Shared/Components/`), not under a feature folder. Feature-specific components stay with their feature. Until that project exists, generic components live in `SideGigPh.Maui/Components/Shared/`: `InterviewStep`, `ChoiceTiles` (+ `ChoiceTile`), `WorkingLoader`, `ProCard` (+ `ProCardFact`; one card for search results *and* the pro's own preview), `ListingFacts` and `ServiceChips` (shared by the card and the public profile; their text comes from `Services/Localization/ListingText`), `PersonAvatar` (photo, initials or a person icon), `HomeSection` (collapsible home section), `ReviewRow` + `RatingSummary`, `FilterMenu` (a chip with a dropdown: Search filters and the home town picker), `GoalPayoff`, `BrandMark` (logo + name), and `MePage` (the Me page frame both sides share: title, the side's own sections, then `LanguagePicker`, the on-this-phone note and `StartOverCard`, a confirm-twice Start over).
- **MudBlazor only, no raw HTML** — markup is built from MudBlazor components; don't write `<div>`, `<span>`, `<p>`, `<button>`, `<input>` etc. in `.razor` files. Use `MudStack` for layout and spacing (`Row`, `Spacing`, `AlignItems`, `Justify`, `Wrap`), `MudText` for all text (`Typo` for size, `HtmlTag="h1"` for headings — `FocusOnNavigate` focuses the page's `h1`), `MudContainer` for page width, and MudBlazor's own spacing/flex utility classes (`pa-4`, `flex-1`, `mud-width-full`…). Reach for the out-of-the-box component first (`MudButton`, `MudTextField`, `MudChip`, `MudAlert`, `MudCard`, `MudSkeleton`, `MudProgressLinear`…). Only when MudBlazor has no parameter or utility for something, add a small, named helper class in the "MudBlazor gaps" section of `wwwroot/app.css`. Scoped `.razor.css` files don't reach inside MudBlazor components, so don't add them. The only raw HTML is `wwwroot/index.html` (the pre-Blazor splash).
- **Bilingual copy (English / Taglish)**: never hard-code user-facing text in a component.
  - UI strings are `LocalizedText(English, Taglish)` pairs in `Services/Localization/Copy.cs`. Answer lists (jobs, towns, prices…) are in `Services/ReferenceData.cs`.
  - Components inject `AppText T` and render `@T.Get(...)` / `@T.Format(...)`.
  - Write for low English literacy: short plain words, an icon on every choice tile, and numbers (₱, years) doing the work.
  - Taglish is everyday conversational Taglish, not formal Filipino. Use local lingo, not forced translation:
    - Keep the English technical terms (split-type, chemical wash, freon, breaker, inverter, PMS, CCTV, waterproofing).
    - Use the local words people actually say (CR, gripo, lababo, yero, alulod, kisame, semento, poso).
    - Use Pa-/Paki- only where it sounds natural.
  - Job labels stand on their own ("Linis ng aircon"), so they also read correctly inside the pre-filled SMS.
- **Theming / colors** — the palette, typography and corner radius live in one `MudTheme`: `SideGigPh.Maui/Components/Layout/AppTheme.cs`. The brand palette is **blue/white** (GCash/Microsoft style): primary blue `#0078d4`, white surfaces, navy ink text (`Color.Dark`). `Secondary` is the muted slate, so `Color.Secondary` is the helper-text color; `Info` is the brand blue. Components pick colors only through MudBlazor's `Color` parameters; any rule in `app.css` uses the theme's `var(--mud-palette-...)` variables. Never hard-code hex in components or CSS (the one exception: the pre-Blazor splash fallbacks in `app.css`, which must match `AppTheme`). To restyle the app, change `AppTheme`.
- **No dead code** — delete unused code, don't comment it out
- Keep business logic in `SideGigPh.Shared`, platform-specific code in `SideGigPh.Maui`
- Any change to the project structure , architecture, design patterns, theme, design should reflect in this CLAUDE.md

## Build & CI

- GitHub Actions workflow at `.github/workflows/build.yml`
- Android builds on `ubuntu-latest`, iOS builds on `macos-latest`
- Both install .NET 10 + platform-specific MAUI workload
- Working directory for builds: `SideGigPh.Maui/`

## Common Commands

```bash
# Build Android
dotnet publish -f net10.0-android -c Release

# Build iOS (macOS only)
dotnet build -f net10.0-ios -c Release

# Restore workloads
dotnet workload install maui
```

## Project Skills (`.claude/skills/`)

Load the matching skill before doing these tasks. Every folder in `.claude/skills/` is listed here; add a line when you add a skill, and remove it when you delete one.

**Building features**
- **`new-razor-component`**: any new, split-out or restyled `.razor` component, page, tab or route (folders, code-behind, the `AppShell` layouts and routes). Script: `ui-lint.sh` (raw HTML, hex, literal text, page h1s).
- **`mudblazor-ui`**: MudBlazor 9 markup, verified parameter/enum/icon/utility names, gap classes, theme. Script: `mud-api.sh`.
- **`taglish-copy`**: any user-facing text (English/Taglish wording, local lingo, SMS templates). Script: `copy-check.sh` (missing/unused keys, placeholder mismatches).
- **`ports-and-stubs`**: new stored data or a new port method: model, port, `Dev/` stub persisted with `JsonFile`, Debug-only samples, DI, workspace.
- **`search-relevance`**: how Seeker search understands words and ranks pros (`Services/Search/`), including after category/job label changes. Script: `search-check.sh` (runs the real search code against queries).
- **`add-interview-step`**: adding or changing an onboarding question, or the flow and resume logic.
- **`onboarding-ux-review`**: UX critique against the onboarding research, plus trust guardrails (permissions, hooks, sign-up placement).
- **`maui-firebase-auth`**: Firebase phone OTP / Google sign-in behind `IIdentityProvider`, `google-services.json`, SHA-1, status codes 7/10/12501.
- **`maui-app-branding`**: launcher icon, native splash, in-app loading screen.

**Build, run and fix**
- **`build-check`**: compile-check a throwaway copy after code changes (safe with Visual Studio open). Never build or deploy in the repo folder from the CLI.
- **`maui-build-troubleshooting`**: cancelled/locked builds, and a Debug app that dies on launch with "No assemblies found … Fast Deployment". Script: `fix-fast-deploy.sh`.
- **`android-emulator-vs-deploy`**: emulator with no internet, network errors only on the emulator, VS stuck on "Waiting for emulator".
- **`rename-project`**: renaming the project or root namespace, or CS0234 after a rename.

**Manual only**
- **`update-skills`**: capture lessons from a session into these skills. Run only when the user asks.
- **`update-tools`**: turn repeatable checks, fixes or queries from a session into helper scripts in `skills/*/scripts/`. Run only when the user asks.
