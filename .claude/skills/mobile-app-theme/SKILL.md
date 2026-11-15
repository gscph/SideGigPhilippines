---
name: mobile-app-theme
description: The MobileAppTheme skeleton, the reusable app template (shell, bottom tabs, top bar, theme builder, generic components, English/Taglish localization, on-device adapters) that SideGigPh.Maui and future mobile MVPs reference. Use BEFORE adding or changing anything in MobileAppTheme/, before creating a component, service, model or CSS helper (to decide skeleton vs app), when promoting an app component into the skeleton, when changing the brand/theme, or when starting a new app on the skeleton. Includes skeleton-check.sh, which fails if the skeleton depends on an app or contains domain code.
---

# MobileAppTheme: the reusable skeleton

`MobileAppTheme/` (a plain `net10.0` Razor class library) is to our mobile apps what an admin-dashboard template is to web apps: the
frame is done, each product only adds its brand and its features. `SideGigPh.Maui` is the first
app on it. **The skeleton never knows which app it is in.**

```
App (SideGigPh.Maui) ──references──▶ MobileAppTheme ──▶ MudBlazor, MAUI, Components.Web
        ▲                                   │
        └──── never (no project ref, no namespace, no domain word) ◀┘
```

## 1. Skeleton or app? Decide before writing code

Ask: **"Would a different product (a booking app, a marketplace, a habit tracker) use this
unchanged?"**

| It is… | Goes in | Examples |
|---|---|---|
| App frame, navigation, theme mechanism | `MobileAppTheme/Components/Layout/`, `Branding/` | `AppShell`, `AppTopBar`, `AppBottomNav`, `AppTab`, `MainLayout`, `MobileTheme`, `BrandStyle`, `AppBrand` |
| UI with no domain nouns, data in via `[Parameter]` | `MobileAppTheme/Components/Shared/` | `InterviewStep`, `ChoiceTiles`, `PersonAvatar`, `HomeSection`, `FilterMenu`, `BrandMark`, `MePage`, `ConfirmActionCard`, `LanguagePicker` |
| Localization mechanism + strings every app needs | `MobileAppTheme/Localization/` | `AppText`, `LocalizedText`, `AppLanguage`, `TimeAgo`, `CommonCopy` (Back, Next, Cancel, Start over, "3 hr ago") |
| A port for a phone capability + its on-device adapter | `MobileAppTheme/Services/` + `Services/Local/` | `IContactLauncher`, `IPhotoPicker`, `IPhotoStore`, `JsonFile<T>`, `IAppAccount` (port only) |
| Sign-in contract + screens (provider-neutral) | `MobileAppTheme/Auth/`, `Components/Auth/` | `IIdentityProvider`, `AuthError`, `IAuthLanding`, `IPhoneNumberFormat`, `AuthPage`, `PhoneSignIn`, `GoogleSignInButton` |
| A cloud vendor's adapter for a skeleton port | Its own sibling project `MobileAppTheme.<Vendor>/` | `MobileAppTheme.Firebase` (`AddFirebaseAuth`) |
| CSS helper any app would use | `MobileAppTheme/wwwroot/mobile-app-theme.css` | `full-screen`, `sticky-footer`, `one-line`, `app-fab`, `app-sticky-action` |
| **Anything that names the product's domain** | **The app** | `ProCard`, `ReviewRow`, `ListingFacts`, `ProWorkspace`, `ReferenceData`, `Copy`, `ProRoutes`, search, `IOnboardingStore`, the app's `IAuthLanding` / `IPhoneNumberFormat`, app CSS (`category-rail`) |
| Brand values (name, logo, colors) | **The app** (`Components/Layout/AppTheme.cs`) | `AppTheme.Style`, `AppTheme.Brand` |

When in doubt, **start in the app** and promote later (section 4). Moving something out of the
skeleton later breaks every app on it; moving something in is cheap.

## 2. Hard rules for code inside `MobileAppTheme/`

These are checked by `scripts/skeleton-check.sh`. Never bypass them.

1. **No app dependency.** No `ProjectReference` to an app, no `SideGigPh` (or any app) namespace, type, route or string.
2. **No domain words** in code, docs or copy: Professional, Seeker, listing, inquiry, tradesman, peso/₱, town names… A component takes its labels, icons and data as `[Parameter]`s; the app supplies the domain.
3. **Text through `CommonCopy` only.** Never reference the app's `Copy`. A skeleton string must be true in every app; if an app needs different wording, add a `string?` parameter that falls back to `CommonCopy` (`MePage.StartOverWarning`, `InterviewStep.NextLabel`).
4. **Brand through `AppBrand` only.** Never hard-code a product name, logo or hex color; inject `AppBrand` (`BrandMark`, `AppTopBar`) and use `Color.*` parameters. The only hex values in the skeleton are the defaults in `Branding/BrandStyle.cs`.
5. **No routes to app pages.** A component that navigates takes the `Href` as a parameter (`AppShell.ProfileHref`, `AppTab.Href`, `HomeSection.Href`) or asks an app port (`IAuthLanding`). The skeleton's own routes are `/not-found`, `/verify` (`AuthPage`) and `/` (after Start over / default landing).
6. **Ports, not vendors.** A capability is an interface in `MobileAppTheme` plus a default adapter registered with `TryAddSingleton` in `AddMobileAppTheme` (on-device, or a "not set up" one like `NoIdentityProvider`), so an app can swap it. A cloud SDK (Firebase, Supabase, Azure…) never enters `MobileAppTheme`: it gets its own sibling project `MobileAppTheme.<Vendor>/` (references the skeleton, never the app) with a `builder.Add<Vendor>…()` extension that `Replace`s the port. The UI only sees provider-neutral types (`AuthError`, not the SDK's exceptions).
7. **Same house rules as the app** (`CLAUDE.md`, `new-razor-component`, `mudblazor-ui`): `.razor` + `.razor.cs` code-behind, no `@code`, MudBlazor only (no raw HTML), no `.razor.css`, nullable on, `EditorRequired` on mandatory parameters, small single-purpose components, no dead code.
8. **Public API is a promise.** Renaming or removing a public type, parameter, `CommonCopy` key or CSS class breaks every app. Prefer adding an optional parameter. If you must break it, update every app in the repo in the same change.
9. **Keep the inventories in sync**: `MobileAppTheme/README.md` (what's inside) and the Architecture / generic-components parts of the root `CLAUDE.md`.

## 3. Using the skeleton from the app

- Namespaces are global in the app (`SideGigPh.Maui/GlobalUsings.cs`) and in `Components/_Imports.razor`, so app code uses `AppShell`, `AppText`, `CommonCopy`, `JsonFile<T>` with no extra `using`.
- Register once in `MauiProgram`: `builder.Services.AddMobileAppTheme(AppTheme.Brand);` then the app's ports, then `IAppAccount` (the app's account class, e.g. `LocalAccount`).
- Restyle the app by editing **only** `AppTheme.Style` (a `BrandStyle`) and `AppTheme.Brand` (name + logo icon). Typography and the palette mapping live in `MobileTheme.Create`. If you change `Primary`, `Background` or `Lines`, also update the splash fallbacks in the app's `app.css`.
- Replace a skeleton adapter (e.g. a cloud `IPhotoStore`): register it **before** `AddMobileAppTheme`, or after it (last registration wins).
- Tabbed pages: the app writes a thin side layout (`@layout MainLayout`, renders `<AppShell Tabs=… ProfileHref=…>`), subscribes to its workspace's and `IAppAccount`'s `Changed`, and pages opt in with `@layout`.
- The router (`Components/Routes.razor`) uses the skeleton's `MainLayout` as `DefaultLayout`, its `NotFound` page, and lists `typeof(MainLayout).Assembly` in `AdditionalAssemblies`.
- `index.html` links `_content/MobileAppTheme/mobile-app-theme.css` after MudBlazor's CSS and before `app.css`.

## 4. Promoting an app component into the skeleton

Do this when a second feature *and* plausibly a second product would use it unchanged.

1. `git mv` the `.razor` + `.razor.cs` into `MobileAppTheme/Components/Shared/` (history follows).
2. Namespace → `MobileAppTheme.Components.Shared`; drop `using SideGigPh…`.
3. Replace domain types with parameters (a `ProfessionalListing` becomes `Title`/`Subtitle`/`ChildContent`…). If that makes the component awkward, it isn't generic: leave it in the app.
4. Move its strings from `Copy` to `CommonCopy` (generic wording) or turn them into parameters; delete the old `Copy` keys and update the app's references (`Copy.Back` → `CommonCopy.Back`).
5. Move its CSS helper from `app.css` to `mobile-app-theme.css`.
6. Rewrite the doc comment without product words.
7. Update `MobileAppTheme/README.md`, root `CLAUDE.md`, run the checks (section 6).

## 5. Starting a new app on the skeleton

Full snippets are in `MobileAppTheme/README.md` ("Start a new app"). In short:

1. New .NET MAUI Blazor Hybrid app (Android + iOS), `<ProjectReference Include="..\MobileAppTheme\MobileAppTheme.csproj" />`, no MudBlazor package of its own.
2. Copy SideGigPh's `GlobalUsings.cs` and the `MobileAppTheme.*` lines of `_Imports.razor`.
3. `Components/Layout/AppTheme.cs` with the new `BrandStyle` and `AppBrand(name, icon, MobileTheme.Create(style))`.
4. `MauiProgram`: `AddMauiBlazorWebView()`, `AddMobileAppTheme(AppTheme.Brand)`, an `IAppAccount` implementation, the app's ports.
5. `Routes.razor`, `index.html` (skeleton CSS link + splash) and `app.css` (splash fallbacks matching the brand) as in SideGigPh.
6. Android `<queries>` for `tel`/`smsto` (`IContactLauncher`), `READ_EXTERNAL_STORAGE maxSdkVersion=32`, iOS `NSPhotoLibraryUsageDescription` (`IPhotoPicker`).
7. A `/` page, one side layout with its `AppTab`s, and the app's own `Copy`.

## 6. Checks before saying it's done

```bash
bash .claude/skills/mobile-app-theme/scripts/skeleton-check.sh     # skeleton boundary + CommonCopy keys
bash .claude/skills/new-razor-component/scripts/ui-lint.sh          # raw HTML, hex, literal text, h1s (both projects)
```

Then the `build-check` skill (it copies both projects). A passing build doesn't prove the UI looks
the same; tell the user to check it on a device from Visual Studio.

## Gotchas

| Symptom | Cause | Fix |
|---|---|---|
| `Conflicting assets with the same target path '_framework/blazor.modules…json'` | The skeleton got the WebView host package: directly, or implicitly via `<UseMaui>true</UseMaui>` on a Razor SDK project | Keep the skeleton a **plain `net10.0` Razor class library** (no `UseMaui`): `Microsoft.AspNetCore.Components.Web` + `Microsoft.Maui.Essentials` + MudBlazor. The WebView host package stays in the app |
| `NU1605` package downgrade of `Microsoft.AspNetCore.Components.Web` | MudBlazor needs a newer patch than the skeleton pins | Raise the skeleton's `Components.Web` version to what MudBlazor asks for |
| `FileSystem`, `PhoneDialer`, `MediaPicker`… not found in the skeleton | Plain `net10.0` has no MAUI implicit usings | Add `using Microsoft.Maui.Storage;` / `.ApplicationModel(.Communication)` / `.Media` in the adapter. At runtime the app's Android/iOS Essentials replaces the `net10.0` build (it throws if called outside a MAUI app) |
| `CS0104 'AppTheme' is an ambiguous reference … Microsoft.Maui.ApplicationModel.AppTheme` | MAUI has an `AppTheme` enum | Outside the brand's own namespace, alias it: `using AppTheme = MyApp.Components.Layout.AppTheme;` (see SideGigPh's `MauiProgram`) |
| `RZ10012` unknown component `AppShell` in the app | Razor tag lookup ignores C# global usings | Add the `@using MobileAppTheme.…` line to the app's `Components/_Imports.razor` |
| A skeleton page (e.g. `/not-found`) never shows | Router only scans the app assembly | `AdditionalAssemblies="new[] { typeof(MainLayout).Assembly }"` in `Routes.razor` |
| Skeleton CSS class does nothing | `index.html` doesn't link `_content/MobileAppTheme/mobile-app-theme.css` | Add the link after MudBlazor's CSS, before `app.css` |
| `Unable to resolve service for type IAppAccount` on the Me page | The app didn't register its account | `AddSingleton<IAppAccount>(sp => sp.GetRequiredService<LocalAccount>())` |
