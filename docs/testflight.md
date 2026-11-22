# iOS: TestFlight

The iPhone build is made in GitHub Actions on a Mac runner (`.github/workflows/ios.yml`, **Build iOS to TestFlight**,
manual only), signed for the App Store and uploaded to TestFlight. Nobody needs a Mac. The same build also runs on
Codemagic (`codemagic.yaml`, [below](#build-on-codemagic-when-github-actions-cant-run)) when GitHub Actions can't.

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

## Build on Codemagic (when GitHub Actions can't run)

`codemagic.yaml` at the repo root does the same build on Codemagic's Macs: 500 free macOS M2 minutes a month on a
personal account (not a team), so about 15-20 builds. It doesn't use GitHub Actions, so GitHub billing doesn't matter.
Two workflows, both manual only: **Tarbaho iOS to TestFlight** (`ios-testflight`) and **Tarbaho iOS .ipa (no upload)**
(`ios-ipa-only`). Codemagic has no .NET, so the build installs the same SDK and workload set `ios.yml` pins, with
Xcode 26.6: change `WORKLOAD_VERSION` and `xcode` in `codemagic.yaml` together, as in `ios.yml`.

Set up once (steps 1-7 above are still needed; the files are the same ones):

1. Sign up at codemagic.io with a **personal** account and add the repo (GitHub app, or Git URL with a deploy key).
2. **Team integrations → Developer Portal → Manage keys:** add the App Store Connect API key (step 5) and name it
   **`Tarbaho`** (the name `codemagic.yaml` asks for).
3. **codemagic.yaml settings → Code signing identities:** upload the `.p12` (step 3) with its password under iOS
   certificates, then under iOS provisioning profiles fetch the App Store profile for `com.tarbaho.app` from the
   Developer Portal (or upload the `.mobileprovision`).
4. **Environment variables:** a group named **`tarbaho_ios`** with, ticked *Secret*:
   - `GOOGLE_SERVICE_INFO_PLIST_BASE64`: base64 of `GoogleService-Info.plist` (same as the GitHub secret).
   - `GH_TOKEN` (optional): a fine-grained GitHub token with read access to this repo's pull requests. Without it the
     version counts PR titles only (`feat: …`), not their `minor` / `major` labels, so it can come out lower than the
     Android build's.

Each build: Codemagic → the app → **Start new build** → branch `main` → pick the workflow. The `.ipa` is kept under the
build's Artifacts. The iOS build number is Codemagic's own build number plus `BUILD_NUMBER_OFFSET` (1000), so it stays
above the numbers GitHub's builds already used.

## When it fails

- *Google / Apple open, you pick an account, then sign-in fails*: Firebase Auth couldn't store the user in the
  keychain. `Entitlements.plist` needs `keychain-access-groups` = `$(AppIdentifierPrefix)com.tarbaho.app`, and the
  workflow refuses an `.ipa` without it. Apple also needs the Apple provider enabled in Firebase → Authentication.
- *The app says it's offline, and Safari shows "This connection is not private" issued by "Sinkhole Intermediate CA"*:
  the network's DNS (an ISP filter, ClearPath in PH) blocks `tarbaho.com`. On that iPhone: Settings → Wi-Fi → (i) →
  Configure DNS → Manual → `8.8.8.8`, `8.8.4.4`. Ask the ISP to unblock the domain.
- *The app shows the logo and closes at once* (crash log: `_assertionFailure` in `FirebaseAuth`): Firebase didn't
  start, nearly always because `GoogleService-Info.plist` isn't at the top of the app bundle. The csproj's
  `BundleResource` puts it there (`LogicalName`), and the workflow's *Find and check the .ipa* step refuses a build
  without it. Get crash logs from the iPhone: Settings → Privacy & Security → Analytics & Improvements → Analytics
  Data → `SideGigPh.Maui-….ips`.
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
