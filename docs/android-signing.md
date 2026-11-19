# Android signing (and why sign-in depends on it)

Firebase checks which key signed the APK. Google sign-in fails with `DEVELOPER_ERROR` (status 10),
and phone sign-in fails app attestation, unless the key's **SHA-1 and SHA-256** are registered on
the Android app in the Firebase console. So every build that should be able to sign in must be
signed with a registered key.

## The two keys

| Key | Where | Used for | Registered in Firebase |
|---|---|---|---|
| **Shared dev key** | `SideGigPh.Maui/Platforms/Android/Signing/dev.keystore` (committed; alias `androiddebugkey`, passwords `android`) | Debug and Release builds on **any** PC, and CI when no release key is set. Sideloaded test APKs. | Yes |
| **Release (store) key** | Never in git. Your PC: any path you choose. CI: GitHub secrets. | Release builds for Google Play | Add it when you create it |

The csproj picks the key automatically: a **Release** build with `ReleaseKeystore` set to an existing
file uses the release key, and everything else uses the shared dev key. There's nothing to set up for
Debug: clone, open in Visual Studio and run.

Shared dev key fingerprints (already registered):

```
SHA1:   A5:F0:C9:6E:1D:B3:FC:85:E7:2E:6C:98:4F:D4:03:1A:D5:C3:31:B6
SHA256: 01:BD:75:86:62:4D:36:5C:10:6F:7F:66:36:8E:C3:41:B3:A4:7F:73:21:40:EC:65:5B:FA:6F:70:31:45:0E:50
```

> The dev key is public (it's in the repo). That's fine for testing, but a store build must use
> the release key. Anyone with the dev key could sign an APK Firebase accepts as yours, so remove its
> fingerprints from Firebase once real users are on the store build.

## Every machine also needs `google-services.json`

It's git-ignored. Download it from Firebase console → Project settings → your Android app, and put it
in `SideGigPh.Maui/Platforms/Android/`. Without it the app builds, but Firebase sign-in
can't start. Download it again after you add or remove a fingerprint.

## Creating the release key (once)

```bash
keytool -genkeypair -v -keystore sidegigph-release.keystore -alias sidegigph -keyalg RSA -keysize 2048 -validity 10000
```

Keep the file and both passwords in a password manager: if you lose them you can't update the Play
listing (unless Play App Signing is on, which it is by default). Then print its fingerprints and add
both in Firebase:

```bash
keytool -list -v -keystore sidegigph-release.keystore -alias sidegigph
```

The upload key (`CN=Joseph Cadiao, O=Tarbaho`, made 17 Nov 2026, in the CI secrets) prints:

```
SHA1:   55:D2:EF:BE:2A:0C:79:7C:88:4E:11:08:47:E2:44:F2:2C:F1:E0:DC
SHA256: F4:C7:8A:57:95:6A:B9:0C:25:0E:BA:4B:5F:30:24:65:EF:71:9F:AB:CB:ED:5C:8B:DC:95:C3:91:5A:89:5B:0F
```

A prod run's page shows the SHA-1 that signed its bundle; it must be this one.

Play's app signing keys (Play Console → Test and release → Setup → App signing), all registered in Firebase on
19 Nov 2026. Play keeps three: the classical key, a quantum-ready one, and the first key (listed under *Previous*):

```
classical  SHA1:   C4:92:16:5A:47:51:61:D1:F2:5E:36:E7:4C:72:66:D8:C9:C6:AF:00
           SHA256: D2:22:0A:43:53:E3:9B:D4:BF:1E:6B:D4:E9:22:4F:6B:82:93:CE:D6:45:A9:12:C7:38:8E:F8:EF:D4:14:0A:09
quantum    SHA1:   81:23:23:C8:14:78:A9:BD:06:5E:60:AC:CD:FD:D0:E9:8F:32:76:E6
           SHA256: 6D:2B:86:67:E4:FD:C1:A3:C3:17:69:25:F0:D9:EA:28:49:54:9C:42:66:FE:12:94:4C:64:DE:64:08:0E:A8:A4
previous   SHA1:   84:B1:40:20:07:CD:C2:54:3C:65:49:F2:84:6D:2A:90:15:80:A1:D1
           SHA256: 5A:F1:1A:C2:19:FB:C1:D1:52:81:85:0C:83:EC:AE:4F:3F:60:94:DC:55:3B:9E:20:B7:0E:0D:C7:A1:7A:E5:9D
```

**Google Play:** Play re-signs the app with its own *App signing key*. Also register that key's
SHA-1/SHA-256, from Play Console → Setup → App integrity, or sign-in fails only in the Play-installed app.

## Release build on your PC

```powershell
$env:SIDEGIGPH_KEYSTORE_PASS = '<store password>'
$env:SIDEGIGPH_KEY_PASS = '<key password>'
dotnet publish SideGigPh.Maui -f net10.0-android -c Release -p:ReleaseKeystore=D:\keys\sidegigph-release.keystore -p:ReleaseKeyAlias=sidegigph
```

(Or set `SIDEGIGPH_KEYSTORE` and `SIDEGIGPH_KEY_ALIAS` as environment variables instead of the `-p:` options.)

## CI (GitHub Actions secrets)

Repository → Settings → Secrets and variables → Actions:

| Secret | Value | Required |
|---|---|---|
| `GOOGLE_SERVICES_JSON_BASE64` | `google-services.json`, base64 | **Yes** |
| `ANDROID_KEYSTORE_BASE64` | release keystore, base64 | For `prod` builds (the run fails without it) |
| `ANDROID_KEY_ALIAS` | e.g. `sidegigph` | with the keystore |
| `ANDROID_KEYSTORE_PASSWORD` | store password | with the keystore |
| `ANDROID_KEY_PASSWORD` | key password | with the keystore |

Base64 a file in PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("SideGigPh.Maui\Platforms\Android\google-services.json")) | Set-Clipboard
```

Without the keystore secrets, a `beta` run signs its APK with the shared dev key, so test APKs can sign in.
A `prod` run (the AAB for Google Play) refuses to build without them, and fails if the bundle came out signed
with the dev key. Uploading to Play and the rest of the store setup: [`play-store.md`](play-store.md).

## Checking which key signed an APK

```bash
keytool -printcert -jarfile path\to\com.tarbaho.app-Signed.apk
```

The SHA-1 must be one of the fingerprints registered in Firebase.
