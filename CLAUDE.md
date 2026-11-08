# SideGigPh — Tradesmen Open Directory (Philippines)

## What This Is

A high-density open local service directory (Carousell/Craigslist style) connecting urban households with skilled tradesmen (Aircon, Plumbing, Electrical, Carpentry). Neutral "Yellow Pages" model — direct consumer-to-tradesman contact, zero transaction commissions.

**Target region:** Taytay, Pasig, Antipolo, Rizal (Philippines).

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

> **Naming:** new and user-facing code uses **"Professional"** for the side-gig service provider (aircon tech, plumber — and also non-trade pros like web developers or video editors), never "tradesman". `ITradesmanRepository` / `TradesmanRegistrationReq` predate this rule; prefer the neutral form. Auth added `IProfessionalProfileRepository` for profile lookups.

### Auth & Initial Navigation

- **Initial page** `AuthPage` (route `/`, `Components/Auth/`) — **Phone OTP + Google** sign-in via `IIdentityProvider`.
- **Adapter:** native Firebase SDK (Plugin.Firebase) in `Services/Firebase/` (`#if ANDROID || IOS`). Firebase init and `GetCurrentUserAsync` are wrapped so a missing config doesn't crash launch — the app loads and sign-in fails gracefully.
- **Post-auth routing** (`AuthPage.razor.cs` → `IProfessionalProfileRepository.HasProfileAsync`): profile exists → `/professional` (`ProfessionalHome`, intentionally empty for now); otherwise → `/onboarding` (stub). The seeker/customer path (sign up → search) and role selection are deferred.
- **Packages (Android/iOS only):** `Plugin.Firebase.Auth` 3.1.1 + `Plugin.Firebase.Auth.Google` 3.1.2 (the **modular** Auth packages — NOT the bundled `Plugin.Firebase`, which drags in Crashlytics and crashes at launch in MAUI). Google sign-in uses `CrossFirebaseAuthGoogle.Current` and needs `FirebaseAuthGoogleImplementation.Initialize(webClientId)` at startup — set `GoogleWebClientId` in `Services/Firebase/FirebaseRegistration.cs` (currently a `TODO` placeholder).
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
- **Generic vs feature components** — a component used by two or more features and not tied to any single feature is a *generic* component and belongs in `SideGigPh.Shared` (e.g. `SideGigPh.Shared/Components/`), not under a feature folder. Feature-specific components stay with their feature.
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
