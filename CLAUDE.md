# SideGigPh — Tradesmen Open Directory (Philippines)

## What This Is

A high-density open local service directory (Carousell/Craigslist style) connecting urban households with skilled tradesmen and home-service pros. The categories (trades), in the order shown, are Aircon Technician, **Construction** (Foreman, Carpentry, Plumbing, Masonry, Tiles, Welding, Roofing & gutter, Steelman, Helper), **Errands** (house cleaning, grocery, bills, PasaBuy, documents, parcels, car cleaning, gardening), IT, Electrician, Carpentry (Karpintero), Masonry (Mason), Car Mechanic (Mekaniko) and Solar Panel Technician (`SortOrder` in `004-config.sql`). **They live in Azure SQL** (`Categories` / `Services`, filled by `SideGigPh.AzureSQL/Sql/004-config.sql` with the other answer lists in `ConfigOptions`: towns, price tiers, experience, urgency, income goals) and reach the app in one **config** document, the public `GET config` (see App config below): `ReferenceData.Categories` / `ServicesByCategory` / `Cities` / `PriceTiers`… read it; `ReferenceData.BuiltInConfig` is the copy shipped for the first launch offline; the server's replaces it, so **a label change or a new trade is a SQL-only edit** (the integration test only checks every built-in id still exists in `004-config.sql`). Use `ReferenceData.ServicesOf(categoryId)`, never `ServicesByCategory[...]`, since a trade can disappear. **Category and service IDs are stable; only labels change.** A trade we stop offering is retired (`IsActive = 0`, kept in `004-config.sql`, e.g. `home_massage`), never deleted or reused; saved answers naming it are dropped on load (`OnboardingCleanup.DropRetiredJobs`). A trade **merged into another** as one of its jobs (Plumber, Welder, Roofing & Gutter → Construction's `construction-plumbing` / `-welding` / `-roofing`) is retired too, its server rows moved by a schema script (`019-schema-construction-trade.sql`, `020-schema-construction-roofing.sql`) and saved answers moved on load (`OnboardingCleanup.MergedTrades`), so nobody is asked again. Each category's services are ordered most-requested first and end with **"Others / Wala sa nabanggit"** (`…-other`, `ReferenceData.IsOtherJob`). Picking it shows `Onboarding/OtherJobField` so the person can type the job (`SeekerAnswers` / `ProfessionalDraft.OtherJobText`); the job stays "Others", and the text is sent once for analytics (`OtherJobNotes.NoteAsync` → `POST other-jobs` → table `OtherJobNotes`; it also words the Seeker's first message). **Both interviews' trade question also ends with an "Others" tile** (`ReferenceData.TradeChoices` = trades + `OtherTradeId` `"other"`, in code, never in `004-config.sql`): the person types the kind of work we don't list and **it is saved as their trade and job** (`CategoryId = ServiceId = "other"`, `PickTrade` / `PickCategory`; the jobs question is skipped, `NextPath` / `BackPath`), so they always finish the interview and land in the app: **onboarding never loops or blocks on a missing option**; a feature that truly needs missing data disables its own button instead (only a worker's verified number stays required to publish; badges are optional). `ReferenceData.FindTrade` / `FindService` / `ServicesOf` know "Others". **"Others" text is matched, not just counted:** a worker's text is stored on the listing (`Professionals.OtherWorkText`, `ListingDto.OtherWork`, shown in place of "Others"), a Seeker's search for an "Others" job sends theirs (`otherText`), and `SearchRanker` orders workers who share its meaningful words first (`OtherWordsMatch`, `FeedbackMatch` words). **Prices are from–to ranges and the suggested minimum price is only a guide:** the Seeker budget is two boxes (`BudgetRangeFields`; the guide's ranges fill them, "Not sure yet" skips), the worker's rate is their own range (`RateRange` `"rate-800-1500"` in `RateTierId`; guide levels fill the boxes, "depends on the job" stays), and a job post has `BudgetMinPesos` ("from", optional) under `BudgetPesos` ("up to", what "Take it" offers); households pay after the work, at the price both agree on. **Users see the app as "Tarbaho"** (name in `AppTheme.Brand`, `ApplicationTitle`, `App.xaml.cs`, `wwwroot/index.html` and `Copy.cs`); so does the Android package id `com.tarbaho.app` (the Firebase Android app); the code, projects, Firebase project and cloud resources keep the SideGigPh name. Brand voice: workers are called **Manong / Kuya / Ate / Manang** (`Services/Honorifics`: random on the splash and search box, the trade's own once one is picked), never "laborers"; details in [`docs/architecture.md`](docs/architecture.md#brand-tarbaho). Neutral "Yellow Pages" model — direct consumer-to-tradesman contact, zero transaction commissions.

**Target region (MVP):** three Rizal towns, **Angono, Antipolo and Taytay** (`ReferenceData.Cities`). Coverage was briefly four areas (Metro Manila, Rizal, Laguna, Cavite) and was cut back on purpose.

**Town IDs are stable** (no accents: `taytay`, `angono`, `antipolo`); only labels change. All towns sit in one area, `ReferenceData.ServiceAreaId` (`rizal`), which is **never asked**: `ReferenceData.AreaOf(cityId)` gives it (null for a town we don't serve), the interviews set `AreaId` from the town(s), and listings and search still carry it, so more areas can return without a schema change. A Skilled Worker ticks **any number of the towns** they go to; a Seeker picks **one home town**. Saved answers naming a town we no longer serve are cleaned on load (`Services/OnboardingCleanup`, called by `FileOnboardingStore`), and `LaunchRouter` reopens the town question for a finished interview that lost its town.

**Audience:** many Professionals (tradesmen) are not confident English readers. The app is **English / Taglish**, and the user picks one on the very first screen.

## Tech Stack

- **Frontend:** .NET MAUI Blazor Hybrid — shared Razor Class Library for Android and iOS
- **Target platforms:** Android + iOS only (Windows and Mac Catalyst are not supported)
- **UI:** Mobile-first. Component library: **MudBlazor** (9.x, version set once in `MobileAppTheme.csproj`). Providers (`MudThemeProvider`, `MudPopoverProvider`) sit in the skeleton's `MainLayout`; MudBlazor's and the skeleton's CSS/JS load from the app's `wwwroot/index.html`
- **App skeleton:** **`MobileAppTheme/`** is a reusable Razor class library (plain `net10.0` + `Microsoft.Maui.Essentials`) holding the shell, theme builder, generic components, localization and on-device adapters. `SideGigPh.Maui` references it; the skeleton never references the app, so the next MVP can start from it. See [`MobileAppTheme/README.md`](MobileAppTheme/README.md) and the `mobile-app-theme` skill.
- **Auth (MVP):** Firebase Authentication — Phone/SMS OTP + Google login. Firebase is the sign-in provider and sends the new-message notifications (Cloud Messaging, below); its console setup and the SMS-region script are in [`firebase/`](firebase/README.md)
- **Database:** **Azure SQL** — users, listings, reviews, chats, the app config (trades and jobs, towns, price tiers…). The phone never connects to it: the app calls **`SideGigPh.Api`**, which checks the Firebase ID token and uses **`SideGigPh.AzureSQL`** (EF Core 10, `Microsoft.EntityFrameworkCore.SqlServer`). **The schema is the T-SQL in `SideGigPh.AzureSQL/Sql/`** (`001-schema.sql` the first release's tables/keys/indexes, **frozen: the database is live**, so every later change is a new guarded, numbered script `NNN-schema-<what>.sql` (`005-schema-ranking-signals.sql`, `007-schema-config-tables.sql`, `008-schema-service-prices.sql`, `009-schema-users-phone-index.sql`, `010-schema-error-reports.sql`, `011-schema-safety.sql`, `012-schema-feedback.sql`, `013-schema-geo.sql`, `014-schema-verification.sql`, … `017-schema-others-and-ranges.sql`, `018-schema-job-done.sql`, `019-schema-construction-trade.sql`, `020-schema-construction-roofing.sql` (data: Plumber/Welder/Roofing rows → Construction)), applied by the deploy **straight to the live database when it reaches `main`** (`deploy-api.yml` → `db-check` dry run → `db-migrate` → deploy; no approval step, merging is the approval; tool `SideGigPh.Migrate`, never `001`, seeds or, unless ticked on a manual run, `004-config`), recorded in `dbo.SchemaChanges`, its name added to `SchemaScripts.Required` (the warm-up writes an error report naming any the live database is missing); load the `db-migration` skill; `004-config.sql` the trades, jobs and other answer lists (required data, an idempotent upsert that retires what it no longer lists), `002-seed.sql` sample workers and reviews with ids `seed-…`, no chats or photos; `006-seed-search-ranking.sql` 45 sample workers (`seed-s2-…`) with reviews and chats, one trade per ranking signal, for [`docs/search-ranking-test-plan.md`](docs/search-ranking-test-plan.md), its orders checked by `SearchRankingSeedTests`, `@RemoveOnly = 1` removes it); there are no EF migrations, so `SideGigPhDb.cs` must match 001 plus every later schema script. The connection string is the Function App setting **`SqlDatabase`** (or the git-ignored `local.settings.json`), never in the phone app. Plan, tables and routes: [`docs/data-model.md`](docs/data-model.md); setup: [`azure/`](azure/README.md)
- **Photos (MVP):** Azure Blob Storage (account `sidegigphbfb9`, container `photos`, blob `{sha256}.jpg`, public read by URL), written only through **`SideGigPh.Api`** (Azure Functions, isolated worker, .NET 10, Function App `SideGigPhApi`, https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/), which re-encodes every upload (`PhotoImage`: refuses more than 64 MP from the header, then upright, sRGB, ≤ 1280px, JPEG 82, EXIF/GPS stripped) and refuses another account's photo (anti-fraud rule 2). The storage connection string lives only in the Function App setting `PhotoStorage` (or the git-ignored `local.settings.json`), never in the phone app; the app uses `ApiPhotoStore`. Details: [`docs/architecture.md`](docs/architecture.md#photos); setup: [`azure/`](azure/README.md)
- **Shared contracts:** **`SideGigPh.Contracts`** (plain `net10.0`, no dependencies): the JSON records the app and the API send each other (`ListingDto`, `ChatDto`, `PublishListingRequest`, `SearchPageDto`…, `Wire` strings, `ApiErrors` codes, `Paging` page sizes) and the source-generated `ApiJson` context. Every new contract type and its `List<T>` needs a `[JsonSerializable]` line there (trim-safe on the phone)
- **Future migration:** PostgreSQL/PostGIS for spatial queries (`ST_DWithin`): only `SideGigPh.AzureSQL` changes
- **Tests:** **`SideGigPh.Tests`** (plain `net10.0`; xUnit 2, FluentAssertions **7.x** (8+ needs a paid licence), `Microsoft.AspNetCore.TestHost`, coverlet). `Unit/` (fakes only) and `Integration/` (the real API functions in memory, a fresh SQL Server database built like live is upgraded, the app's real services; trait `Category=Integration`). The csproj **compiles `SideGigPh.Maui/Models/**` and `Services/**` by link**, so keep pages' rules in `Services/`. Snapshot tests in `Unit/UpdateSafety/` fail on purpose when an id the code names, a JSON field name or an error code changes; they never list data ids (trades, jobs), so adding data never needs a test edit. Details: [`docs/architecture.md`](docs/architecture.md#tests) and [`SideGigPh.Tests/README.md`](SideGigPh.Tests/README.md); after a relevant change load the `run-tests` skill

## Architecture — Ports & Adapters

All external infrastructure is decoupled behind C# interfaces (Dependency Inversion). On the phone: page → workspace → port → `Services/Api/` adapter → HTTPS → `SideGigPh.Api` function → store in `SideGigPh.AzureSQL` → Azure SQL. Swapping the database touches only `SideGigPh.AzureSQL`; swapping the sign-in provider touches only its adapter project (Auth below).

### Architecture map

Each line is the rule to keep; **the full reference for every area (ports, adapters, caching windows, app shell,
both sides' pages, onboarding, auth) is [`docs/architecture.md`](docs/architecture.md): read its section before
changing that area.**

- **Ports** (`SideGigPh.Maui/Services/`, skeleton ports in `MobileAppTheme/Services/` and `Auth/`): `IIdentityProvider`
  (sign-in; Firebase adapter), `IOnboardingStore` (answers on the phone, one JSON file via the skeleton `JsonFile<T>`,
  never the WebView's `localStorage`, no SQLite until local data needs querying; Android backup is off so a reinstall starts fresh), `IChatRepository`, `IUserRepository`,
  `IReviewRepository`, `IProfessionalProfileRepository`, `IConfigSource<AppConfig>`, `IOtherJobNoteRepository`,
  `IPhotoPicker` / `IPhotoStore`, `IContactLauncher`, `IPushNotifications`, `IErrorReportRepository` /
  `IErrorLogStore` (error reports), `ISafetyRepository` (report and block), `IFeedbackRepository` (feedback board), `IJobPostRepository` (job posts),
  skeleton `IAnalytics` / `IExperiments` (usage analytics and A/B tests; do-nothing defaults), `IAppAccount` (= `LocalAccount`: language, sign out keeping the answers (the add-phone page's "Sign in with this number" when a Google account's number belongs to another account), Start over, Delete account), skeleton `ISplashLines` (= `RoleSplashLines`: the busy splash's lines by role).
- **API adapters** (`Services/Api/`, the only app code that talks HTTP): `ApiClient` (one `HttpClient`, the Firebase
  ID token on every request, asks for gzip) and one `Api*Repository` per port, registered by `AddSideGigPhApi`
  after `AddMobileAppTheme`; `ApiMapping` converts contracts ↔ `Models/` (models never get contract types). Failed
  reads log and come back empty; failed writes log and throw, and pages show "didn't send / didn't publish".
- **Ids:** the signed-in uid is the person's id on both sides (`ProWorkspace.MyId` = listing id,
  `SeekerWorkspace.MyId`); the server always takes it from the token, never the body. Compare against those, never a literal.
- **Workspaces** (`ProWorkspace`, `SeekerWorkspace`, singletons) hold each side's data and actions and raise
  `Changed`; pages call them, keep rules in `Services/` (tests compile only `Models/` and `Services/`).
- **Lists come in pages:** search (`Paging.Search` 4, ranked on the server), chat messages (`Paging.Messages` 20,
  `ChatHistory`), Inbox (`Paging.Inbox` 30, `InboxPages`); each scrolls with the skeleton `LoadMoreOnScroll`. The
  server still answers old apps that don't send `take`.
- **Caching windows are named constants:** app `SearchFreshFor` 60 s, `ListingFreshFor` 5 min, `ActivityFreshFor`
  15 s, chat poll `WatchInterval` 5 s (paused while the app is hidden, skeleton `AppVisibility`); server
  `ListingCache` 30 s, `ConfigStore.ConfigFor` 12 h; the app config `RemoteConfigRules.FreshFor` 12 h (only for the app build that saved it: an update asks at once), then
  `If-None-Match` → `304`. Writes update lists from the server's answer, never re-read everything.
- **App config** (trades, jobs with price guides, towns, minimum wage, answer lists): one `ConfigDto` from public
  `GET config`, loaded once per launch behind the splash (`LaunchRouter` → `AppConfigService.LoadAsync`), held by
  `ReferenceData` (`UseConfig`, guarded by `CanUse`); pages never fetch it. New stale-OK data goes into this config,
  not a new endpoint.
- **Server:** one function class per area, token check first (`FirebaseTokenValidator`; the only public routes are
  `GET config`, the legal pages `GET legal/{page}` and, until launch, the error-report reader `GET error-reports`, `ApiWiringTests.PublicRoutes`), then a store in `SideGigPh.AzureSQL` holding the data rules and
  answering with contracts or an outcome enum. Errors are `{ "error", "detail" }` (`ApiErrors` codes); `detail`
  carries the stack trace while `ApiResults.ShowDetails` is on (Function App setting `ShowErrorDetails` = `false`
  turns it off: do that before launch). Repeatable writes are limited per account (`CallerLimits` → `429
  too-many-requests`; publish/remove a listing and profile saves too, since each listing change clears the shared
  search cache); every JSON body is capped at `ApiResults.MaxJsonBytes` (256 KB, `400 bad-body`); JSON answers are gzipped for callers that accept it (`GzipMiddleware`). Multi-row writes use
  `db.InTransactionAsync`; a caller's own data sits under `…/me` / `…/mine`. Settings: `PhotoStorage`,
  `SqlDatabase`, `FirebaseProjectId`, optional `FirebaseServiceAccount`, `ShowErrorDetails`, `DocumentIntelligenceEndpoint` / `DocumentIntelligenceKey`, `AdminUids`. **Cold starts:** the
  app's `ServerWarmup` calls signed-in `GET warmup` (`WarmupStore`: `SELECT 1`, at most once a minute per instance)
  when it opens and when it comes back after `IdleAfter` (5 min), so the Function App and the auto-pausing database
  wake behind the splash instead of on the first search.
- **Chats:** only the Seeker starts a chat (id `{proId}_{seekerId}`); the server stamps times and takes the worker's
  name from their listing; reviews only 24 h after the chat started (`ReviewRules`, both sides), or at once after a
  **completed job**: a picked offer both sides marked done (`PickedJobStore`, `018-schema-job-done.sql`,
  `picked-jobs/…`; both chat pages show it as `PickedJobCard` with **Mark done**, re-read every 10 s while it waits, `PickedJobWatch`; a pick moves an existing chat to the post's service and town), which shows "Verified service" on the
  review (`ReviewRow`), counts on the listing (`CompletedJobs`, `ListingFacts`) and lets the worker rate the household
  on the card (`HouseholdReviews`; workers see its stars on its posts).
  **Notifications:** a customer writing to a worker rings, a worker's reply only badges the icon; permission is asked
  only with a reason, never at first launch (`ChatAlerts`; a household is asked right after its first post). A new
  job post rings the workers its Requests feed would show it to (`JobAlerts`, `JobPostStore.WorkersToAlertAsync`, at
  most `MaxWorkers` 30: a service they offer first, then nearest; a retry doesn't ring again), and an offer rings the
  household; channels `requests` / `offers`, a tap opens the feed or `/posts/{id}/offers` (`ChatAlerts.PathFor`).
- **Error reports** (`Services/Diagnostics/`): logged errors (`ErrorLogLoggerProvider`), crashes and loading screens
  stuck past `BusyIndicator.StuckAfter` go into `ErrorLog` (`diagnostics.json`), **scrubbed when recorded and again on
  the server** (`ErrorReportRules.Scrub`: no tokens, numbers, emails, uids, query strings), and `ErrorReportSender`
  sends them in the background while signed in (`POST error-reports` → `ErrorReports`, with app and code version,
  channel `dev`/`beta`/`prod` (csproj `AppChannel`; store builds pass `-p:AppChannel=prod`), OS and phone; install id never the uid,
  deleted after 90 days). On by default, disclosed in the privacy policy, off with the Me page's `ErrorReportsSwitch`.
  Never log message text, names or numbers on purpose; the scrubber is the safety net, not the plan. A page that
  throws (render or tap handler) is caught by the skeleton's `AppErrorBoundary` in `MainLayout`: logged (so
  reported; BlazorWebView alone only writes it to the WebView console) and replaced by a "back to the start" screen.
  On Android, `Platforms/Android/AndroidCrashCapture` (from `MainApplication.OnCreate`) also records Java exceptions
  nothing caught (`Crash.Java`) and, at each launch, how earlier runs ended (`ApplicationExitInfo`: crash, native
  crash, ANR with the main thread's stack, memory kill while on screen; `Services/Diagnostics/PastExits`, source
  `Crash.LastRun`, each exit once), so crashes .NET never sees still reach `ErrorReports`; a lost connection that
  escapes to the top (skeleton `NetworkErrors.IsNetwork`: only a request that got no answer, never a file error such as a full phone) is reported (`Crash.Network`) and the app keeps running.
  **A lost connection is never "something broke":** `AppErrorBoundary` shows "No internet" + Try again (rebuilds the
  page) for it, and the skeleton `ConnectionBanner` (`INetworkStatus`) shows a gray "you're offline" strip, sticky above the top bar, after 3 s offline (not on a 1-second signal drop); the busy splash says "Slow connection" (offline: "No internet. Waiting…") once a wait passes `BusyOverlay.SlowAfter` (12 s); a chat's "didn't send" sits inside the pinned `ChatComposer`. A failed read comes back empty, so a list read from the server passes `OnRetry` to the skeleton `EmptyState`: offline it says "No internet" + Try again (never "nothing here") and reloads by itself when the connection is back; `LoadMoreOnScroll` shows Try again when a page never arrives; `ApiClient` gives each GET `ReadTryLimit` (30 s, so two tries match the old 60 s timeout a cold server and paused database may need) and one more try (`ReadTries`), and sends a write once. A push while the app is open refreshes the Seeker's posts and the worker's Requests feed (the layouts' `Alerts.Attach`).
  Read the reports without a database login at public `GET error-reports?take=50` (remove before launch).
  A sign-in whose account is gone (deleted, disabled) is signed out by the Firebase adapter (`IsDeadSession`), never
  left calling the API without a token.
- **Version** (one place: `Directory.Build.props`): `AppVersionNumber` (SemVer, now `0.1.0`) + `AppVersionStage`
  (`rc`, empty for a final release) + CI's `BuildNumber` (the run number) make the label `0.1.0-rc.12` (`0.1.0-rc` on
  a PC). `Directory.Build.targets` stamps it with the git commit into the app and the API (`StampBuildInfo`); the app
  reads it as `Services/Diagnostics/BuildInfo`, the API as `ApiVersion`. It shows small and muted on the splash and
  beside the logo in the top bar (`AppBrand.Version`), goes into every error report (`0.1.0-rc.12 (12)`), on every API
  call (`X-App-Version`, logged by `ErrorDetailsMiddleware` with each crash and refusal) and on every API answer
  (`X-Api-Version`, logged by `ApiClient` with each failed call). To release a new version change
  `Directory.Build.props`, never a csproj; the build number only counts up.
- **Analytics and A/B tests** are ports with do-nothing defaults (skeleton `IAnalytics` / `NoAnalytics`,
  `IExperiments` / `NoExperiments`) behind `Services/Analytics/AppAnalytics`. The Firebase adapters are **build-time
  feature flags, off by default** (`FirebaseAnalytics`, `FirebaseExperiments` in `Directory.Build.props`, or
  `-p:FirebaseAnalytics=true`; CI repo variables `FIREBASE_ANALYTICS` / `FIREBASE_EXPERIMENTS`): off, the package isn't
  referenced and `AddFirebaseAnalytics()` / `AddFirebaseExperiments()` do nothing, so the APK builds with nothing set
  up in Firebase. Only screen routes with ids taken out (`AppAnalytics.ScreenName`), the events in `AppEvents` and the
  traits in `AppProperties` are sent: never a name, number, message or search words. Update the privacy policy before
  turning analytics on.
- **Geo** (`013-schema-geo.sql`, `GeoRules`): a listing has a private base point (`Professionals.Latitude` /
  `Longitude`, kept by the database as the geography column `Location`, SRID 4326, with a spatial index) and a travel
  distance `RadiusKm` (1–50, default 10). `PublishListingRequest` carries them, all optional: without them the server
  keeps what it has or starts at the middle of the listing's towns (`GeoRules.TownCentres`: a new town needs a line).
  Matching by distance is `ProfessionalStore.WorkersInRangeAsync` (`Location.STDistance(point) <= RadiusKm * 1000`).
  **The point never leaves the server**: not in `ListingDto`, only a town or a rounded distance may be shown. Town
  lists and town-based search stay until search moves to radius. **In the app** the point comes only from a tap on
  **Use my location** (`Components/Shared/LocationButton`, skeleton port `IDeviceLocation`, "while using the app",
  never background; rules in `Services/WorkArea`: a rough or foreign point is not used): the worker's **Where you
  work** page (`ProRoutes.WorkArea`: base + travel distance, `ProWorkspace.SetWorkAreaAsync`) and the post form.
- **Worker verification** (`014-schema-verification.sql`, `VerificationFunctions`, `VerificationStore`, contracts in
  `Verification.cs`): **three badges**: a government ID, a TESDA certificate or training, and a past project with a
  developer **or any other company**. A worker sends a photo with `POST verifications/{kind}?consent=true` (`id`,
  `tesda`, or `developer` + `badgeId`: a well-known developer from `DeveloperBadges`, or `other` + `companyName`
  typed by the worker; each company is its own badge); no consent, nothing stored. Proofs are sensitive personal
  information (RA 10173): they go to the **private** container `verification` (`IPrivateFiles`), never `photos`. An ID
  is read by Azure AI Document Intelligence (`IIdDocumentReader` → `DocumentIntelligenceIdReader`, prebuilt ID model,
  settings `DocumentIntelligenceEndpoint` / `DocumentIntelligenceKey`, free tier, `azure/cloudshell-document-intelligence.sh`;
  without them `NoIdDocumentReader`) only to help the operator: **a person always decides**, comparing the profile
  photo with the ID (no selfie, no face matching). Operators are the uids in the setting `AdminUids`
  (`admin/verifications…` routes, `403 not-admin` for anyone else); every look and decision goes into
  `VerificationAudit`; on the decision the file and everything read from it are deleted and only the result stays.
  An approved request is a `BadgeDto` in `ListingDto.Badges`. Nothing read from a document is ever sent to a phone or logged.
  **Badges count in search** as a bonus on top of the 100 (`SearchRanker.TrustFeature`: ID 4, TESDA 3, past project 3;
  a worker without badges scores as before). **In the app:** port `IVerificationRepository`
  (`ApiVerificationRepository`), `Services/ProBadges` (the rules: nothing leaves the phone without the consent tick;
  one request per badge), `BadgeCatalog` (developer ids = the server's, checked by a test), `BadgeText`; the pro's
  pages `ProRoutes.Badges` (`BadgesPage`, a `BadgeRow` per badge with its state) and `ProRoutes.BadgeProof(kind)`
  (`BadgeProofPage`: what to photograph, the company for a project, the unticked consent box, then the photo picker);
  Seekers see `BadgeChips` (app-generic, brand-purple outlined chips) on every `ProCard` and a row per badge in the
  profile's "What we checked". **The operator decides in the app:** an account in `AdminUids` (`GET admin/me` →
  `AdminStatusDto`) gets a "Check badge requests" row on either Me page (`Components/Admin/OperatorMenuRow`) to
  `AdminRoutes.Badges` (`BadgeReviewPage`, full screen): one `PendingBadgeCard` per request, oldest first (profile
  photo, badge, what the AI read, the proof photo only on a tap, Approve / Reject with a reason the worker sees);
  rules in `Services/BadgeReview`, port `IBadgeReviewRepository`. The privacy policy's §3.5 describes all of it:
  change them together.
- **Feedback board** (`Components/Feedback/`, `Services/FeedbackBoard`, `FeedbackStore`, tables `FeedbackPosts` /
  `FeedbackVotes`): from the Me page, both sides suggest features and vote; the board is anonymous and sorted most
  voted first. Before posting, the app asks the server for look-alikes (`FeedbackMatch`, shared meaningful words) and
  the person adds their vote to one or says theirs is different. One vote per person; the writer may remove their own
  only while nobody else voted; a 🚩 sends it to the operator's `Reports` queue; the operator sets `Status`
  (`open`/`planned`/`done`/`declined`/`hidden`) in SQL.
- **Job posts** (`Components/JobPosts/`, `Services/JobBoard`, `JobPostStore`, `015-schema-job-posts.sql`, tables
  `JobPosts` / `JobPostPhotos`): a Seeker's **Post** tab lists their posts and opens the form (trade, service, town,
  what is wrong, their own budget in pesos, up to 3 photos; `JobPostRules`); a worker's **Requests** tab is the feed
  (`GET job-posts/feed`): open posts of **their trade** within **their own `RadiusKm`** of their base point
  (`STDistance`; town match when a point is unknown), ordered **a service they offer first, then nearest, then
  newest**, never their own or a blocked person's. The feed needs a published listing (`HasListing`). **The
  household's point is private:** it is stored (the phone's, else the town's centre) and only a town and a distance
  in whole kilometres, never under 1, are sent (`JobPostStore.DistanceKm`). A 🚩 on a post goes to the operator's
  `Reports` queue (`job-post:{id}`); the operator hides it with `Status = 'hidden'`. Copy calls it "what you need
  done" / "Requests", never a job. **Offers** (`016-schema-job-offers.sql`, table `JobOffers`, inDrive style): under
  each card in the feed a worker taps "Take it for ₱budget" or offers another price with a short note
  (`Components/JobPosts/OfferBox`), changes it or takes it back, one offer per worker per post; the household's card
  says "See N offers" (`OffersPage`, `/posts/{id}/offers`: each worker's `ProCard`, price, distance, note, the picked
  one first, then cheapest); "Pick" asks once more, closes the post, sends the worker a first chat message and opens
  the chat (the Seeker still starts every chat). A worker sees their own offers under Requests → **My offers**
  (`GET offers/mine`, `MyOffersPage`: waiting, picked with Open chat, or not picked). Offers need the worker's published listing of that trade; a block
  either way hides them. **Phone numbers stay private until a pick:** `ListingDto.MobileNumber` is empty for everyone
  but the listing's owner (search, profiles, offers); the two sides talk in the app's chat, and Call shows on both
  chat pages only once a picked offer links them (`GET contacts/{otherUid}` → `ContactDto`,
  `JobPostStore.ContactNumberAsync`, `JobBoard.ContactNumberAsync`; null with no pick, a block, or no number).
- **App shell:** skeleton `AppShell` (top bar, optional banner, bottom tabs); each side's thin layout
  (`ProLayout`, `SeekerLayout`) loads its workspace and passes the tabs; routes live in `ProRoutes` / `SeekerRoutes`
  (link through them, never a literal path); full-screen steps use `InterviewStep` outside the shell.
- **Pro side:** publishing needs a complete listing (`IsListingComplete`) **and proof** (`HasProof`: profile photo,
  2–6 work photos, 1–3 private references; Seekers see only the count) and a verified number; the server checks
  the same (`ProfessionalStore.InvalidField`). **The interview's Done leads straight into the setup** (`Onboarding/ProSetup`, `/start/pro/setup/{1-3}`: a selfie (asks for the camera, opens the front camera by itself, moves on when saved; Android: `Platforms/Android/AndroidPhotoPicker`) → work photos → references, then Publish; rules `ProSetupStep` / `AfterInterviewPath` in `OnboardingSteps`), and launch resumes it until the listing is live; Pro home's `SetupCard` covers whatever is still missing after that. **The profile is a game with levels** (`Services/ProfileProgress`, tested): each part (own photo, work photos, references, number, badges) gets a `ProfileMark` (red `Missing` blocks publishing, green `Done` is enough, blue `Full` = 6 photos / 3 references / 3 badges, amber `Recommended` = a badge still worth asking for, gray `Waiting` while checked) and the profile a `ProfileLevel` (red until published, green live, blue `Complete` when every part is full); a red `NotLiveBanner` under the top bar links to the first missing part (`NextStep`), Me shows `ProfileLevelChip` under the name once live (before that only the banner says "not visible yet") and a `ProfileMarkIcon` on the photos, references and badges rows (skeleton `MenuRow.EndContent`), and `BadgeNudgeCard` (amber) sits on Me, and on Pro home once live, while a badge can still be asked for. **Edits to a live listing always reach the server:** Edit listing's Done, photo and reference changes mark it `ProfessionalDraft.ListingOutOfDate` and re-publish (`ProWorkspace.ListingEditedAsync` / `SyncListingAsync`, never throwing); a failure keeps the mark and the next Pro refresh retries.
- **Seeker side:** search → public profile `/pro/{id}` → chat with a ready first message (Send; Call only after a pick). Search
  words are parsed in the app (`SearchText`, `SearchIntentParser`, synonyms in `TradeWords`); ranking is on the server
  (`SearchRanker`, exact job only, "your town" then "nearby", a 0–100 explainable score; paid boosts are never part of
  the score; [`docs/search-ranking.md`](docs/search-ranking.md)).
- **Onboarding** ([`docs/onboarding.md`](docs/onboarding.md)): `/` = `LaunchRouter` → language → sign in (**phone
  number first for both sides**, then "or" Google, Apple on iOS; each SMS costs money, so another code to the
  same number waits `SmsSendLimit.Wait` (360 s) and at most 3 a day per phone; the sixth digit sends the code, and a number Android confirms by itself signs in without it) → role (a worker confirms their number
  right here if they signed in with Google/Apple, once in their life, or "Do this later" and Publish asks again (its own "Do this later" goes to Pro home, whose banner says the number is still needed to publish): the number stays on the sign-in account, so later Google sign-ins never ask again;
  Seekers are never asked for a number) →
  the interview, one URL per step, rules in `Services/OnboardingSteps` (tested). The Seeker's last question is **search or post** (`SeekerAnswers.FindBy`, `ReferenceData.FindByChoices`): the interview lands on the results or the post form (`FinishPath`), and Seeker home's `PostRequestCard` offers posting to everyone. After that a Seeker has no saved "preferences" (none on the Me page): Home's **recent searches** (`Services/RecentSearches`, last 3 searches and posts, only on the phone in `SeekerAnswers.RecentSearches`) each search again or open the post form filled in. Money is the hook: prices are per job
  (`Services/Pricing`, price guides in SQL, never under the minimum wage).
- **Auth** is provider-neutral: contract and screens in the skeleton (`IIdentityProvider`, `AuthError`,
  `SignInGate`), the vendor in `MobileAppTheme.Firebase` (`AddFirebaseAuth`, `AddFirebasePush`). Never call a provider
  SDK from a page, workspace or service. Adapters log the full exception and map it to an `AuthError`; the UI shows
  `AppText.Describe(error)`. Android needs `MainActivity.OnActivityResult` → `FirebaseAuthSetup.HandleActivityResult`
  and the SHA-1/SHA-256 in Firebase. **Sign in with Apple is iOS only** (`AuthMethods.Apple`, App Store 4.8 because
  Google is offered; `Platforms/iOS/Entitlements.plist`). Load the `maui-firebase-auth` skill.
- **Legal and store rules** ([`docs/legal/`](docs/legal/README.md)): the Privacy Policy, Terms and data page are
  `docs/legal/*.md`, served as public web pages by `LegalFunctions` (`GET legal/privacy|terms|data`, embedded at
  build; no `[placeholders]`) and linked from `AppBrand.Legal` (sign-in `LegalConsent` line, Me page). **Delete
  account** (Me, `IAppAccount.DeleteAccountAsync` → `DELETE users/me/account`: every row about the person incl.
  reviews about them, their photos, then the Firebase account via the Admin SDK `IAccountDeleter`) is separate from
  Start over. **Report and block** (`ReportPage`, 🚩 on profiles, reviews and chats; `SafetyStore`, tables `Reports`,
  `Blocks`): a block either way hides the two from each other in search, profiles, the Inbox and new messages;
  reports are answered within 5 business days. **Copy uses service words, never job/hire/employment or "minimum
  wage"** (Tarbaho is a directory, not an employer or recruiter), and never vouches for a worker ("What we checked").
  A reference needs the worker's consent tick.

### Anti-Fraud Rules

These are hard business rules — never bypass them:
1. **1 Phone = 1 GCash = 1 Registration** — unique constraint, no duplicates. The account whose token carries the SMS-verified number holds it: the sign-in provider gives a number to one account at a time, so on publish another account still showing it is stale and loses it (its listing comes down, its profile's number is cleared; `ProfessionalStore.PublishAsync`), never a refusal that leaves the person stuck (`409 phone-used-by-seeker` is retired); `Users.PhoneNumber` is always the token's verified number (`UserFunctions.Save` ignores the body's), so a profile can't claim someone else's
2. **Duplicate image detection** — SHA-256 or perceptual hash on portfolio uploads, reject duplicates. The upload refuses another account's bytes (`409 photo-in-use`); each table's unique index covers only itself, so `PhotoUse.UsedByAnotherAsync` also checks across `Users`, `Professionals` and `ProfessionalWorkPhotos` (publish → `PhotoInUse`; a profile save leaves the photo off)

## Target Solution Structure

```
SideGigPh.slnx
├── MobileAppTheme/            # Reusable app skeleton (Razor class library) — no SideGigPh code
│   ├── Auth/                  # IIdentityProvider, AuthUser/AuthResult/AuthError, IAuthLanding, IPhoneNumberFormat
│   ├── Branding/              # BrandStyle, MobileTheme.Create → MudTheme, AppBrand
│   ├── Components/Auth/       # AuthPage (/verify), NamePage, AddPhonePage, PhoneSignIn, GoogleSignInButton
│   ├── Components/Layout/     # MainLayout, AppShell, AppTopBar, AppBottomNav, AppTab
│   ├── Components/Shared/     # generic components (InterviewStep, ChoiceTiles, MePage…)
│   ├── Components/Pages/      # NotFound
│   ├── Localization/          # AppText, LocalizedText, AppLanguage, TimeAgo, CommonCopy
│   ├── Models/                # CatalogOption
│   ├── Services/              # ports: IAppAccount, IContactLauncher, IDeviceLocation, IPhotoPicker, IPhotoStore, IPushNotifications, IAnalytics, IExperiments; RemoteConfig<T>
│   │   └── Local/             # on-device adapters + JsonFile<T>
│   └── wwwroot/mobile-app-theme.css
│
├── Directory.Build.props      # THE version (AppVersionNumber, AppVersionStage) and the Firebase build flags
├── Directory.Build.targets    # StampBuildInfo: version + git commit into the app and the API
├── MobileAppTheme.Firebase/   # Firebase adapters (Android/iOS): AddFirebaseAuth (sign-in), AddFirebasePush (notifications), AddFirebaseAnalytics / AddFirebaseExperiments (build flags, off by default) — swap for another provider's project
│
├── SideGigPh.Contracts/       # JSON records shared by the app and the API (+ source-generated ApiJson)
│
├── SideGigPh.Api/             # Azure Functions (server): users, listings, reviews, chats (via AzureSQL) + photos (Blob Storage)
│
├── SideGigPh.AzureSQL/        # EF Core: Rows, SideGigPhDb, the stores (data rules)
│   └── Sql/                   # 001-schema.sql (first release, frozen), NNN-schema-*.sql (later changes, guarded), 004-config.sql (trades, jobs, answer lists), 002-seed.sql (sample data), 006-seed-search-ranking.sql (ranking test data)
│
├── SideGigPh.Migrate/         # Console tool: runs the Sql/ scripts the live database hasn't had (deploy-api.yml, on every deploy)
│
├── SideGigPh.Maui/            # The SideGigPh app (Android/iOS via BlazorWebView), references the skeleton
│   ├── Components/            # UI views — one folder per feature (+ Shared/ for SideGigPh-only generic parts)
│   │   └── Layout/AppTheme.cs # the Tarbaho brand (BrandStyle + AppBrand)
│   ├── Models/                # Data contracts only, no DTOs
│   ├── Services/              # SideGigPh ports, workspaces, search, Api/ adapters (all data + photos), Local/, auth hooks (SideGigPhAuthLanding, PhilippinePhoneNumberFormat)
│   └── MauiProgram.cs         # DI container — AddMobileAppTheme, then binds the app's adapters
│
└── SideGigPh.Tests/           # xUnit tests (README.md): compiles the app's Models/ + Services/ by link
    ├── Unit/                  # one feature per folder, fakes only (Onboarding, Catalog, Publishing, Search, Seeker, Chats, Reviews, Photos, Account, PhoneNumbers, Adapters, Diagnostics, Feedback, JobPosts, UpdateSafety, Performance)
    ├── Integration/           # real API functions + SQL Server + the app's adapters (Support/: ApiTestHost, TestDatabase, TestTokens, TestPhone)
    └── TestSupport/           # fakes for the ports, FakeApp, StubServer, test data
```

Business logic may later move out of `SideGigPh.Maui` into a `SideGigPh.Shared` library; the skeleton stays app-agnostic either way.

## Coding Standards

- **C# / .NET 10**, nullable enabled, implicit usings
- **Interfaces first** — define the port, then implement the adapter
- **No vendor lock-in** — Firebase, Azure, etc. must sit behind an interface
- **Mobile-first UI** — every component must work on phone-width viewports
- **Reusable, composable components** — build small, single-responsibility Razor components and compose them; never create one giant `.razor` page that does everything. Extract repeated or self-contained UI into its own component with `[Parameter]`-driven inputs and `EventCallback` outputs
- **Component folder structure** — group components by feature under `Components/<Feature>/`, and always use code-behind: each component is a `<Name>.razor` markup file paired with a `<Name>.razor.cs` partial class for its logic. Shared shell/layout components live under `Components/Layout/`
- **Skeleton vs app vs feature components** (load the `mobile-app-theme` skill to decide):
  - **Skeleton** (`MobileAppTheme/Components/Shared/`): generic UI any product would use unchanged, with no domain words (`InterviewStep`, `ChoiceTiles`, `PersonAvatar`, `SplashScreen`, `BusyOverlay` + `Busy.RunAsync` (the brand splash with lines from the `ISplashLines` port; the first line is the main one, shown first and held 3 s; Tarbaho's `RoleSplashLines` opens both sides on "AI-assisted ID checks", also the launch splash's line, then picks the rest by role), `SheetPage`, `MenuRow`, `HomeSection`, `FilterMenu`, `LoadMoreOnScroll`, `EmptyState`, `MePage`…). The skeleton never references SideGigPh.
  - **App-generic** (`SideGigPh.Maui/Components/Shared/`): used by two or more SideGigPh features but tied to its domain (`ProCard`, `TradeBadge`, `ListingFacts`, `ReviewRow`, `PhotoStrip`…).
  - **Feature** components stay in their feature folder.
  - The full inventory: [`docs/architecture.md`](docs/architecture.md#component-inventory) and [`MobileAppTheme/README.md`](MobileAppTheme/README.md).
- **MudBlazor only, no raw HTML** — markup is built from MudBlazor components; don't write `<div>`, `<span>`, `<p>`, `<button>`, `<input>` etc. in `.razor` files. Use `MudStack` for layout and spacing (`Row`, `Spacing`, `AlignItems`, `Justify`, `Wrap`), `MudText` for all text (`Typo` for size, `HtmlTag="h1"` for headings — `FocusOnNavigate` focuses the page's `h1`), `MudContainer` for page width, and MudBlazor's own spacing/flex utility classes (`pa-4`, `flex-1`, `mud-width-full`…). Reach for the out-of-the-box component first (`MudButton`, `MudTextField`, `MudChip`, `MudAlert`, `MudCard`, `MudSkeleton`, `MudProgressLinear`…). Only when MudBlazor has no parameter or utility for something, add a small, named helper class in the "MudBlazor gaps" section of `MobileAppTheme/wwwroot/mobile-app-theme.css` (generic) or `SideGigPh.Maui/wwwroot/app.css` (SideGigPh-only). Scoped `.razor.css` files don't reach inside MudBlazor components, so don't add them. The only raw HTML is `wwwroot/index.html` (the pre-Blazor splash).
- **One tap, one call** — a handler that calls the API, the sign-in provider or the camera starts with
  `if (_busy) return;` before setting `_busy = true` (a quick double-tap reaches it twice before the button re-renders
  as disabled), and catches its failure into a message: an exception escaping a tap handler breaks the screen. The
  server's `CallerLimits` is the backstop, not the plan.
- **Bilingual copy (English / Taglish)**: never hard-code user-facing text in a component.
  - UI strings are `LocalizedText(English, Taglish)` pairs in `Services/Localization/Copy.cs`. Strings every app shares (Back, Next, Cancel, Start over, Language, "3 hr ago", Not Found) are in the skeleton's `CommonCopy`; use those instead of duplicating them. Answer lists (jobs, towns, prices…) are in `Services/ReferenceData.cs`.
  - Components inject `AppText T` (skeleton) and render `@T.Get(...)` / `@T.Format(...)`.
  - Write for low English literacy: short plain words, an icon on every choice tile, and numbers (₱, years) doing the work.
  - Taglish is everyday conversational Taglish, not formal Filipino. Use local lingo, not forced translation:
    - Keep the English technical terms (split-type, chemical wash, freon, breaker, inverter, PMS, CCTV, waterproofing).
    - Use the local words people actually say (CR, gripo, lababo, yero, alulod, kisame, semento, poso).
    - Use Pa-/Paki- only where it sounds natural.
  - Job labels stand on their own ("Linis ng aircon"), so they also read correctly inside the pre-filled SMS.
- **Theming / colors** — SideGigPh's name, logo icon, colors and corner radius live in `SideGigPh.Maui/Components/Layout/AppTheme.cs` (`AppTheme.Style`, a `BrandStyle`, and `AppTheme.Brand`, an `AppBrand`). **The logo** is a crew of three workers in hard hats with a hammer and a wrench behind them (a team, not one person), one SVG path (`AppTheme.Logo`, the `AppBrand` logo icon drawn in `Color.Primary`); the same path is the launcher icon (`Resources/AppIcon/appiconfg.svg`, white on the purple `appicon.svg`), the native splash (`Resources/Splash/splash.svg`, purple on white) , the pre-Blazor splash in `wwwroot/index.html` and the Android notification icon (`Platforms/Android/Resources/drawable/ic_notification.xml`), so a new logo changes all five. The skeleton's `MobileTheme.Create` turns them into the `MudTheme` (palette mapping + typography). The look is **clean and minimal, Airbnb style**: **no blue except the verified check** (below), and a **60/30/10 palette**: ~60% white surfaces (white top bar with a hairline), ~30% near-black ink `#222222` (`Color.Dark`: text, icons, back buttons, own chat bubbles, avatars, unselected chips), ~10% the brand accent, deep purple `#5B2A86` (`Primary`, `Color.Primary`): logo, launcher icon, native splash and loading screen, filled main-action buttons (Next, Contact, Publish, Send, the FAB), the active tab, progress bars and spinners, selected tiles, checks and chips. Four fixed exceptions, never used for anything else: **stars are gold** `#FFB400` (`Color.Warning`, on every `MudRating` and the star fact); **money is dollar green** `#118C4F` (`Color.Success`: the rate fact, the "Pasok sa budget" chip, plus "published" and done checks); **a fully set-up worker profile is verified blue** `#1877F2` (`Color.Tertiary`, `BrandStyle.Tertiary`, like Meta's verified check: the `Verified` icon on the profile level and each full part); and **the badge nudge is amber** `#E8710A` (`Color.Info`, `BrandStyle.Info`: `BadgeNudgeCard` and the badges row's mark); plain notes use `Severity.Normal`, never `Severity.Info`; **10+ years of experience** (`ReferenceData.TopExperienceId`, `listing.IsSeasoned()`) shows in the brand purple with the `WorkspacePremium` award icon (`ProCardFact`/`StatTile` `Highlight`); `ProCardFact.IconColor` picks them; red (`Color.Error`) for Start over, soft gray lines, 8px buttons, 12px fields and tiles, flat (no shadows except `app-raised` cards), big bold `Typo.h4` page/question titles. **The font is DM Sans** (round, close to Airbnb's Cereal; `BrandStyle.FontFamily`, the file bundled in `wwwroot/fonts/` with its OFL licence and an `@font-face` in `app.css`, so it works offline). Interview steps and sign-in sheets center their content vertically when it is short, and each interview question has a picture (`InterviewStep.Illustration`, an image in a soft circle) above its centered title, which is a tenth smaller than a page title (`app-question`). **Pictures are the app's own images, never emoji**: one small SVG per trade, job, answer and interview question in `SideGigPh.Maui/wwwroot/images/icons/` (shipped in the app, so they show offline and look the same on every phone), all in one look: a 48-unit box, 2.5 ink outlines with round ends, the light brand tint `#E9DFF3` and one brand-purple detail (the colors are written in the files and must match `AppTheme`). `Services/AppImages` names them: `AppImages.For(id)` picks a trade's, job's or answer's picture by its stable id ("Others" and an id without a picture get the plus, `AppImages.More`), and the question pictures are its fields (`AppImages.Trade`, `Town`, `Price`…). `CatalogOption.Icon` / `ChoiceItem.Icon` / `InterviewStep.Illustration` hold the image path; the skeleton's `ChoiceTile` and `InterviewStep` and the app's `CategoryRail` draw it with `MudImage`. The `Icon` emoji in `004-config.sql` stays for app versions before the pictures and is not shown (`ApiMapping.ToModel`); a saved config that still has emoji is refused by `ReferenceData.CanUse`. A new trade or job added only in SQL shows the plus until it gets its own SVG in the same look plus a line in `AppImages` (`AppImagesTests` checks every built-in id has a picture and every picture is a file). Android draws dark status-bar icons on the white bar (`MainActivity.OnCreate`) and `MainPage` keeps the WebView out from under the status bar (`SafeAreaEdges="Container"`). Text fields use `Variant.Filled` + `app-field` (label inside the box; stack related ones in an `app-field-group`). `Secondary` is the muted gray, so `Color.Secondary` is the helper-text color; `Info` is the badge amber and `Tertiary` the verified blue (the skeleton's `BrandStyle.Info` / `Tertiary`, the brand color when an app leaves them unset). Components pick colors only through MudBlazor's `Color` parameters; any rule in `app.css` uses the theme's `var(--mud-palette-...)` variables. Never hard-code hex in components or CSS (the one exception: the pre-Blazor splash fallbacks in `app.css`, which must match `AppTheme`). To restyle the app, change `AppTheme`.
- **No dead code** — delete unused code, don't comment it out
- Keep business logic in `SideGigPh.Shared` (when it exists), platform-specific code in `SideGigPh.Maui`, and only app-agnostic frame/UI/adapters in `MobileAppTheme`
- Any change to the project structure, architecture, design patterns, theme or design must be reflected in this CLAUDE.md (the rule, short) and in [`docs/architecture.md`](docs/architecture.md) (the detail)

## Build & CI

- GitHub Actions workflow at `.github/workflows/build.yml` (manual run; pick the channel `beta`/`prod`): passes `-p:BuildNumber=<run number>` and `-p:AppChannel`, so run 12 builds `0.1.0-rc.12` (Android versionCode 12), names the APK artifact after it, and reads the Firebase flags from the repo variables
- `.github/workflows/tests.yml` runs `SideGigPh.Tests` on `ubuntu-latest` with a SQL Server 2022 service container (`SIDEGIGPH_TEST_SQL`), on pushes and pull requests to `main` that touch code the tests see (not CSS, images, `.razor` markup or docs), by hand, and as a required first job of `deploy-api.yml` (no deploy unless every test passes). It fails if any test was skipped and writes a coverage summary to the run page.
- `.github/workflows/deploy-api.yml` builds and deploys `SideGigPh.Api` to the Function App `SideGigPhApi` (Flex Consumption, Linux, Southeast Asia) on its own when a push to `main` changes `SideGigPh.Api/`, `SideGigPh.AzureSQL/` (including `Sql/`), `SideGigPh.Migrate/`, `SideGigPh.Contracts/`, `Directory.Build.props`/`.targets` or the workflow, and by hand (it passes `-p:BuildNumber=<run number>`, so the API answers with its own `X-Api-Version`) (Actions → Run workflow); a pending schema script runs on the live database first (the `db-migrate` job), then the API deploys; a script that fails stops the run before deploying. It signs in with OIDC as the user-assigned managed identity `sidegigph-github-deploy` (repo secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, made by `azure/cloudshell-github-deploy.sh`; trusts `main` only), so there is no publish profile; the app's own system-assigned identity is what reaches Azure SQL (setup: [`azure/README.md`](azure/README.md) → Automatic deploy from GitHub)
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

# Run the tests (from the repo folder; integration tests need SQL Server LocalDB or SIDEGIGPH_TEST_SQL)
dotnet test SideGigPh.Tests

# Only the fast unit tests
dotnet test SideGigPh.Tests --filter "Category!=Integration"

# Claude / CLI with Visual Studio open: run in a throwaway copy (skips when only CSS/markup/docs changed)
bash .claude/skills/run-tests/scripts/run-tests.sh --changed
```

## Project Skills (`.claude/skills/`)

Load the matching skill before doing these tasks. Every folder in `.claude/skills/` is listed here; add a line when you add a skill, and remove it when you delete one.

**Building features**
- **`mobile-app-theme`**: anything in `MobileAppTheme/`, deciding skeleton vs app for a new component/service/model/CSS helper, promoting a component into the skeleton, rebranding, or starting a new app on the skeleton. Script: `skeleton-check.sh` (no app dependency, no domain words, `CommonCopy` keys).
- **`new-razor-component`**: any new, split-out or restyled `.razor` component, page, tab or route (folders, code-behind, the `AppShell` layouts and routes). Script: `ui-lint.sh` (raw HTML, hex, literal text, page h1s).
- **`mudblazor-ui`**: MudBlazor 9 markup, verified parameter/enum/icon/utility names, gap classes, theme. Script: `mud-api.sh`.
- **`ports-and-adapters`**: new stored data or a new port method, end to end: SQL script (+ seed), EF row/mapping, store, contract + `ApiJson`, function, app adapter, port, DI, workspace.
- **`db-migration`**: any table, column, index or constraint change on the live database: a new guarded `NNN-schema-<what>.sql`, never an edit to `001-schema.sql` or a drop-and-create. Scripts: `new-migration.sh` (next-numbered file from the template), `migration-check.sh` (001 unchanged, every step guarded, recorded in `SchemaChanges`).
- **`add-interview-step`**: adding or changing an onboarding question, or the flow and resume logic.
- **`onboarding-ux-review`**: UX critique against the onboarding research, plus trust guardrails (permissions, hooks, sign-up placement).
- **`maui-firebase-auth`**: Firebase phone OTP / Google sign-in behind `IIdentityProvider`, `google-services.json`, SHA-1, status codes 7/10/12501.

**Build, run and fix**
- **`run-tests`**: after a significant change to code the tests cover (app Services/Models, API, AzureSQL incl. SQL scripts, Contracts, skeleton C#, csproj): run `SideGigPh.Tests` in a throwaway copy and fix failures at the root cause, never by weakening a test. Not for CSS/markup/docs-only changes. Scripts: `run-tests.sh` (`--changed`, `--unit`, `--coverage`), `coverage-summary.awk`, `uncovered-lines.awk`.
- **`build-check`**: compile-check the Android app and the API in a throwaway copy after code changes (safe with Visual Studio open). Never build or deploy in the repo folder from the CLI. Script: `build-check.sh` (`--app`, `--api`).
- **`maui-build-troubleshooting`**: cancelled/locked builds, and a Debug app that dies on launch with "No assemblies found … Fast Deployment". Script: `fix-fast-deploy.sh`.
- **`android-emulator-vs-deploy`**: emulator with no internet, network errors only on the emulator, VS stuck on "Waiting for emulator".

**Manual only**
- **`update-skills`**: capture lessons from a session into these skills. Run only when the user asks.
- **`update-tools`**: turn repeatable checks, fixes or queries from a session into helper scripts in `skills/*/scripts/`. Run only when the user asks.
