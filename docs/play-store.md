# Google Play: from the first internal test to production

The app's Play package is `com.tarbaho.app` (the Firebase Android app). Store builds come from
`.github/workflows/build.yml` with channel **prod**: a signed **AAB** (Play takes only bundles), stamped
`AppChannel=prod`. Keys and fingerprints: [`android-signing.md`](android-signing.md). What to type in the Play Console's forms
(App access, content rating, Data safety, release notes): [`play-console-answers.md`](play-console-answers.md).

## 1. Once: get the first build into Internal testing

Do these in order. Steps 1–4 make the build; 5–8 make sign-in work on the Play-installed app.

1. **Create the upload key** (`android-signing.md` → Creating the release key). Keep the file and both
   passwords in a password manager, plus one backup copy off GitHub.
2. **Add the GitHub secrets** (repo → Settings → Secrets and variables → Actions): `ANDROID_KEYSTORE_BASE64`,
   `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_PASSWORD` (and `GOOGLE_SERVICES_JSON_BASE64`,
   already there). A prod run fails if any is missing.
3. **Run the build**: Actions → *Build Android* → Run workflow → channel `prod`.
   The run page shows the signing SHA-1; it must be the upload key's, not the dev key's (the run checks).
   Download the artifact `tarbaho-0.1.0-build34-prod-android` (`tarbaho-0.1.0-build34-prod.aab`); the build number in the name is its versionCode, so pick the newest (Play refuses a code it has seen).
4. **Create the app in the Play Console** (Create app → name *Tarbaho*, app, free), then Testing →
   **Internal testing** → Create new release → keep **Play App Signing** on (Google keeps the app signing key,
   your key only uploads) → upload the AAB by hand. The Play API can't create an app, so this first bundle is
   always a manual upload. Add a testers list (Gmail addresses) and copy the opt-in link for them.
5. **Register both keys in Firebase.** Play Console → Test and release → Setup → **App signing**: copy the
   *App signing key certificate* SHA-1 and SHA-256, and the *Upload key certificate* ones. Firebase console →
   Project settings → Android app `com.tarbaho.app` → Add fingerprint, all four. Without the app signing key's,
   Google sign-in fails (status 10) and phone sign-in fails app verification **only** in the app installed
   from Play.
6. **Play Integrity for phone sign-in.** Google Cloud console (the Firebase project) → APIs & Services →
   enable **Play Integrity API**. Firebase uses it to check the app before sending an SMS; without it phone
   sign-in falls back to a reCAPTCHA page.
7. **Download `google-services.json` again** (Firebase → Project settings → the Android app) and update the
   secret `GOOGLE_SERVICES_JSON_BASE64`, so the next build carries the new OAuth clients.
8. **A test number for Play's reviewers and for you.** Firebase console → Authentication → Sign-in method →
   Phone → *Phone numbers for testing*: e.g. `+63 917 555 0100`, code `123456`. No SMS is sent (and it costs
   nothing). Put it in Play Console → App content → **App access** ("All functionality is behind sign-in":
   the number and the code). Reviewers can't get past the sign-in screen without it. Tell testers too: the app still allows only 3
   codes a day per phone and a 6-minute wait after a restart (`SmsSendLimit`), test number or not, so they shouldn't sign out and in again and again.

Then run the build again (step 3) and upload it by hand as a second internal release.
Install from the testers' opt-in link and check: phone sign-in, Google sign-in, publish a listing, search,
chat, a push notification, post a request, delete the account.

## 2. Each new release

CI never uploads to Play: run *Build Android* with channel `prod`, download the AAB from the run, and upload it in
the Play Console (Testing → Internal testing → Create new release, or the track you want).

Every run's number is the Android versionCode, so each upload is higher than the last, as Play requires.
**Start a new run** for each upload: "Re-run jobs" keeps the run number, and Play refuses a versionCode it has seen.

## R8 (code shrinking and obfuscation)

Play's "DEX code optimization is below our threshold … Obfuscation (2%)" means the Java side of the app (MAUI's glue,
AndroidX, Firebase, Play services) wasn't run through R8; under 25% may hurt visibility. Release builds now run R8
(`AndroidLinkTool` `r8`, `AndroidR8ObfuscationMode` `private-members` in `SideGigPh.Maui.csproj`, the most .NET for
Android allows). Each build keeps `mapping.txt` (artifact `…-r8-mapping`); when you upload
the bundle, add it in the Play Console (App bundle explorer → the version → Downloads →
ReTrace mapping file) so crash stacks show real names. Our own keep rules (Java code .NET reads only by name,
such as MAUI's photo picker) are in `Platforms/Android/proguard.cfg`. A missing keep rule only shows as a crash at run time: after
adding an Android library, install a `beta` APK on a phone and try sign-in, notifications, photos and chat.

## 3. Before production (the launch checklist)

**Server**
- Function App setting `ShowErrorDetails` = `false` (no stack traces to phones).
- Sample workers out of the live database: `002-seed`'s `seed-…` rows (`003-rollback-seed.sql`) and
  `006-seed-search-ranking.sql` with `@RemoveOnly = 1`.
- `AdminUids` holds only the real operators. Azure SQL backups on; cost alerts in Azure; an SMS budget in
  Firebase / Google Cloud.
- Error reports are read in SQL only (`dbo.ErrorReports`); there is no public reader.

**Play Console → App content**
- **Privacy policy**: `https://api.tarbaho.com/api/legal/privacy`.
- **Data safety**: phone number, name, email (Google), photos, approximate and precise location (only while
  the app is open), messages (chat), government ID / TESDA photos (badges; deleted after the decision),
  crash and diagnostic data (error reports, can be turned off). References (third people's names and numbers) are
  no longer collected since 18 Nov 2026: untick them in the Data safety form with the next release. Encrypted in
  transit; people can ask for deletion. It must match [`legal/privacy-policy.md`](legal/privacy-policy.md).
- **Account deletion link**: `…/api/legal/data` (in-app: Me → Delete account).
- **App access**: the Firebase test number above.
- **Content rating**: users talk to each other (chat) and share photos; report and block exist.
- **Target audience**: 18 and over. **Ads**: none.
- **Store listing**: name *Tarbaho*, short description (80 characters), full description, 512×512 icon,
  1024×500 feature graphic, at least 2 phone screenshots (search, a worker's profile, a request). Service
  words only: no job / hire / employment, no promises about workers. Country: Philippines.

**Account**
- A **personal** developer account made after November 2023 must run a **closed test with at least 12
  testers for 14 days in a row** before it can apply for production. An organisation account skips this.
- Production goes out as a staged rollout (e.g. 10% → 50% → 100%).

**After launch**
- Remove the shared dev key's fingerprints from Firebase once nobody uses the sideloaded test APKs
  (`android-signing.md`).
