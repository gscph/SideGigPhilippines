# SideGigPh — Tradesmen Open Directory (Philippines)

## What This Is

A high-density open local service directory (Carousell/Craigslist style) connecting urban households with skilled tradesmen and home-service pros. The categories are Aircon, Plumbing, Electrical, Roofing & Gutter, Carpentry, Welding, Masonry & Tiling, Errands/PasaBuy, Solar Panel Cleaning and Home Service Massage. They live in `ReferenceData.Categories` / `ServicesByCategory`. **Category and service IDs are stable; only labels change.** Each category's services are ordered most-requested first and end with an "Other / Iba pa" option. Neutral "Yellow Pages" model — direct consumer-to-tradesman contact, zero transaction commissions.

**Target region (supported towns, Rizal):** Angono, Antipolo, Binangonan, Taytay. The list lives in `ReferenceData.Cities`.

**Audience:** many Professionals (tradesmen) are not confident English readers. The app is **English / Taglish**, and the user picks one on the very first screen.

## Tech Stack

- **Frontend:** .NET MAUI Blazor Hybrid — shared Razor Class Library for Android and iOS
- **Target platforms:** Android + iOS only (Windows and Mac Catalyst are not supported)
- **UI:** Mobile-first. Component library TBD
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

### Onboarding & Initial Navigation (interview-first, auth after)

Full spec and research rationale: [`docs/onboarding.md`](docs/onboarding.md).

- **`/` = `Onboarding/LaunchRouter`** (no UI). It applies the saved language, then routes: `/start` (language) → `/start/role` (Seeker / Professional fork) → the saved interview step → `/search` (Seeker) or `/professional` (Professional).
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
- **Generic vs feature components** — a component used by two or more features and not tied to any single feature is a *generic* component and belongs in `SideGigPh.Shared` (e.g. `SideGigPh.Shared/Components/`), not under a feature folder. Feature-specific components stay with their feature. Until that project exists, generic components live in `SideGigPh.Maui/Components/Shared/`: `InterviewStep`, `ChoiceTiles`, `WorkingLoader`, `ProCard` (one card for search results *and* the pro's own preview), and `GoalPayoff`.
- **Bilingual copy (English / Taglish)**: never hard-code user-facing text in a component.
  - UI strings are `LocalizedText(English, Taglish)` pairs in `Services/Localization/Copy.cs`. Answer lists (jobs, towns, prices…) are in `Services/ReferenceData.cs`.
  - Components inject `AppText T` and render `@T.Get(...)` / `@T.Format(...)`.
  - Write for low English literacy: short plain words, an icon on every choice tile, and numbers (₱, years) doing the work.
  - Taglish is everyday conversational Taglish, not formal Filipino. Use local lingo, not forced translation:
    - Keep the English technical terms (split-type, chemical wash, freon, breaker, inverter, PMS, CCTV, waterproofing).
    - Use the local words people actually say (CR, gripo, lababo, yero, alulod, kisame, semento, poso).
    - Use Pa-/Paki- only where it sounds natural.
  - Job labels stand on their own ("Linis ng aircon"), so they also read correctly inside the pre-filled SMS.
- **Theming / colors** — the color palette lives as CSS custom properties (design tokens) in `SideGigPh.Maui/wwwroot/app.css` (`:root`). The brand palette is **blue/white** (GCash/Microsoft style): primary blue `--color-primary` (#0078d4), white surfaces, navy ink text. Every component MUST consume these tokens (`var(--color-...)`); never hard-code hex colors in components or scoped CSS. To restyle the app, change the tokens in one place.
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
