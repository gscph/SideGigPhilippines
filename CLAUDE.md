# SideGigPh — Tradesmen Open Directory (Philippines)

## What This Is

A high-density open local service directory (Carousell/Craigslist style) connecting urban households with skilled tradesmen (Aircon, Plumbing, Electrical, Carpentry). Neutral "Yellow Pages" model — direct consumer-to-tradesman contact, zero transaction commissions.

**Target region:** Taytay, Pasig, Antipolo, Rizal (Philippines).

## Tech Stack

- **Frontend:** .NET MAUI Blazor Hybrid — shared Razor Class Library for Android and iOS
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

### Anti-Fraud Rules

These are hard business rules — never bypass them:
1. **1 Phone = 1 GCash = 1 Registration** — unique constraint, no duplicates
2. **Duplicate image detection** — SHA-256 or perceptual hash on portfolio uploads, reject duplicates

## Target Solution Structure

```
SideGigPh.slnx
├── SideGigPh.Shared/          # Razor Class Library (95% of UI + business logic)
│   ├── Components/            # UI views (Search, Registration, Profiles)
│   ├── Models/                # Data contracts, DTOs
│   └── Services/              # Interfaces (IIdentityProvider, ITradesmanRepository)
│
├── SideGigPh.Maui/            # Native app shell (Android/iOS via BlazorWebView)
│   └── MauiProgram.cs         # DI container — binds adapters
```

The current codebase is a single `SideGigPh` project. It will be restructured into the above layout.

## Coding Standards

- **C# / .NET 10**, nullable enabled, implicit usings
- **Interfaces first** — define the port, then implement the adapter
- **No vendor lock-in** — Firebase, Azure, etc. must sit behind an interface
- **Mobile-first UI** — every component must work on phone-width viewports
- **No dead code** — delete unused code, don't comment it out
- Keep business logic in `SideGigPh.Shared`, platform-specific code in `SideGigPh.Maui`
- Any change to the project structure should reflect in this CLAUDE.md

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

# Build Windows
dotnet build -f net10.0-windows10.0.19041.0 -c Release

# Restore workloads
dotnet workload install maui
```
