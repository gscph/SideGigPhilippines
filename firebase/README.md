# firebase/

Firebase is SideGigPh's **sign-in provider only** (phone SMS code + Google), behind the skeleton's `IIdentityProvider` (`MobileAppTheme.Firebase/`). The data is in Azure SQL behind `SideGigPh.Api` ([`azure/`](../azure/README.md)), which checks each request's Firebase ID token.

| File | What it is |
|---|---|
| `cloudshell-auth.sh` | Paste into **Google Cloud Shell**: turns on the Auth admin API and limits SMS codes to Philippine numbers (blocks SMS-pumping fraud). Safe to run again. |

## First time (Firebase console)

1. Put the project on the **Blaze** plan (SMS sign-in needs it).
2. **Authentication → Sign-in method:** turn on **Phone** and **Google**.
3. **Project settings → Your apps → Android:** add the shared dev key's SHA-1 and SHA-256 (see [`docs/android-signing.md`](../docs/android-signing.md)), then download `google-services.json` into `SideGigPh.Maui/Platforms/Android/` (git-ignored). iOS: `GoogleService-Info.plist` into `Platforms/iOS/`.
4. Paste `cloudshell-auth.sh` into Cloud Shell.
5. The API needs the project id: the Function App setting `FirebaseProjectId` (`sidegigphilippines`).
