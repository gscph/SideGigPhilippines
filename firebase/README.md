# firebase/

Firebase is SideGigPh's **sign-in provider** (phone SMS code + Google), behind the skeleton's `IIdentityProvider`, and its **new-message notifications** (Cloud Messaging), behind `IPushNotifications` (both adapters in `MobileAppTheme.Firebase/`). The data is in Azure SQL behind `SideGigPh.Api` ([`azure/`](../azure/README.md)), which checks each request's Firebase ID token.

| File | What it is |
|---|---|
| `cloudshell-auth.sh` | Paste into **Google Cloud Shell**: turns on the Auth admin API and limits SMS codes to Philippine numbers (blocks SMS-pumping fraud). Safe to run again. |

## First time (Firebase console)

1. Put the project on the **Blaze** plan (SMS sign-in needs it).
2. **Authentication → Sign-in method:** turn on **Phone** and **Google**.
3. **Project settings → Your apps → Android:** add the shared dev key's SHA-1 and SHA-256 (see [`docs/android-signing.md`](../docs/android-signing.md)), then download `google-services.json` into `SideGigPh.Maui/Platforms/Android/` (git-ignored). iOS: `GoogleService-Info.plist` into `Platforms/iOS/`.
4. Paste `cloudshell-auth.sh` into Cloud Shell.
5. The API needs the project id: the Function App setting `FirebaseProjectId` (`sidegigphilippines`).

## Notifications (Cloud Messaging)

The API sends a notification when someone gets a chat message ([`docs/data-model.md`](../docs/data-model.md) §6.1). It needs:

1. **The API's key to send:** Firebase console → Project settings → **Service accounts** → **Generate new private key** (a JSON file). Put the **whole JSON** in the Function App setting **`FirebaseServiceAccount`** (Azure portal → Function App → Environment variables), then delete the file. It's a secret: never in the phone app or git. Without it messages still send and the API logs "Push not sent".
2. **Android:** nothing more. Cloud Messaging is on by default and uses the same `google-services.json`.
3. **iOS (before the first iPhone release):** in the Apple developer account make an **APNs auth key** (.p8) and upload it under Project settings → **Cloud Messaging → Apple app configuration**; turn on the **Push Notifications** capability for the app id; add `Platforms/iOS/Entitlements.plist` with `aps-environment` (`development`, `production` for the store) and point the csproj's `CodesignEntitlements` at it. Not in the repo yet, because a build signed without a push-enabled profile fails.
4. **To test on a phone:** sign in, publish a listing (the app asks to allow notifications), close the app, then message that worker from another account. Logs: `adb logcat -s SideGigPh` on the phone, and the Function App's log for `Couldn't notify`.

