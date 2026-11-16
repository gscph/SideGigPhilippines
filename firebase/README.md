# firebase/

Firebase is SideGigPh's **sign-in provider** (phone SMS code + Google), behind the skeleton's `IIdentityProvider`, and its **new-message notifications** (Cloud Messaging), behind `IPushNotifications` (both adapters in `MobileAppTheme.Firebase/`). The data is in Azure SQL behind `SideGigPh.Api` ([`azure/`](../azure/README.md)), which checks each request's Firebase ID token.

| File | What it is |
|---|---|
| `cloudshell-auth.sh` | Paste into **Google Cloud Shell**: turns on the Auth admin API and the Play Integrity API, and limits SMS codes to Philippine numbers (blocks SMS-pumping fraud). Safe to run again. |

## First time (Firebase console)

1. Put the project on the **Blaze** plan (SMS sign-in needs it).
2. **Authentication → Sign-in method:** turn on **Phone** and **Google**.
3. **Project settings → Your apps → Android** (package `com.tarbaho.app`, App ID `1:742654871295:android:dabafefa22ac5c58b26eca`): add the shared dev key's SHA-1 and SHA-256 (see [`docs/android-signing.md`](../docs/android-signing.md)), then download `google-services.json` into `SideGigPh.Maui/Platforms/Android/` (git-ignored). iOS: `GoogleService-Info.plist` into `Platforms/iOS/`.
4. Paste `cloudshell-auth.sh` into Cloud Shell.
5. The API needs the project id: the Function App setting `FirebaseProjectId` (`sidegigphilippines`).

## Phone sign-in: no SMS arrives

Read the real error: `adb logcat | grep -E "FirebaseAuth|SideGigPh"`. Authentication → Usage stays at 0 when Firebase refused to send.

| Log says | Fix |
|---|---|
| `17028 Invalid app info in play_integrity_token` / "This app is not authorized" | The Android app's **SHA-256** (not only SHA-1) is missing in Project settings → Your apps, or the **Play Integrity API** is off (run `cloudshell-auth.sh` again). Can take a few minutes to apply. On an **emulator** it fails anyway (Play can't vouch for an app it didn't install), so `AddFirebaseAuth` switches emulators to the reCAPTCHA check (a browser tab opens before the code is sent). |
| No error, the code screen shows, Usage 0 | The number is under Sign-in method → Phone → *Phone numbers for testing*: no SMS is sent, use its fixed code. |
| `DEVELOPMENT_MODE_MISMATCH` on the code | The code belongs to a testing-number session; ask for a new code. |
| Usage counts the SMS but it never arrives | Carrier filtering: check the phone's Spam folder, try another network. |

## Notifications (Cloud Messaging)

The API sends a notification when someone gets a chat message ([`docs/data-model.md`](../docs/data-model.md) §6.1). It needs:

1. **The API's key to send:** Firebase console → Project settings → **Service accounts** → **Generate new private key** (a JSON file). Put the **whole JSON** in the Function App setting **`FirebaseServiceAccount`** (Azure portal → Function App → Environment variables), then delete the file. It's a secret: never in the phone app or git. Without it messages still send and the API logs "Push not sent".
2. **Android:** nothing more. Cloud Messaging is on by default and uses the same `google-services.json`.
3. **iOS (before the first iPhone release):** in the Apple developer account make an **APNs auth key** (.p8) and upload it under Project settings → **Cloud Messaging → Apple app configuration**; turn on the **Push Notifications** capability for the app id; add `Platforms/iOS/Entitlements.plist` with `aps-environment` (`development`, `production` for the store) and point the csproj's `CodesignEntitlements` at it. Not in the repo yet, because a build signed without a push-enabled profile fails.
4. **To test on a phone:** sign in, publish a listing (the app asks to allow notifications), close the app, then message that worker from another account. Logs: `adb logcat -s SideGigPh` on the phone, and the Function App's log for `Couldn't notify`.

## Analytics and A/B Testing (optional, off by default)

The app is ready for both, behind build flags that are **off**: until you do the steps below, the APK builds and runs
with neither, and nothing is collected ([`docs/architecture.md`](../docs/architecture.md#analytics-and-ab-tests)).

**Google Analytics**

1. Firebase console → Project settings → **Integrations → Google Analytics → Enable** (pick or create an Analytics
   account). Then download `google-services.json` again and replace `SideGigPh.Maui/Platforms/Android/google-services.json`
   (and the `GOOGLE_SERVICES_JSON_BASE64` secret for CI); for iOS the same with `GoogleService-Info.plist`.
2. Add analytics to the privacy policy (`docs/legal/privacy-policy.md`) and to the Play Console **Data safety** form
   (App activity → App interactions, not linked to identity) before testers get a build with it.
3. Turn the flag on: `FirebaseAnalytics` = `true` in `Directory.Build.props` (every build), or only in CI with the
   repo variable `FIREBASE_ANALYTICS` = `true` (GitHub → Settings → Secrets and variables → Actions → Variables).
4. Check it: on a phone with a debug build run `adb shell setprop debug.firebase.analytics.app com.tarbaho.app`,
   open the app, and watch Firebase console → Analytics → **DebugView**. You should see `screen_view` with
   `screen_name` like `/seeker/search`, and the user properties `role`, `language`, `build_label`, `build_channel`.

**A/B Testing** (needs Analytics on)

1. Turn the flag on: `FirebaseExperiments` = `true` (same two places; CI variable `FIREBASE_EXPERIMENTS`).
2. Firebase console → **Remote Config** → add a parameter (a short snake_case key, e.g. `publish_button`) with a
   default value, then **A/B Testing → Create experiment → Remote Config**: target `com.tarbaho.app`, pick the
   parameter and its variants, and a goal event (e.g. `listing_published`).
3. In the app read it where it matters: `Analytics.Variant("publish_button", fallback)` or
   `Analytics.IsEnabled("new_search", false)` (`Services/Analytics/AppAnalytics`). The fallback is what everyone
   sees while the flag is off, the phone is offline, or the console has no value.
4. A `dev` build asks for new values on every launch; `beta` and `prod` at most every 12 hours.
