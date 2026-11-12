---
name: maui-firebase-auth
description: >
  Wire or fix Firebase Authentication (Phone/SMS OTP + Google sign-in) in a .NET MAUI
  Blazor Hybrid app behind an IIdentityProvider port, using the MODULAR Plugin.Firebase
  packages. Use when adding Firebase auth, when the app crashes at launch right after
  adding Firebase, when Google sign-in hangs on "connecting" or returns an opaque status
  (7/10/12501), when wiring phone OTP, or when setting up google-services.json / SHA-1. Also
  covers the easily-missed debug-keystore trap: MAUI/VS signs with the Xamarin "Mono for Android"
  keystore, NOT ~/.android/debug.keystore, so a naive SHA-1 is wrong and causes DEVELOPER_ERROR (10).
---

# Firebase Auth in .NET MAUI (Plugin.Firebase)

Hard-won setup for native Firebase auth in MAUI. The defaults from blogs/docs are often
wrong for the current package version — the notes below are what actually compiles and runs.

## 0. Where it lives in this repo

| Piece | Location |
|---|---|
| Provider-neutral contract (`IIdentityProvider`, `AuthMethods`, `AuthResult`, `AuthError`, `AuthUser`, `IAuthLanding`, `IPhoneNumberFormat`) | `MobileAppTheme/Auth/` (skeleton) |
| Sign-in screens (`AuthPage` `/verify`, `PhoneSignIn`, `GoogleSignInButton`) | `MobileAppTheme/Components/Auth/` (skeleton) |
| **Firebase adapter**: `FirebaseIdentityProvider`, `FirebaseAuthErrors`, `FirebaseAuthSetup` (`AddFirebaseAuth`, `HandleActivityResult`) | `MobileAppTheme.Firebase/` (Android/iOS MAUI library, `Microsoft.NET.Sdk`) |
| App wiring: `builder.AddFirebaseAuth(GoogleWebClientId)`, `IAuthLanding` / `IPhoneNumberFormat` registrations | `SideGigPh.Maui/MauiProgram.cs` |
| `OnActivityResult` → `FirebaseAuthSetup.HandleActivityResult(...)` | `SideGigPh.Maui/Platforms/Android/MainActivity.cs` |
| Config files (`google-services.json`, `GoogleService-Info.plist`) | the app's `Platforms/` |

Rules: no Firebase type or call outside `MobileAppTheme.Firebase/`; the UI only sees `AuthError`
(shown via `AppText.Describe`, `CommonCopy`). Swapping providers is in `CLAUDE.md` → Auth →
"Switching the auth provider". The same error policy (section 6) applies to any provider's adapter.

## 1. Packages — modular, NOT bundled
Use the **modular** Auth packages, Android/iOS only. Here they live in `MobileAppTheme.Firebase.csproj`
with `PrivateAssets="none"`, so their build targets flow to the app (which needs them to turn
`google-services.json` into Android resources); the app doesn't reference them itself:

```xml
<PackageReference Include="Plugin.Firebase.Auth" Version="3.1.1" PrivateAssets="none" />
<PackageReference Include="Plugin.Firebase.Auth.Google" Version="3.1.2" PrivateAssets="none" />
```

Check after a change: the build output has `obj/<cfg>/net10.0-android/googsvcsjson/values/goog_svcs_json.xml`.

**Do NOT use the bundled `Plugin.Firebase` meta-package** — it drags in Crashlytics, which
auto-initializes at launch and **hard-crashes in MAUI** (missing Gradle-injected build ID).
That crash looks like an app that dies during loading with no useful message.

> Package APIs are version-sensitive and docs lag. To find the real API for the version you
> restored, inspect the NuGet DLL instead of guessing:
> `grep -ao -E "SignIn[A-Za-z]*|CrossFirebase[A-Za-z]*|FirebaseAuthGoogle[A-Za-z]*" "$HOME/.nuget/packages/plugin.firebase.auth.google/<ver>/lib/<tfm>/Plugin.Firebase.Auth.Google.dll" | sort -u`
> — or just iterate the Android compiler; its errors reveal exact signatures.

## 2. Config files (not committed)
- Android: `Platforms/Android/google-services.json` → build action `GoogleServicesJson`.
- iOS: `Platforms/iOS/GoogleService-Info.plist` → build action `BundleResource`.
Reference them with `Condition="Exists(...)"` so the build still works before they're added:

```xml
<ItemGroup Condition="$(TargetFramework.Contains('-android'))">
  <GoogleServicesJson Include="Platforms\Android\google-services.json" Condition="Exists('Platforms\Android\google-services.json')" />
</ItemGroup>
```

## 3. Initialization (resilient)
`FirebaseAuthSetup.AddFirebaseAuth(webClientId)` (called from `MauiProgram`) does this in lifecycle
events, then `Replace`s the skeleton's default `NoIdentityProvider`:
- Android `OnCreate`: `CrossFirebase.Initialize(activity)` then `FirebaseAuthGoogleImplementation.Initialize(webClientId)`.
- iOS `WillFinishLaunching`: `CrossFirebase.Initialize()` then `FirebaseAuthGoogleImplementation.Initialize()`.
- `CrossFirebase` lives in `Plugin.Firebase.Core.Platforms.Android` / `...iOS` (NOT `Plugin.Firebase.Core`).
- **Wrap init in try/catch** so a missing config doesn't crash launch — the app should load
  and sign-in fail gracefully instead.
- `webClientId` = the `oauth_client` with `"client_type": 3` in `google-services.json`.

## 4. Android MUST forward OnActivityResult
In `MainActivity.OnActivityResult`, after `base`, call `FirebaseAuthSetup.HandleActivityResult(requestCode, resultCode, data)`
(it forwards to `FirebaseAuthGoogleImplementation.HandleActivityResultAsync`).
**Without this, `SignInWithGoogleAsync()` hangs forever** (no exception, stuck on "connecting").

## 5. The API (behind IIdentityProvider)
- Phone: `CrossFirebaseAuth.Current` (`IFirebaseAuth`).
  - `await auth.VerifyPhoneNumberAsync(phone)` returns **void** (the lib tracks the verification).
  - then `await auth.SignInWithPhoneNumberVerificationCodeAsync(code)` — **one arg** (the code).
  - Adding a number to the signed-in (Google) account: `VerifyPhoneNumberAsync(phone)` then `LinkWithPhoneNumberVerificationCodeAsync(code)`, then `user.GetIdTokenResultAsync(forceRefresh: true)` so the new `phone_number` claim reaches Firestore rules. A number another account has fails with "already associated with a different user account" (no reason code) → `AuthError.PhoneInUse`.
- Google: `await CrossFirebaseAuthGoogle.Current.SignInWithGoogleAsync()` (its own cross instance,
  from `Plugin.Firebase.Auth.Google` — NOT a method on `IFirebaseAuth`).
- `IFirebaseUser` has `Uid`, `DisplayName`, `Email`, `PhotoUrl` (no `PhoneNumber`: read it from `user.ProviderInfos`, the phone provider's entry). Name: `user.UpdateProfileAsync(displayName, photoUrl)`.
- Keep all of this behind `IIdentityProvider` (ports & adapters); `GetCurrentUserAsync` must
  also try/catch (accessing `CrossFirebaseAuth.Current` throws if Firebase isn't initialized).

## 6. Error policy — make failures legible
Never reduce an auth-infra exception to a bare message. Log the full exception (type + stack)
to Logcat/Output, then return `AuthResult.Fail(AuthError, detail)`. For Google, `FirebaseAuthErrors.FromGoogle`
decodes the Google Play status code out of the message (which is just `"7:"`, `"10: …"`),
**no dependency on a Play-services binding**: `7` NETWORK_ERROR → `Network`, `10` DEVELOPER_ERROR and
`12500` → `Misconfigured`, `12501` SIGN_IN_CANCELLED → `Cancelled` (shows nothing). A wrong SMS code is
`FirebaseAuthException` with `Reason == FIRAuthError.InvalidCredential` (`Plugin.Firebase.Core.Exceptions`) → `InvalidCode`.
Phone `SignUp...` logs then rethrows. Add a mapping in `FirebaseAuthErrors` when the log shows an unmapped status.

## 7. Firebase console checklist
- Register the app with the package name = csproj `<ApplicationId>` (they must match).
- Add the **debug AND release SHA-1** (and **SHA-256** — phone auth needs it for app attestation),
  then **re-download `google-services.json`**.
- `DEVELOPER_ERROR` (status 10) on Google sign-in = the running APK's SHA-1 isn't registered.
- Enable the **Phone** and **Google** providers (Authentication → Sign-in method).

### Getting the RIGHT SHA-1 (the trap that causes most DEVELOPER_ERRORs)
**MAUI / Visual Studio on Windows does NOT sign debug builds with `~/.android/debug.keystore`.** It
uses the legacy Xamarin keystore at `%LOCALAPPDATA%\Xamarin\Mono for Android\debug.keystore`. Reading
the SHA-1 from `~/.android/debug.keystore` gives the **wrong** fingerprint — register it and you still
get `DEVELOPER_ERROR`. Don't guess the keystore; read the fingerprint from the **APK actually
installed**, which is authoritative:
```bash
adb shell pm path com.sidegigph.app                     # -> package:/data/app/.../base.apk
adb pull <that base.apk path> ./running.apk
# apksigner lives in <android-sdk>/build-tools/<ver>/
apksigner verify --print-certs ./running.apk | grep -iE "SHA-1|SHA-256"
```
Register **both** SHA-1 and SHA-256 in Firebase, then **re-download `google-services.json`**. SHA
registration is **server-side** — once the running APK is signed with a registered key you do NOT need
to rebuild to clear a `10`. To confirm which local keystore matches, fingerprint candidates with
`keytool -list -v -keystore "<path>" -alias androiddebugkey -storepass android -keypass android | grep SHA1`.

> **CI / other machines (solved in this repo):** the Xamarin debug keystore is per-machine, so the
> csproj signs every build with the committed `Platforms/Android/Signing/dev.keystore` (one registered
> fingerprint everywhere). Only a Release build with `ReleaseKeystore` set uses the store key. See
> `docs/android-signing.md`. If a fresh clone gets status 10, check that the APK really is signed by that
> keystore (`keytool -printcert -jarfile <apk>`) and that `google-services.json` is present.

- A status **7** (NETWORK_ERROR) after the account picker is usually the **emulator**, not your config
  — see the `android-emulator-vs-deploy` skill.
