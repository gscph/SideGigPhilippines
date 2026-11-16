---
name: android-emulator-vs-deploy
description: >-
  Diagnose Android emulator networking and Visual Studio deploy failures for this .NET MAUI project.
  Use this whenever the emulator shows "limited internet" or no connectivity, whenever Google/Firebase
  sign-in fails with a network error (status 7) only on the emulator, or whenever a VS deploy hangs on
  "Waiting for emulator to be ready…" / "Emulator is not ready to be used" / silently cancels. CRITICAL:
  on this project the user builds and deploys from Visual Studio — never run CLI builds, dotnet
  -t:Run, adb install, or launch the emulator from the command line as a workaround; diagnose the root
  cause with read-only adb and let the user deploy from VS.
---

# Android emulator + Visual Studio deploy

Two distinct problems share this skill because they look alike ("it won't run on the emulator") but
have different causes: the **emulator has no usable internet**, or **VS can't deploy to it**.

## Ground rule for this project (do not violate)

The user builds, deploys, and runs **from Visual Studio**. Do **not**, as a workaround:

- run CLI Android builds, `dotnet build -t:Run`, or `adb install`;
- launch the emulator from the command line (`emulator.exe`) or Android Studio.

**Why:** concurrent CLI + VS builds cause file-lock/cancel errors, and a CLI-launched emulator makes VS
hang on "Waiting for emulator to be ready…" (VS wants to *own* the emulator, and a CLI-started adb
server of a different version makes VS's adb fight it). Diagnose with **read-only** adb (`devices`,
`getprop`, `dumpsys`), then **fix the root cause and let the user deploy from VS themselves.** The only
writes you should do are cleanup that hands control back to VS (`adb kill-server`, killing a stray
emulator you started).

## Problem A: emulator "limited internet" / Firebase status 7 (NETWORK_ERROR)

### The ping red herring — read this first
`adb shell ping 8.8.8.8` **always fails** in the emulator (its NAT blocks ICMP by design). Ping is
**not** a connectivity test. Judge internet by Android's own validation, not ping.

### Diagnose (read-only)
```bash
# Is Android's network VALIDATED? (it only validates after a real HTTP 204 to Google)
adb shell "dumpsys connectivity" | grep -m1 -oE "VALIDATED|NO_INTERNET"
# DNS working? (resolution, not reachability)
adb shell "ping -c1 www.google.com"     # ignore packet loss; a resolved IP in the header = DNS OK
# Clock correct? (skew breaks TLS and looks like no internet)
echo "host: $(date -u)"; adb shell date
# Right image + account for Google sign-in?
adb shell pm path com.android.vending                 # present = Google Play image (required)
adb shell "dumpsys account | grep -i 'type=com.google'"  # a Google account signed in
```

### Interpretation & fix
- **VALIDATED + DNS resolves** → the internet is fine; a `7` elsewhere is clock, image, or a
  transient SHA propagation — not the network.
- **NO_INTERNET / stale after the host slept or changed networks** → the emulator resumed a **saved
  snapshot** with stale network state. The fix is a **cold boot with DNS pinned**. Tell the user to
  set the AVD's Boot option to **Cold boot** (Device Manager → Edit) and relaunch **from VS**, or to
  cold-boot the AVD in Device Manager. (If you must verify, a cold boot is
  `emulator -avd <name> -dns-server 8.8.8.8,8.8.4.4 -no-snapshot-load` — but per the ground rule,
  prefer having the user launch from VS.)
- **No `com.android.vending`** → it's a non-Play image; Google sign-in can't work. Use a Google Play
  system image and add a Google account.

## Problem B: VS deploy hangs — "Waiting for emulator to be ready…" / "Emulator is not ready to be used"

This is almost always a **control / adb-handshake** problem, not a broken emulator. Confirm the device
is actually fine first:
```bash
adb devices                       # look for "device" (ready) vs "offline"
adb shell getprop sys.boot_completed   # 1 = fully booted
```
If adb shows the device as a booted `device` but VS still says "not ready", the cause is one of:

1. **The emulator was launched outside VS** (CLI / Android Studio). VS tracks readiness of emulators
   *it* starts; an externally-started one races or makes VS spin up a second instance of the same AVD
   (which conflicts). **Fix:** let VS launch the emulator from its device dropdown.
2. **adb server version conflict** — two different `adb.exe` versions fight over the server
   ("adb server version doesn't match this client; killing…"), so VS never sees a stable device.
   **Fix:** `adb kill-server`, then let VS (or one SDK's adb) own the single server.
3. **Multiple Android SDK paths.** Ensure VS's Android SDK (Tools → Options → Android) matches the one
   whose `adb.exe`/`emulator.exe` are in play, so only one adb version exists.

### The clean-slate fix (hand control back to VS)
```bash
adb kill-server
taskkill //F //IM qemu-system-x86_64.exe     # only if you (or CLI) launched a stray emulator
taskkill //F //IM emulator.exe
```
Then tell the user: in VS, pick the AVD in the device dropdown, let **VS** boot it, wait for the home
screen, and Deploy. If it still hangs: **Build → Clean**, cold-boot the AVD from Device Manager, deploy
once more.

## Changing `google-services.json` and "stale" deploys

Config changes are consumed at **build** time (values are baked into resources), so they never cause a
*deploy* failure by themselves. A deploy that fails or runs old behavior is a stale **installed app**
or build cache. The recipe (user runs it, or does the equivalent in VS):
`adb uninstall com.tarbaho.app` (clears signature/version clashes) → **Build → Clean** →
rebuild → deploy. A signature mismatch specifically shows as `INSTALL_FAILED_UPDATE_INCOMPATIBLE` and is
fixed by the uninstall.
