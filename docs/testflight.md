# iOS: TestFlight

The iPhone build is made in GitHub Actions on a Mac runner (`.github/workflows/ios.yml`, **Build iOS to TestFlight**,
manual only), signed for the App Store and uploaded to TestFlight. Nobody needs a Mac.

- **Version:** the same `major.minor.patch` as Android (`.github/scripts/app-version.sh`). The iOS build number
  (`CFBundleVersion`) is this workflow's own run number, so it goes up with every run as App Store Connect requires.
- **Cost:** the repo is private, so macOS minutes count 10x against the free Actions minutes (about 200 macOS minutes
  a month on the free plan). One build takes about 20-30 minutes. Untick *upload* to build an `.ipa` without sending it.
- **Pinned .NET:** GitHub's Mac images lag Apple (no Xcode 27 yet), and the newest .NET iOS workload needs Xcode 27,
  so the workflow pins the .NET 10.0.4xx SDK (the image's default `dotnet` is .NET 11) and workload set `10.0.401`
  (iOS 26.5, MAUI 10.0.20, built with Xcode 26.6) in a `global.json` written for the run only (`DOTNET_SDK`, `WORKLOAD_VERSION` in
  `ios.yml`). Visual Studio on a PC keeps its own. Raise both once the image has Xcode 27.
- **Xcode:** the .NET iOS workload builds only with the Xcode it was made for (iOS 26.5.10318 "requires Xcode 26.6",
  which carries the iOS 26.5 SDK). That isn't derivable from the workload's number, so `XCODE_VERSION` in `ios.yml` is
  pinned next to `WORKLOAD_VERSION`: change them together. The `xcode` input overrides it for one run.

## Set up once

1. **App ID** (developer.apple.com → Certificates, IDs & Profiles → Identifiers): explicit bundle id
   `com.tarbaho.app`, with **Sign in with Apple** (`Platforms/iOS/Entitlements.plist`, App Store rule 4.8 since
   Google sign-in is offered) and **Push Notifications** (`aps-environment`, chat and request alerts).
2. **App** in App Store Connect (Apps → + New App) with that bundle id.
3. **Apple Distribution certificate** as a `.p12` (made from a CSR with OpenSSL on Windows; the `.p12` with
   `-legacy` so the Mac keychain can import it).
4. **Provisioning profile** (developer portal → Profiles → +): Distribution → **App Store Connect**, App ID
   `com.tarbaho.app`, the certificate from step 3, any name (e.g. `Tarbaho App Store`). The workflow downloads it by
   bundle id. After a capability changes on the App ID, edit and save the profile again, or signing fails.
5. **App Store Connect API key** (Users and Access → Integrations → App Store Connect API → **Team Keys**), role
   **App Manager**. The `.p8` downloads only once.
6. **Firebase iOS app** (Firebase console → SideGigPh → Add app → iOS, bundle id `com.tarbaho.app`): download
   `GoogleService-Info.plist`. It is git-ignored: keep a copy in `SideGigPh.Maui/Platforms/iOS/` for local builds.
   The workflow writes it from a secret and copies its `REVERSED_CLIENT_ID` (Google sign-in) and Encoded App ID
   (`app-` + `GOOGLE_APP_ID` with `-` for `:`, phone sign-in's web check) into `Info.plist` as URL schemes, so no
   Firebase id is committed.
7. **APNs key** for notifications (developer portal → Keys → +, Apple Push Notifications service, Sandbox &
   Production): upload the `.p8` with its Key ID and the Team ID (developer account → Membership details) in Firebase →
   Project settings → Cloud Messaging → Apple app configuration, in both the Production and Development slots. This is
   not the App Store Connect key from step 5.
8. **Repo secrets** (Settings → Secrets and variables → Actions):

| Secret | Value |
|---|---|
| `APPLE_CERTIFICATE` | base64 of the `.p12` |
| `APPLE_CERTIFICATE_PASSPHRASE` | its password |
| `APP_STORE_CONNECT_KEY_ID` | the API key's 10-character Key ID |
| `APP_STORE_CONNECT_ISSUER_ID` | the Issuer ID above the keys table |
| `APP_STORE_CONNECT_PRIVATE_KEY` | the whole `.p8`, BEGIN/END lines included |
| `GOOGLE_SERVICE_INFO_PLIST_BASE64` | base64 of `GoogleService-Info.plist` |

Base64 of a file on Windows, straight to the clipboard:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("$HOME\tarbaho-signing\tarbaho_ios.p12")) | Set-Clipboard
```

Never commit `.key`, `.csr`, `.p12`, `.p8` or `GoogleService-Info.plist`.

## Each build

1. Actions → **Build iOS to TestFlight** → Run workflow (from `main`).
2. App Store Connect processes the upload for 5-30 minutes, then it shows under the app's **TestFlight** tab.
   `Info.plist` says `ITSAppUsesNonExemptEncryption = false` (only HTTPS), so there is no export-compliance question.
3. **Internal testing:** up to 100 people on the App Store Connect team, no review, available once processed.
4. **External testing:** a group, testers by email or a public link, a beta description, a feedback email and review
   notes (a Firebase test phone number and its code for sign-in, and where report/block and Delete account are). The
   first build of each version goes to Beta App Review (usually 1-2 days).

Builds expire after 90 days. Testers need the TestFlight app on their iPhone.

## When it fails

- *actool: SDK "…/Xcode_26.6.0.app/…/MacOSX.sdk" cannot be located*: Xcode was selected through the image's
  `Xcode_X.0.app` symlink; the workflow resolves it to the real folder, so this means that step changed.
- *The iOS workload needs Xcode X, which this runner doesn't have*: `WORKLOAD_VERSION` points at a workload newer than
  the image's Xcode. Pick a workload set whose iOS version matches an installed Xcode.
- *actool: No simulator runtime version … available*: the selected Xcode has no simulator runtime on the image (the
  asset catalog compiler needs one); pick a workload set whose Xcode has one (the image README lists them).
- *No App Store provisioning profile*: make it (step 4).
- *Provisioning profile doesn't include the aps-environment / Sign in with Apple entitlement*: tick the capability on
  the App ID, then edit and save the profile.
- *Beta contract missing* or an upload refused for agreements: accept the pending agreement in App Store Connect →
  Business.
