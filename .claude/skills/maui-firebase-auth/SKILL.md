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

## 1. Packages — modular, NOT bundled
Use the **modular** Auth packages, Android/iOS only:

```xml
<ItemGroup Condition="$(TargetFramework.Contains('-android')) or $(TargetFramework.Contains('-ios'))">
  <PackageReference Include="Plugin.Firebase.Auth" Version="3.1.1" />
  <PackageReference Include="Plugin.Firebase.Auth.Google" Version="3.1.2" />
</ItemGroup>
```

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
`RegisterFirebaseServices()` extension called from `MauiProgram`, in lifecycle events:
- Android `OnCreate`: `CrossFirebase.Initialize(activity)` then `FirebaseAuthGoogleImplementation.Initialize(webClientId)`.
- iOS `WillFinishLaunching`: `CrossFirebase.Initialize()` then `FirebaseAuthGoogleImplementation.Initialize()`.
- `CrossFirebase` lives in `Plugin.Firebase.Core.Platforms.Android` / `...iOS` (NOT `Plugin.Firebase.Core`).
- **Wrap init in try/catch** so a missing config doesn't crash launch — the app should load
  and sign-in fail gracefully instead.
- `webClientId` = the `oauth_client` with `"client_type": 3` in `google-services.json`.

## 4. Android MUST forward OnActivityResult
In `MainActivity.OnActivityResult`, forward to
`FirebaseAuthGoogleImplementation.HandleActivityResultAsync(requestCode, resultCode, data)`.
**Without this, `SignInWithGoogleAsync()` hangs forever** (no exception, stuck on "connecting").

## 5. The API (behind IIdentityProvider)
- Phone: `CrossFirebaseAuth.Current` (`IFirebaseAuth`).
  - `await auth.VerifyPhoneNumberAsync(phone)` returns **void** (the lib tracks the verification).
  - then `await auth.SignInWithPhoneNumberVerificationCodeAsync(code)` — **one arg** (the code).
- Google: `await CrossFirebaseAuthGoogle.Current.SignInWithGoogleAsync()` (its own cross instance,
  from `Plugin.Firebase.Auth.Google` — NOT a method on `IFirebaseAuth`).
- `IFirebaseUser` has `Uid`, `DisplayName`, `Email`, `PhotoUrl` (no `PhoneNumber`).
- Keep all of this behind `IIdentityProvider` (ports & adapters); `GetCurrentUserAsync` must
  also try/catch (accessing `CrossFirebaseAuth.Current` throws if Firebase isn't initialized).

## 6. Error policy — make failures legible
Never reduce an auth-infra exception to a bare message. Log the full exception (type + stack)
to Logcat/Output, and for Google decode the Google Play status code out of the message (which
is just `"7:"`, `"10: …"`) → human text, **no dependency on a Play-services binding**:
`7` NETWORK_ERROR, `10` DEVELOPER_ERROR, `12501` SIGN_IN_CANCELLED. Phone `SignUp...` logs then rethrows.

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

> **CI / other machines:** the Xamarin debug keystore is per-machine; CI and other devs sign with a
> different key, so register each one, or commit a single explicit debug keystore and point the csproj
> at it for a stable fingerprint everywhere.

- A status **7** (NETWORK_ERROR) after the account picker is usually the **emulator**, not your config
  — see the `android-emulator-vs-deploy` skill.
