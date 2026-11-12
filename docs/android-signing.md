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
| `ANDROID_KEYSTORE_BASE64` | release keystore, base64 | Only for store builds |
| `ANDROID_KEY_ALIAS` | e.g. `sidegigph` | with the keystore |
| `ANDROID_KEYSTORE_PASSWORD` | store password | with the keystore |
| `ANDROID_KEY_PASSWORD` | key password | with the keystore |

Base64 a file in PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("SideGigPh.Maui\Platforms\Android\google-services.json")) | Set-Clipboard
```

Without the keystore secrets, CI signs with the shared dev key, so its test APKs can sign in.

## Checking which key signed an APK

```bash
keytool -printcert -jarfile path\to\com.sidegigph.app-Signed.apk
```

The SHA-1 must be one of the fingerprints registered in Firebase.
