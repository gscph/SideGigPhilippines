# MobileAppTheme

A reusable skeleton for **.NET MAUI Blazor Hybrid** apps (Android + iOS) built with **MudBlazor 9**: a plain
`net10.0` Razor class library (`Microsoft.AspNetCore.Components.Web` + `Microsoft.Maui.Essentials` + MudBlazor).
Think of it as an admin-dashboard template for phones: the app frame, navigation, theme, common
screens and phone adapters are done; a new product adds its brand and its features.

`SideGigPh.Maui` is the first app built on it. The skeleton never references an app.

## What's inside

| Folder | Contents |
|---|---|
| `Auth/` | Provider-neutral sign-in contract: `IIdentityProvider` (with `Methods`; phone code, Google, `LinkPhoneAsync` to add a verified number to the signed-in account, `SetDisplayNameAsync`), `AuthMethods`, `AuthUser` (`HasName`, `HasVerifiedPhone`), `AuthResult`, `AuthError` (+ `AppText.Describe`), `PhoneVerification`; `SignInGate` (pages ask it which sign-in step is missing: sign in, name, verified phone), `AuthRoutes`, `PersonName.Short`; app hooks `IAuthLanding` (where to go after sign-in) and `IPhoneNumberFormat` (typed number → E.164); defaults `NoIdentityProvider`, `HomeAuthLanding`, `E164PhoneNumberFormat` |
| `Components/Auth/` | `AuthPage` (`/verify`: brand + tagline, only the methods the provider offers, phone first), `NamePage` (`/verify/name`: first + last name once, when the account has none), `AddPhonePage` (`/verify/phone?then=…`: link a verified number), `PhoneSignIn` (number → SMS code; `LinkToCurrentUser` to link instead of sign in), `GoogleSignInButton` |
| `Branding/` | `BrandStyle` (colors + corner radius), `MobileTheme.Create(style)` → `MudTheme` (palette + mobile typography), `AppBrand(Name, LogoIcon, Theme, Tagline?)` |
| `Components/Layout/` | `MainLayout` (theme + popover providers), `AppShell` (top bar, optional sticky one-line `Banner` under it, page, bottom tabs), `AppTopBar` (brand + avatar link + status dot), `AppBottomNav` / `AppNavItem` / `AppTab` (icon tabs, badges, most-specific-match highlighting) |
| `Components/Shared/` | `InterviewStep` (one question per screen: back + thin progress, optional emoji `Illustration`, centered big title and why-we-ask, short steps centered on screen, sticky ink Next), `ChoiceTiles` / `ChoiceTile` / `ChoiceItem` (bordered answer rows, ink border when chosen), `SheetHeader` / `SheetPage` (full-screen sheet: ✕/← + centered title, used by the sign-in screens), `MenuRow` (settings row with chevron), `ProfileCard` (raised card with big photo and name), `WorkingLoader` (step-by-step "labor illusion" loader), `BusyOverlay` (hosted in `MainLayout`: light veil, spinner and a rotating word like "Thinking…" / "Sending…" while `BusyIndicator` is busy; calls under 300 ms never show it), `PersonAvatar` (photo → initials → icon), `HomeSection` (collapsible home section), `FilterMenu` (chip with dropdown; scrolls, optional `ChoiceItem.Group` headings), `BrandMark` (logo + name), `LanguagePicker`, `StartOverCard` (confirm-twice reset), `MePage` (Me page frame: title, your sections, language, on-this-phone note, Start over) |
| `Components/Pages/` | `NotFound` (`/not-found`) |
| `Localization/` | `AppLanguage` (English / Taglish), `LocalizedText(English, Taglish)`, `AppText` (`T.Get`, `T.Format`), `TimeAgo` (`T.Ago(at)`), `CommonCopy` (the skeleton's own strings) |
| `Models/` | `CatalogOption` (an answer in a reference list; `ChoiceItem.From` turns a list into tiles) |
| `Services/` | `BusyIndicator` (wrap a slow user action: `await Busy.RunAsync(...)` shows `BusyOverlay`). `RemoteConfig<T>` (data every page needs that may be hours stale, loaded once per launch: the saved copy for `RemoteConfigRules.FreshFor` (12 h) with no call, then the server asked with the saved version (`IConfigSource<T>`, "unchanged" = no download), falling back to the stale copy; `Stamped<T>`, `IConfigCache<T>`). Ports: `IAppAccount` (language + Start over, implemented by the app), `IContactLauncher` (call / SMS), `IPhotoPicker` (gallery), `IPhotoStore` (SHA-256-named photos), `IPushNotifications` (notifications from the app's server + the icon badge; default `NoPushNotifications`, Android channels as `PushChannel`) |
| `Services/Local/` | On-device adapters: `DeviceContactLauncher`, `DevicePhotoPicker` (resized to 1280px; asks for the storage permission on Android 12 and older, returns a `PhotoPick` with a `PhotoPickStatus`), `FilePhotoStore` (data URLs for the WebView), `JsonFile<T>` (atomic, corrupt-safe JSON file in app storage), `FileConfigCache<T>` (`RemoteConfig`'s saved copy) |
| `wwwroot/mobile-app-theme.css` | Shell CSS and the "MudBlazor gaps" every app needs (`full-screen`, `sticky-footer`, `choice-tile` / `choice-tile-selected`, `brand-logo`, `app-field` / `app-field-group` / `app-field-code` (Airbnb-style filled fields), `app-button-ink`, `app-link-button`, `app-social-button` / `app-social-icon`, `app-sheet-header` / `app-header-slot`, `app-raised`, `app-rounded`, `app-avatar-xl`, `app-illustration`, `app-bubble*`, flat tall Large buttons, `one-line`, `two-lines`, `app-pill-input`, `app-top-bar` / `app-top-bar-logo`, `app-bottom-nav`, `app-content`, `app-sticky-action`, `app-fab`, error UI, status-bar safe area) |
| `ServiceCollectionExtensions.cs` | `services.AddMobileAppTheme(brand)`: MudBlazor, the brand, `AppText`, the three device adapters, `TimeProvider.System` and the auth defaults (`TryAdd`, so an app can swap them) |

**Vendor adapters** live in sibling projects so the skeleton never depends on a cloud SDK:

| Project | Contents |
|---|---|
| `../MobileAppTheme.Firebase/` | Firebase Authentication (phone SMS code + Google, phone linking, display name), Android + iOS: `builder.AddFirebaseAuth(googleWebClientId)` replaces `NoIdentityProvider`; Android `MainActivity.OnActivityResult` calls `FirebaseAuthSetup.HandleActivityResult(...)`. Errors are logged in full and mapped to `AuthError` (Google status 7 / 10 / 12500 / 12501 decoded). Also Cloud Messaging: `builder.AddFirebasePush(channels)` replaces `NoPushNotifications` with `FirebasePushNotifications` (creates the Android channels, forwards notification taps, clears read notifications and sets the badge in `ShowOnly`); Android needs `POST_NOTIFICATIONS` and `LaunchMode.SingleTop` on `MainActivity`. Both setup calls share one SDK start (`FirebaseCore`). |

To use a different auth provider, write `MobileAppTheme.<Provider>/` the same way (implement
`IIdentityProvider`, map failures to `AuthError`, expose `builder.Add<Provider>Auth(...)` that
`Replace`s the port) and reference it instead. No page or app service changes.

Rules for changing the skeleton (what may and may not go in it) are in `CLAUDE.md` here and in
the `mobile-app-theme` skill.

## Start a new app

1. **Create the app** next to this folder: a .NET MAUI Blazor Hybrid project targeting
   `net10.0-android;net10.0-ios`, then reference the skeleton (no MudBlazor package of its own):

   ```xml
   <ItemGroup>
       <ProjectReference Include="..\MobileAppTheme\MobileAppTheme.csproj" />
   </ItemGroup>
   ```

2. **Imports.** `GlobalUsings.cs`:

   ```csharp
   global using MobileAppTheme;
   global using MobileAppTheme.Branding;
   global using MobileAppTheme.Components.Layout;
   global using MobileAppTheme.Components.Shared;
   global using MobileAppTheme.Localization;
   global using MobileAppTheme.Models;
   global using MobileAppTheme.Services;
   global using MobileAppTheme.Services.Local;
   ```

   and in `Components/_Imports.razor` (Razor tags don't see C# global usings):

   ```razor
   @using MudBlazor
   @using MobileAppTheme
   @using MobileAppTheme.Branding
   @using MobileAppTheme.Components.Layout
   @using MobileAppTheme.Components.Shared
   @using MobileAppTheme.Localization
   @using MobileAppTheme.Models
   @using MobileAppTheme.Services
   ```

3. **Brand** in `Components/Layout/AppTheme.cs`, the only place colors live:

   ```csharp
   public static class AppTheme
   {
       public static BrandStyle Style { get; } = new() { Primary = "#e4572e", PrimaryDarken = "#b8401f", PrimaryLighten = "#f08a6c" };

       public static AppBrand Brand { get; } = new("MyApp", Icons.Material.Filled.Bolt, MobileTheme.Create(Style));
   }
   ```

4. **DI** in `MauiProgram.cs` (MAUI has an `AppTheme` enum too, hence the alias):

   ```csharp
   using AppTheme = MyApp.Components.Layout.AppTheme;
   ...
   builder.Services.AddMauiBlazorWebView();
   builder.Services.AddMobileAppTheme(AppTheme.Brand);
   builder.Services.AddSingleton<MyAccount>();                      // implements IAppAccount
   builder.Services.AddSingleton<IAppAccount>(sp => sp.GetRequiredService<MyAccount>());
   // then the app's own ports and adapters
   ```

5. **Router** `Components/Routes.razor`:

   ```razor
   <Router AppAssembly="typeof(MauiProgram).Assembly" AdditionalAssemblies="new[] { typeof(MainLayout).Assembly }"
           NotFoundPage="typeof(MobileAppTheme.Components.Pages.NotFound)">
       <Found Context="routeData">
           <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
           <FocusOnNavigate RouteData="routeData" Selector="h1" />
       </Found>
   </Router>
   ```

6. **Host page** `wwwroot/index.html`: link the CSS in this order, keep the error UI and safe-area blocks.
   Copy the pre-Blazor splash (`.app-loading` markup + CSS) from SideGigPh and set its fallback
   colors to your `BrandStyle`, because it paints before the theme exists.

   ```html
   <link rel="stylesheet" href="_content/MudBlazor/MudBlazor.min.css" />
   <link rel="stylesheet" href="_content/MobileAppTheme/mobile-app-theme.css" />
   <link rel="stylesheet" href="app.css" />
   ...
   <div class="status-bar-safe-area"></div>
   <div id="app">…splash…</div>
   <div id="blazor-error-ui" data-nosnippet>An unhandled error has occurred. <a href="." class="reload">Reload</a> <span class="dismiss">🗙</span></div>
   <script src="_framework/blazor.webview.js" autostart="false"></script>
   <script src="_content/MudBlazor/MudBlazor.min.js"></script>
   ```

7. **Platform permissions** for the device adapters you use:
   - `IContactLauncher` on Android 11+: `<queries>` with `<intent><action android:name="android.intent.action.DIAL" /><data android:scheme="tel" /></intent>` and the `SENDTO` / `smsto` equivalent in `AndroidManifest.xml`.
   - `IPhotoPicker`: `READ_EXTERNAL_STORAGE` with `android:maxSdkVersion="32"` (Android 13+ needs none) and iOS `NSPhotoLibraryUsageDescription`.

8. **Sign-in (optional).** Without a provider the app runs and `/verify` says sign-in isn't available.
   For Firebase: reference `..\MobileAppTheme.Firebase\MobileAppTheme.Firebase.csproj`, call
   `builder.AddFirebaseAuth(googleWebClientId)` in `MauiProgram`, forward Android's
   `OnActivityResult` to `FirebaseAuthSetup.HandleActivityResult(...)`, add `google-services.json` /
   `GoogleService-Info.plist` (with `Condition="Exists(...)"`) and register the SHA-1/SHA-256 (see the
   `maui-firebase-auth` skill). Register your own `IAuthLanding` (where users go after sign-in) and,
   for one-country apps, an `IPhoneNumberFormat` for local number formats. Give `AppBrand` a
   `Tagline` to show under the logo.

9. **Pages.** A `/` page, an app `Copy` class (`LocalizedText` pairs) for your strings, and per
   user role a thin layout:

   ```razor
   @inherits LayoutComponentBase
   @layout MainLayout

   <AppShell Tabs="Tabs" ProfileHref="/me" UserName="@Name">
       @Body
   </AppShell>
   ```

   with `Tabs` = `AppTab(href, icon, label, badge, MatchPrefix)` entries in its code-behind. Pages
   opt in with `@layout`. Full-screen steps use `InterviewStep` without the shell. The Me tab is
   `<MePage Title="…">your sections</MePage>`.

## Languages

The skeleton is bilingual by design: **English and Taglish**, picked by the user. Every string is a
`LocalizedText(English, Taglish)` resolved by `AppText`. To support a different language pair,
change `LocalizedText`, `AppLanguage`, `AppText.Get`, `LanguagePicker` and `CommonCopy` together
(it touches every app's `Copy`).
