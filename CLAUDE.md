# SideGigPh — Tradesmen Open Directory (Philippines)

## What This Is

A high-density open local service directory (Carousell/Craigslist style) connecting urban households with skilled tradesmen and home-service pros. The categories are Aircon, Plumbing, Electrical, Roofing & Gutter, Carpentry, Welding, Masonry & Tiling, Errands/PasaBuy, Solar Panel Cleaning and Home Service Massage. They live in `ReferenceData.Categories` / `ServicesByCategory`. **Category and service IDs are stable; only labels change.** Each category's services are ordered most-requested first and end with an "Other / Iba pa" option. Neutral "Yellow Pages" model — direct consumer-to-tradesman contact, zero transaction commissions.

**Target region:** four areas (`ReferenceData.Areas`), each with its towns (`ReferenceData.CitiesByArea`; `Cities` is all of them in area order):
- **Metro Manila:** all 16 cities + Pateros.
- **Rizal:** Angono, Antipolo, Binangonan, Cainta, Rodriguez (Montalban), San Mateo, Taytay.
- **Laguna:** Biñan, Cabuyao, Calamba, Los Baños, San Pedro, Santa Rosa (Nuvali).
- **Cavite:** Bacoor, Carmona, Dasmariñas, General Trias, Imus, Kawit, Silang, Tagaytay.

**Town IDs are stable** (no accents: `las-pinas`, `santa-rosa`); only labels change. `ReferenceData.AreaOf(cityId)` gives a town's area. Both sides pick the **area (province) first**, then towns in it: a Skilled Worker works in **one area** and ticks any number of its towns; a Seeker picks **one home town**.

**Audience:** many Professionals (tradesmen) are not confident English readers. The app is **English / Taglish**, and the user picks one on the very first screen.

## Tech Stack

- **Frontend:** .NET MAUI Blazor Hybrid — shared Razor Class Library for Android and iOS
- **Target platforms:** Android + iOS only (Windows and Mac Catalyst are not supported)
- **UI:** Mobile-first. Component library: **MudBlazor** (9.x, version set once in `MobileAppTheme.csproj`). Providers (`MudThemeProvider`, `MudPopoverProvider`) sit in the skeleton's `MainLayout`; MudBlazor's and the skeleton's CSS/JS load from the app's `wwwroot/index.html`
- **App skeleton:** **`MobileAppTheme/`** is a reusable Razor class library (plain `net10.0` + `Microsoft.Maui.Essentials`) holding the shell, theme builder, generic components, localization and on-device adapters. `SideGigPh.Maui` references it; the skeleton never references the app, so the next MVP can start from it. See [`MobileAppTheme/README.md`](MobileAppTheme/README.md) and the `mobile-app-theme` skill.
- **Auth (MVP):** Firebase Authentication — Phone/SMS OTP + Google login
- **Database (MVP):** Cloud Firestore — users, provider listings, chats, reviews. Wired through `SideGigPh.Maui/Services/Firestore/` (see **Firestore adapter** under Key Ports). Plan: [`docs/data-model.md`](docs/data-model.md) (MVP collections, sign-in-first flow, SQL mapping, port changes); security rules, indexes, `setup.ps1` and the Cloud Shell scripts in [`firebase/`](firebase/README.md)
- **Photos (MVP):** Azure Blob Storage (storage account `sidegigphbfb9`, also the Function App's own storage; container `photos`, blob `{sha256}.jpg`, readable by URL), written only through **`SideGigPh.Api`** (Azure Functions, isolated worker, .NET 10, published to the Function App `SideGigPhApi`, https://sidegigphapi-hrb9bgerbgcgawbm.southeastasia-01.azurewebsites.net/api/). The API checks the user's Firebase ID token, hashes the bytes itself and refuses a photo another account already stored (anti-fraud rule 2). **The storage connection string lives only in the Function App's settings (`PhotoStorage`)** or, on a PC, the git-ignored `SideGigPh.Api/local.settings.json`; never in the phone app. Setup and checks: [`azure/`](azure/README.md). The app reaches it through **`ApiPhotoStore`** (`SideGigPh.Maui/Services/Api/`, registered in `MauiProgram` after `AddMobileAppTheme`, so it replaces the skeleton's `FilePhotoStore`): `POST photos` uploads, `DELETE photos/{id}` removes, each with the user's ID token (`IIdentityProvider.GetIdTokenAsync`). The server's returned id is the photo id; photos show from their public blob URL. The API URL and blob base URL are constants in `MauiProgram` (not secrets).
- **Future migration:** Azure Functions + PostgreSQL/PostGIS for spatial queries (`ST_DWithin`)

## Architecture — Ports & Adapters

All external infrastructure is decoupled behind C# interfaces (Dependency Inversion). This enables zero-downtime migration from Firebase to Azure/SQL.

### Key Ports (Interfaces)

**`IIdentityProvider`** (skeleton, `MobileAppTheme/Auth/`) — Auth abstraction; Firebase is one swappable adapter (see Auth → Switching the auth provider)
- `Methods` (which `AuthMethods` the provider supports)
- `SignUpWithPhoneAsync(string phoneNumber)` (sends the SMS code, for sign-in or linking)
- `VerifyPhoneOtpAsync(string verificationId, string userEnteredCode)`
- `LinkPhoneAsync(string verificationId, string userEnteredCode)` (adds a verified number to the signed-in account, same uid; `AuthError.PhoneInUse` if another account has it)
- `SignInWithGoogleAsync()`, `SetDisplayNameAsync(string)`
- `GetCurrentUserAsync()`, `GetIdTokenAsync()` (the ID token for our own API as a Bearer header), `SignOutAsync()`

**`ITradesmanRepository`** — Data + spatial search abstraction
- `AddProviderAsync(TradesmanRegistrationReq req)`
- `SearchProvidersAsync(GeoSearchFilter filter)`
- `GetProviderByIdAsync(string providerId)`
- `UpdatePayoutStatusAsync(string providerId, string status)`

> **Naming:** user-facing text (`Copy`, `ReferenceData`, brand tagline) always says **"Skilled Worker"** / "skilled worker" (Taglish too), never "pro", "Professional" or "tradesman". Code keeps **"Professional"** (`ProWorkspace`, `ProfessionalListing`, `/professional` routes) for the side-gig service provider (aircon tech, plumber — and also non-trade pros like web developers or video editors), never "tradesman". `ITradesmanRepository` / `TradesmanRegistrationReq` predate this rule; prefer the neutral form. `IProfessionalProfileRepository` holds profile lookups and `SearchAsync` (backed by `Dev/StubProfessionalProfileRepository` sample data until Firestore).

**`IOnboardingStore`**: the onboarding answers, saved on the device with no account (`Local/FileOnboardingStore`). It writes one JSON file in `FileSystem.AppDataDirectory`, using a source-generated serializer and an atomic write. There is no SQLite yet; add it when local data needs querying. Don't use the WebView's `localStorage`.

**`IContactLauncher`** (skeleton): Call / Text a pro through the phone's apps (`MobileAppTheme/Services/Local/DeviceContactLauncher`, MAUI `PhoneDialer` / `Sms`). Android needs the `tel` / `smsto` `<queries>` in `AndroidManifest.xml`.

**`IChatRepository`** (`Firestore/FirestoreChatRepository`; stand-in `Dev/StubChatRepository`): **in-app chat**, one `Chat` per Seeker and pro (id `{proId}_{seekerId}`, carrying both names, the job from the Seeker's answers and the `LastMessage`) with its `ChatMessage`s (`Text`, or `Call` when the Seeker's Call button opened the dialer). `GetInboxAsync(personId, ChatSide)` is each side's Inbox; unread = the other person wrote last after this side's read time (`MarkReadAsync`); `Watch(chatId, …)` gives live updates while a chat is open (the stub only sees this app's own sends; Firestore will push everyone's). **`IUserRepository`** (`Dev/StubUserRepository`, `dev-users.json`): the signed-in person's `UserProfile` (account name/number, language, side, private earnings goal), saved by `LocalAccount.SaveProfileAsync` at the role pick, the end of either interview and a language change. **`IReviewRepository`** (`Dev/StubReviewRepository`): Seekers write one review per pro they contacted (`SaveAsync` replaces); `GetSummariesAsync` feeds search ranking. The chat and review stubs persist to JSON files on the phone (`dev-chats.json`, `dev-chat-messages.json`, `dev-reviews.json`) and add SAMPLE rows only in Debug (`Dev/DevSamples`; sample chats have ids starting `sample-` and are never written to the file), so a Release build never shows invented activity.

**`JsonFile<T>`** (skeleton, `MobileAppTheme/Services/Local/`): the one place that reads/writes an app-private JSON file (atomic temp-file write, corrupt file reads as missing, source-generated `JsonTypeInfo`). `FileOnboardingStore` and the stubs use it.

**`LocalAccount`** (`Services/`, singleton, also registered as the skeleton's **`IAppAccount`** that `MePage` uses): the person on this phone, whichever side: app language (`SetLanguageAsync`, raises `Changed` for the layouts) and **Start over** (`StartOverAsync`): deletes their photos, published listing, chats, reviews and profile (`DeleteForPersonAsync` on the chat and review repositories, `IUserRepository.DeleteAsync`), clears `IOnboardingStore` (`ClearAsync`) and makes both workspaces `Forget()`, so `/` restarts onboarding at the language screen. With auth it becomes sign-out / delete account.

**Ids:** the signed-in person's uid is their id on both sides: `ProWorkspace.MyId` (also the listing id, `professionals/{uid}`) and `SeekerWorkspace.MyId`, both set when the workspace loads. Pages compare against those, never a literal. Only when no sign-in provider is registered do they fall back to the old fixed ids `ListingMapper.OwnListingId` (`"own"`) and `SeekerWorkspace.LocalSeekerId` (`"me"`).

**Firestore adapter** (`SideGigPh.Maui/Services/Firestore/`, package `Plugin.Firebase.Firestore` 3.1.3, the only code that touches the Firestore SDK): `FirestoreProfessionalProfileRepository` (`professionals/{uid}`; search = equality filters on `categoryId` and `areaId`), `FirestoreUserRepository` (`users/{uid}`), `FirestoreReviewRepository` (`reviews/{proId}_{seekerUid}`; stars averaged on the phone) and `FirestoreChatRepository` (`chats/{proId}_{seekerUid}` + `messages`; each message is one batch with the chat's new `lastMessage`; `Watch` is a snapshot listener). `FirestoreDocs.cs` holds the document classes, which match `firebase/firestore.rules` field for field (the rules refuse any other field); models in `Models/` never get Firestore types. `AddFirestoreData()` registers all four in `MauiProgram`; building with `-p:UseStubData=true` (defines `STUB_DATA`) registers the `Dev/` stubs instead, for running without a Firebase project. Reads that fail (offline, rules) are logged and come back empty; writes log and throw, and the pages already show a "didn't send / didn't publish" message. A listing keeps each photo as `{id, url}` taken from the photo store's URL; the photo bytes are not Firestore's business (Azure, above). Because Firebase Auth allows one account per phone number and the rules make `mobileNumber` the account's verified number, `PublishResult.PhoneTaken` never comes back from this adapter.

**`IPhotoPicker`** (skeleton `Local/DevicePhotoPicker`, MAUI `MediaPicker.PickPhotosAsync`, resized to 1280px) and **`IPhotoStore`** (skeleton default `Local/FilePhotoStore`; SideGigPh uses `Services/Api/ApiPhotoStore`, see Photos above): photo id = SHA-256 of the bytes (anti-fraud rule 2). `FilePhotoStore` keeps files in `AppDataDirectory/photos` shown as data URLs; `ApiPhotoStore` shows https blob URLs. `SaveAsync` throws `PhotoStoreException` when the upload fails (`ProWorkspace` turns it into the "photo failed" notice); `DeleteAsync` logs a failure and carries on. Gallery-only, no camera, so Android 13+ needs no permission (the manifest has `READ_EXTERNAL_STORAGE` `maxSdkVersion=32` for older phones; iOS has `NSPhotoLibraryUsageDescription`).

**`IProfessionalProfileRepository`**: `PublishAsync` returns `PublishResult.PhoneTaken` for a number that already has a listing (anti-fraud rule 1); `GetByIdAsync` serves the public profile and Inbox; `RemoveAsync` takes a listing out of search (Start over). The Firestore adapter keeps it in `professionals/{uid}`; the stub (`UseStubData`) keeps it in memory only, so it shows in this phone's Seeker search but is lost on restart.

### App shell (shared by both sides, after the interview)

- **`AppShell`** (skeleton, `MobileAppTheme/Components/Layout/`), Slack style: `AppTopBar` (brand name + logo from `AppBrand` on the left, the user's `PersonAvatar` on the right linking to their page, optional green status dot), the page in the middle, `AppBottomNav` icon tabs (`AppTab`: href, icon, label, badge, `MatchPrefix` to keep a tab lit on its sub-pages). Fixed parts use the `app-*` gap classes in the skeleton's `mobile-app-theme.css`; a page's main action sits last with `app-sticky-action` so it stays just above the tabs.
- The app wires the skeleton in three places: `MauiProgram` (`AddMobileAppTheme(AppTheme.Brand)`, then `IAppAccount`), `Components/Routes.razor` (skeleton `MainLayout` as default layout, its `NotFound` page, its assembly in `AdditionalAssemblies`) and `wwwroot/index.html` (`_content/MobileAppTheme/mobile-app-theme.css`). Skeleton namespaces are global (`GlobalUsings.cs` + `Components/_Imports.razor`).
- Each side has a thin layout that loads its workspace (non-matching users go back to `/`) and hands `AppShell` its tabs: **`Professional/ProLayout`** and **`Seeker/SeekerLayout`**. Pages opt in with `@layout`. Full-screen steps (`/professional/publish`, `/me/reviews/rate/{id}`) use `InterviewStep` instead, outside the shell.
- Routes live in `ProRoutes` / `SeekerRoutes`; link through them, never a literal path.

### Pro side (`Components/Professional/`)

- **`ProWorkspace`** (`Services/`, singleton) holds the pro's draft, photos, chats and reviews and the actions on them (add/remove photos, change own photo, publish, reply, mark a chat read); it raises `Changed` so the shell re-renders.
- Tabs: Home `/professional` (`SetupCard` checklist until published, collapsible `HomeSection`s, + button adds work photos), Inbox `/professional/inbox` (unread badge; each chat at `/professional/inbox/{chatId}`, `ProChatPage`: the customer, the job, what/where/when/budget, messages and a reply box), Reviews `/professional/reviews`, Me `/professional/me` (`ProMePage`, also the avatar: own photo, Edit listing, Publish, a link to work photos, card preview, then the shared `MePage` parts). Work photos live under Me at `/professional/me/photos` (up to 12), so the Me tab stays lit.
- Profile and work photos live on `ProfessionalDraft` (`ProfilePhotoId`, `WorkPhotoIds`, `PublishedAt`) and show on `ProCard` (avatar photo + the newest 3 work photos).

### Seeker side (`Components/Seeker/`, `Search/`, `Inbox/`)

- **`SeekerWorkspace`** (`Services/`, singleton): the current search (saved into `SeekerAnswers`, so it survives Back and restarts), Inbox threads (`InboxThread` = the pro's listing + the `Chat`), the Seeker's reviews, Send (in-app chat) and Call (opens the dialer via `IContactLauncher`, then logs a `Call` message). The pre-written first message comes from their answers (job, town, urgency).
- Tabs: Home `/seeker` (DoorDash/Grab style: dense pill search box, the town `FilterMenu` under it, `CategoryRail` of trade icons in two sideways-scrolling rows, then last search, recent messages, pros to rate), Inbox `/inbox` (unread badge; + chat `/inbox/{proId}`), Me `/me` (`SeekerMePage`, also the avatar: name shown on reviews, a Your reviews link, then the shared `MePage` parts; badge = contacted pros not yet rated). The Seeker side has 3 tabs; sub-pages sit under a tab's route so it stays lit: results at `/seeker/search` (from the home search box or a trade icon), reviews at `/me/reviews` (write at `/me/reviews/rate/{proId}`).
- **Flow:** search → tap a result → public profile `/pro/{id}` (facts, jobs, work photos, reviews, pinned **Contact**) → chat `/inbox/{id}` with a ready first message: **Send** posts it in the app (it starts the chat), **Call** opens the dialer and is logged in the chat. The pro answers in their own Inbox. Only pros the Seeker chatted with can be reviewed. Shared chat parts live in `Components/Chats/` (`ChatHeader` with the Seeker's **Call** button on the right, `ChatBubble` (theirs gray, mine ink), `ChatComposer` (pill box + round send), `ChatRow`); each side wraps `ChatRow` (`SeekerChatRow`, `ProChatRow`), and `ChatText` (`Services/Localization/`) words the job for the pro.
- **Search relevance (`Services/Search/`)** — keep it explainable, every weight is a named constant:
  - `SearchText`: normalises typed words (lower case, accents off, English/Tagalog filler words dropped) and scores word matches (exact 1, typed prefix 0.8, longer form 0.7).
  - `SearchIntentParser`: free text → trade + (when one clearly fits) job, matched against both languages' category/job labels, category hints and `TradeWords` synonyms (tubero, kuryente, hinang, masahe…). Generic job words (sira, repair, install…) count half toward the trade; a tie or weak match returns no trade plus the closest ones, and the UI asks "which kind of pro?". Add a synonym to `TradeWords` when users type a word we don't catch.
  - `SearchRanker`: only pros listing the exact job (falls back to the whole trade, flagged, when nobody does); grouped "goes to your town" then "other towns nearby" (= other towns in the Seeker's area; workers only in other areas are left out); scored by budget fit, stars (smoothed toward 4.0 so one review can't beat many), experience and photos.
  - `ProfessionalSearch` fetches the trade's pros from all towns plus their review summaries, then ranks.

### Onboarding & Initial Navigation (interview-first, auth after)

Full spec and research rationale: [`docs/onboarding.md`](docs/onboarding.md).

- **`/` = `Onboarding/LaunchRouter`** (no UI). It applies the saved language, then routes: `/start` (language) → sign in (the skeleton `SignInGate`: `/verify`, then `/verify/name` once after a phone sign-in) → `/start/role` (Seeker / Professional fork) → the saved interview step → `/seeker` (Seeker home) or `/professional` (Professional). Right after the Seeker interview the loader lands on `/seeker/search` (the payoff).
- **Interviews:** `/start/seeker/{step}` covers job → service → area (province) → home town (one, in that area; changing the area clears it) → when → budget, then the labor-illusion loader, then results. `/start/pro/{step}` covers trades → jobs → area (one of Metro Manila / Rizal / Laguna / Cavite; changing it clears the towns) → towns in that area (multi-select, "Select all") → experience → rate → monthly earnings goal → preview. Name and number are never asked: the listing takes the account's name and verified number (`ListingMapper.TakeAccount`). Town steps on both sides use `Onboarding/TownPicker` (the chosen area's towns as the usual stacked `ChoiceTiles` rows, a "Type your town" filter for long areas like Metro Manila); the Seeker home and Search town filters use the skeleton `FilterMenu` with area headings (`TownText.TownChoices`). There is one URL per step, so Android back walks the steps. Answers are saved as the user moves forward.
- **Money is the hook on both sides.** Seeker budget and pro rate share one price scale (`ReferenceData.PriceTiers`), which drives the "Pasok sa budget mo" badge and the sort order. The pro's private monthly goal becomes "≈ N jobs a week" (`GoalPayoff`, `Money.JobsPerWeek`).
- **Everyone signs in first** (owner decision), right after the language question: **phone number + SMS code** as the main option (it verifies the number), **Continue with Google** below it. A phone sign-in is asked its name once (`/verify/name`). A Google account brings its name but must **add a verified number** (`/verify/phone`, linked to the same account) before **Search results** (`/seeker/search`) or **Publish** (`/professional/publish`); those pages ask `SignInGate.RedirectAsync(..., requirePhone: true)`. The interview answers stay on the phone (`IOnboardingStore`).

### Auth

Auth is provider-neutral. The contract and screens live in the skeleton; the vendor is an adapter in its own project.

- **Contract (skeleton, `MobileAppTheme/Auth/`):** `IIdentityProvider` (phone code, Google, current user, sign-out, and `Methods`, the `AuthMethods` it supports) with `AuthUser`, `AuthResult` and `PhoneVerification`. A failure is an **`AuthError`** (`Cancelled`, `Network`, `InvalidCode`, `InvalidPhoneNumber`, `PhoneInUse`, `TooManyAttempts`, `Misconfigured`, `NotAvailable`, `Unknown`) plus the provider's raw `Detail` for logs. The UI shows `AppText.Describe(error)` (`CommonCopy`, English/Taglish), never provider text.
- **Screens (skeleton, `MobileAppTheme/Components/Auth/`):** `AuthPage` (route **`/verify`**: brand + tagline, then only the methods the provider offers, phone first), `NamePage` (**`/verify/name`**, once when the account has no name), `AddPhonePage` (**`/verify/phone?then=…`**, links a verified number to the signed-in account), `PhoneSignIn` (`LinkToCurrentUser` for linking) and `GoogleSignInButton`. **`SignInGate`** (`MobileAppTheme/Auth/`) is what pages ask: it returns the missing step (sign in, name, phone) or null, and lets everyone through when no provider is registered. Routes are in `AuthRoutes`.
- **App hooks:** `IAuthLanding` says where to go after sign-in. SideGigPh's `Services/SideGigPhAuthLanding` sends a user with a Professional profile to `/professional` and anyone else to `/`, where the launch router resumes onboarding. `IPhoneNumberFormat` reads typed numbers into E.164; SideGigPh's `PhilippinePhoneNumberFormat` accepts 0917… / 917… / +63 917… and sets the optional `Region` ("Philippines (+63)"), shown read-only above the number like Airbnb's Country/Region box. Skeleton defaults (`TryAdd`): `NoIdentityProvider` (no sign-in, `Methods = None`), `HomeAuthLanding` (`/`) and `E164PhoneNumberFormat`.
- **Adapter: `MobileAppTheme.Firebase/`** (Android/iOS MAUI library, `Microsoft.NET.Sdk`, not Razor, so no WebView asset clash). `FirebaseIdentityProvider`, `FirebaseAuthErrors` (status-code decoding) and `FirebaseAuthSetup`: `builder.AddFirebaseAuth(GoogleWebClientId)` in `MauiProgram` initializes the SDK in the platform lifecycle (wrapped, so a missing config doesn't crash launch) and **replaces** the skeleton's `IIdentityProvider`. `GoogleWebClientId` (the web OAuth client, client_type 3, from `google-services.json`) is a constant in `MauiProgram`.
- **Auth error policy:** an adapter never reduces an auth-infra exception to a bare message. It logs the full exception (type + stack) to Logcat/Output and returns an `AuthResult` with a mapped `AuthError`. For Google sign-in, `FirebaseAuthErrors.FromGoogle` parses the Google Play status code out of the exception message (which is just `"7:"`, `"10: …"`), with no dependency on the Play-services binding: `7` → `Network`, `10` DEVELOPER_ERROR / `12500` → `Misconfigured`, `12501` → `Cancelled`. A wrong SMS code (`FirebaseAuthException` `InvalidCredential`) → `InvalidCode`. `SignUpWithPhoneAsync` logs then rethrows (fail fast).
- **Packages (in `MobileAppTheme.Firebase`, `PrivateAssets="none"` so their build targets reach the app and process `google-services.json`):** `Plugin.Firebase.Auth` 3.1.1 + `Plugin.Firebase.Auth.Google` 3.1.2 (the **modular** Auth packages — NOT the bundled `Plugin.Firebase`, which drags in Crashlytics and crashes at launch in MAUI). Android **requires** `MainActivity.OnActivityResult` to call `FirebaseAuthSetup.HandleActivityResult(...)`; without it `SignInWithGoogleAsync()` hangs forever (no exception, stuck on "connecting"). `HandleActivityResult` forwards only while a Google sign-in is pending (`GoogleSignInPending`): the photo picker's and dialer's results reach `OnActivityResult` too, and the plugin's handler throws a `NullReferenceException` (shown as `Android.Runtime.JavaProxyThrowable`) that kills the app when nothing is waiting. The app's debug/release **SHA-1 (and SHA-256 for phone auth) must be registered in the Firebase console** and `google-services.json` re-downloaded — otherwise Google sign-in returns `DEVELOPER_ERROR` (status 10) and phone verification fails app-attestation.
- **Android signing ([`docs/android-signing.md`](docs/android-signing.md)):** every build on every PC and in CI is signed with the committed shared dev key `Platforms/Android/Signing/dev.keystore` (alias `androiddebugkey`, password `android`; SHA-1 `A5:F0:C9:…:31:B6` registered in Firebase), so sign-in works everywhere without registering per-machine keys. A Release build switches to the store key only when `ReleaseKeystore` (or env `SIDEGIGPH_KEYSTORE`) points at an existing file. The passwords come from env `SIDEGIGPH_KEYSTORE_PASS` / `SIDEGIGPH_KEY_PASS` and are never in the csproj. Other keystores are git-ignored. CI writes `google-services.json` and the optional store key from secrets (`GOOGLE_SERVICES_JSON_BASE64`, `ANDROID_KEYSTORE_BASE64`, …). Google Play's App signing key needs its own fingerprints in Firebase.
- **Firebase config (not committed; required before an Android/iOS run):** `Platforms/Android/google-services.json`, `Platforms/iOS/GoogleService-Info.plist`, plus a Google OAuth client (Android SHA-1; iOS reversed-client-id URL scheme). The app's csproj references the config files with `Condition="Exists(...)"` so the build works without them.

#### Switching the auth provider

Nothing outside the adapter project and `MauiProgram` knows the provider is Firebase. To move to another one (Supabase, Azure AD B2C, Auth0, a custom backend…):

1. Create `MobileAppTheme.<Provider>/` next to `MobileAppTheme.Firebase/`: a class implementing `IIdentityProvider` with its `Methods`, mapping every failure to an `AuthError` (log the full exception first), and a `builder.Add<Provider>Auth(...)` extension that does its setup and `Replace`s `IIdentityProvider`. Load the `maui-firebase-auth` skill for the error policy and the `mobile-app-theme` skill for the boundary rules.
2. In `SideGigPh.Maui.csproj`, swap the `MobileAppTheme.Firebase` project reference for the new one; in `MauiProgram`, swap `AddFirebaseAuth(...)` for the new call; in `MainActivity`, remove the `FirebaseAuthSetup.HandleActivityResult` line (or replace it with what the new SDK needs).
3. Remove the Firebase config files and `GoogleWebClientId` if nothing else uses Firebase.
4. Users are keyed by `AuthUser.Uid`; the new provider's ids differ, so existing accounts need a migration (once there are real accounts).

Never call a provider SDK from a page, workspace or service. If the UI needs something the contract lacks (e.g. email sign-in), add it to `IIdentityProvider` / `AuthMethods` / `AuthError` in the skeleton first, then implement it in every adapter.

### Anti-Fraud Rules

These are hard business rules — never bypass them:
1. **1 Phone = 1 GCash = 1 Registration** — unique constraint, no duplicates
2. **Duplicate image detection** — SHA-256 or perceptual hash on portfolio uploads, reject duplicates

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
│   ├── Services/              # ports: IAppAccount, IContactLauncher, IPhotoPicker, IPhotoStore
│   │   └── Local/             # on-device adapters + JsonFile<T>
│   └── wwwroot/mobile-app-theme.css
│
├── MobileAppTheme.Firebase/   # Firebase auth adapter (Android/iOS): AddFirebaseAuth — swap for another provider's project
│
├── SideGigPh.Api/             # Azure Functions (server): photo upload/delete to Blob Storage; later the SQL API
│
└── SideGigPh.Maui/            # The SideGigPh app (Android/iOS via BlazorWebView), references the skeleton
    ├── Components/            # UI views — one folder per feature (+ Shared/ for SideGigPh-only generic parts)
    │   └── Layout/AppTheme.cs # SideGigPh's brand (BrandStyle + AppBrand)
    ├── Models/                # Data contracts only, no DTOs
    ├── Services/              # SideGigPh ports, workspaces, search, Firestore/ adapter, Api/ (photos), Dev/ stubs, auth hooks (SideGigPhAuthLanding, PhilippinePhoneNumberFormat)
    └── MauiProgram.cs         # DI container — AddMobileAppTheme, then binds the app's adapters
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
  - **Skeleton** (`MobileAppTheme/Components/Shared/`): generic UI any product would use unchanged, with no domain words: `InterviewStep`, `ChoiceTiles` (+ `ChoiceTile`, `ChoiceItem`), `WorkingLoader`, `PersonAvatar` (photo, initials on ink or a person icon), `SheetHeader` (✕/← + centered title + hairline) and `SheetPage` (full-screen sheet with that header: sign-in, name, add phone), `MenuRow` (settings-list row with chevron), `ProfileCard` (raised card: big photo, name, status), `HomeSection` (collapsible home section), `FilterMenu` (a chip with a dropdown that scrolls and shows `ChoiceItem.Group` headings: Search filters and the home town picker), `BrandMark` (logo + name from `AppBrand`), `LanguagePicker`, `StartOverCard` (confirm-twice Start over, optional `Warning`) and `MePage` (the Me page frame: title, the side's own sections, then `LanguagePicker`, the on-this-phone note and `StartOverCard`; SideGigPh passes its own `StartOverWarning`). The skeleton never references SideGigPh.
  - **App-generic** (`SideGigPh.Maui/Components/Shared/`): used by two or more SideGigPh features but tied to its domain: `ProCard` (+ `ProCardFact`; one card for search results *and* the pro's own preview), `ListingFacts` and `ServiceChips` (shared by the card and the public profile; their text comes from `Services/Localization/ListingText`), `ReviewRow` + `RatingSummary`, `GoalPayoff`.
  - **Feature** components stay in their feature folder.
- **MudBlazor only, no raw HTML** — markup is built from MudBlazor components; don't write `<div>`, `<span>`, `<p>`, `<button>`, `<input>` etc. in `.razor` files. Use `MudStack` for layout and spacing (`Row`, `Spacing`, `AlignItems`, `Justify`, `Wrap`), `MudText` for all text (`Typo` for size, `HtmlTag="h1"` for headings — `FocusOnNavigate` focuses the page's `h1`), `MudContainer` for page width, and MudBlazor's own spacing/flex utility classes (`pa-4`, `flex-1`, `mud-width-full`…). Reach for the out-of-the-box component first (`MudButton`, `MudTextField`, `MudChip`, `MudAlert`, `MudCard`, `MudSkeleton`, `MudProgressLinear`…). Only when MudBlazor has no parameter or utility for something, add a small, named helper class in the "MudBlazor gaps" section of `MobileAppTheme/wwwroot/mobile-app-theme.css` (generic) or `SideGigPh.Maui/wwwroot/app.css` (SideGigPh-only). Scoped `.razor.css` files don't reach inside MudBlazor components, so don't add them. The only raw HTML is `wwwroot/index.html` (the pre-Blazor splash).
- **Bilingual copy (English / Taglish)**: never hard-code user-facing text in a component.
  - UI strings are `LocalizedText(English, Taglish)` pairs in `Services/Localization/Copy.cs`. Strings every app shares (Back, Next, Cancel, Start over, Language, "3 hr ago", Not Found) are in the skeleton's `CommonCopy`; use those instead of duplicating them. Answer lists (jobs, towns, prices…) are in `Services/ReferenceData.cs`.
  - Components inject `AppText T` (skeleton) and render `@T.Get(...)` / `@T.Format(...)`.
  - Write for low English literacy: short plain words, an icon on every choice tile, and numbers (₱, years) doing the work.
  - Taglish is everyday conversational Taglish, not formal Filipino. Use local lingo, not forced translation:
    - Keep the English technical terms (split-type, chemical wash, freon, breaker, inverter, PMS, CCTV, waterproofing).
    - Use the local words people actually say (CR, gripo, lababo, yero, alulod, kisame, semento, poso).
    - Use Pa-/Paki- only where it sounds natural.
  - Job labels stand on their own ("Linis ng aircon"), so they also read correctly inside the pre-filled SMS.
- **Theming / colors** — SideGigPh's name, logo icon, colors and corner radius live in `SideGigPh.Maui/Components/Layout/AppTheme.cs` (`AppTheme.Style`, a `BrandStyle`, and `AppTheme.Brand`, an `AppBrand`). The skeleton's `MobileTheme.Create` turns them into the `MudTheme` (palette mapping + typography). The look is **clean and minimal, Airbnb style**: **no blue anywhere**: the brand color (`Primary`) is the same near-black ink `#222222`, so the logo, launcher icon, native splash and loading screen are black on white, with a white top bar with a hairline, near-black ink `#222222` (`Color.Dark`) for everything else that stands out: buttons, selected tiles and tabs, chips, icons, own chat bubbles, avatars; red (`Color.Error`) for Start over, soft gray lines, 8px buttons, 12px fields and tiles, flat (no shadows except `app-raised` cards), big bold `Typo.h4` page/question titles. Interview steps and sign-in sheets center their content vertically when it is short, and each interview question has an emoji picture (`InterviewStep.Illustration`) above its centered title. Android draws dark status-bar icons on the white bar (`MainActivity.OnCreate`) and `MainPage` keeps the WebView out from under the status bar (`SafeAreaEdges="Container"`). Text fields use `Variant.Filled` + `app-field` (label inside the box; stack related ones in an `app-field-group`). `Secondary` is the muted gray, so `Color.Secondary` is the helper-text color; `Info` is the brand ink. Components pick colors only through MudBlazor's `Color` parameters; any rule in `app.css` uses the theme's `var(--mud-palette-...)` variables. Never hard-code hex in components or CSS (the one exception: the pre-Blazor splash fallbacks in `app.css`, which must match `AppTheme`). To restyle the app, change `AppTheme`.
- **No dead code** — delete unused code, don't comment it out
- Keep business logic in `SideGigPh.Shared` (when it exists), platform-specific code in `SideGigPh.Maui`, and only app-agnostic frame/UI/adapters in `MobileAppTheme`
- Any change to the project structure , architecture, design patterns, theme, design should reflect in this CLAUDE.md

## Build & CI

- GitHub Actions workflow at `.github/workflows/build.yml` (manual run)
- `.github/workflows/deploy-api.yml` builds and deploys `SideGigPh.Api` to the Function App `SideGigPhApi` (Flex Consumption, Linux, Southeast Asia) when run by hand (Actions → Run workflow). It uses the publish profile in the repo secret `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` (setup: [`azure/README.md`](azure/README.md) → Automatic deploy from GitHub)
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
- **`mobile-app-theme`**: anything in `MobileAppTheme/`, deciding skeleton vs app for a new component/service/model/CSS helper, promoting a component into the skeleton, rebranding, or starting a new app on the skeleton. Script: `skeleton-check.sh` (no app dependency, no domain words, `CommonCopy` keys).
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
