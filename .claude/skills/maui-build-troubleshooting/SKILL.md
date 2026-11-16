---
name: maui-build-troubleshooting
description: >
  Diagnose and clear .NET MAUI Android build/deploy failures that are environmental, not code —
  "Build was canceled" (MSB4188 / XAA2L7000), "file in use by another process" (XARLP7024),
  "user-mapped section open" (XALNS7024), obj/bin that won't delete, a build/deploy stuck on
  "Waiting for emulator to be ready", or a Debug app that installs but won't start and logs
  "No assemblies found in '.../files/.__override__/...' ... Assuming this is part of Fast
  Deployment. Exiting..." (SIGABRT before any C# runs). Use when a MAUI build cancels or locks,
  or the app dies on launch from Visual Studio, before assuming the code is broken.
---

# MAUI Android build/deploy troubleshooting

These errors are almost always **environmental** (two builders fighting over `obj`/`bin`), not
your code. Confirm the project compiles cleanly on its own before changing any source.

## Golden rule: one builder at a time
**The user builds, deploys, and runs this project from Visual Studio.** Do NOT, as a
"let me just verify" workaround:
- run CLI `dotnet build -f net10.0-android`, `dotnet build -t:Run`, or `dotnet publish`,
- run `adb install` / deploy (the one allowed device write is `fix-fast-deploy.sh` uninstalling
  the app, below), or
- launch the Android emulator from the CLI (`emulator.exe`).

Concurrent CLI + VS builds lock/cancel each other, and a CLI-launched emulator makes VS hang on
"Waiting for emulator to be ready…" (VS wants to own the emulator + adb server). Diagnose the
**root cause**, fix it, then let the user build/deploy from VS. Read-only adb probes
(`adb devices`, `getprop`, `dumpsys`) for diagnosis are fine — just don't leave a stray adb
server or emulator running.

## Error decoder
| Code | Meaning | Usual cause |
|------|---------|-------------|
| `MSB4188` / `XAA2L7000` | "Build was canceled" | A second build/deploy started, or the first was aborted |
| `XARLP7024` | "...file...used by another process" | Concurrent build touching the same `obj` |
| `XALNS7024` | "user-mapped section open" | VS or a **running/deployed app** has the intermediate `.dll` memory-mapped |
| clean fails: "Directory not empty" | files are locked | VS / running app still holds them — close it first |

## App installs but won't start: "No assemblies found … Fast Deployment"
The VS debug output (Logcat) shows, right after `am start`:
```
F/monodroid: No assemblies found in '/data/user/0/com.tarbaho.app/files/.__override__/x86_64' or '<unavailable>'. Assuming this is part of Fast Deployment. Exiting...
F/monodroid: ALL entries in APK named `lib/x86_64/` MUST be STORED. ...
F/libc    : Fatal signal 6 (SIGABRT) ...
```
**Not a code or build error.** In Debug, VS Fast Deployment installs an APK with **no** .NET DLLs
and pushes them separately into the app's `files/.__override__/<abi>/` folder. If that folder is
empty, the runtime aborts before any C# runs. VS skipped the push because its cache says the DLLs
are already on the device (typical after app data was cleared, or an emulator wipe/snapshot).
The "MUST be STORED" line is a red herring.

**Fix — run the bundled script (the owner has approved it uninstalling the app for this):**
```bash
bash .claude/skills/maui-build-troubleshooting/scripts/fix-fast-deploy.sh           # diagnose, uninstall if the folder is empty
bash .claude/skills/maui-build-troubleshooting/scripts/fix-fast-deploy.sh --check   # diagnose only
```
It finds `adb.exe`, checks there is exactly one device, lists `files/.__override__/<abi>` via
`run-as`, and uninstalls `com.tarbaho.app` only when the DLLs are missing. It never builds,
installs, starts the emulator, or kills the adb server. Then tell the user:
1. In VS: **Build → Rebuild**, then debug. VS reinstalls and pushes the DLLs fresh.
2. The uninstall wiped the app's saved data (`onboarding.json`, photos), so the app restarts at
   the language screen.

If it keeps coming back, the durable fix is to stop using Fast Deployment in Debug
(`<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>` in a Debug `PropertyGroup`): slower
deploys, but the DLLs ship inside the APK. Ask the owner before changing the csproj.

## Fix sequence (in Visual Studio)
1. Stop debugging (`Shift+F5`) and **close the app** on the device/emulator — a running app keeps
   its DLLs memory-mapped.
2. `Build → Clean Solution`, then `Build → Rebuild`.
3. Still locked? `dotnet build-server shutdown`, close VS entirely, delete `obj`/`bin` for the TFM,
   reopen, rebuild.

## Emulator stuck ("Waiting for emulator to be ready")
Let **VS** launch the AVD. If a CLI-started emulator/adb is fighting it: kill the stray emulator,
`adb kill-server`, then launch the AVD from the VS device dropdown and deploy.

## Clean command (when VS is closed)
```bash
dotnet build-server shutdown
rm -rf SideGigPh.Maui/obj/Debug/net10.0-android SideGigPh.Maui/bin/Debug/net10.0-android
```
If files remain, something still has them open — close VS / stop the app.

## Verifying a build via CLI (only when VS is closed)
Don't mask the exit code through a pipe (`| tail` swallows `dotnet`'s failure). Capture it and
grep the summary:
```bash
dotnet build SideGigPh.Maui/SideGigPh.Maui.csproj -f net10.0-android -c Debug > build.log 2>&1
echo "EXIT=$?"; grep -E "error |Build succeeded|Build FAILED| Error\(s\)" build.log
```
Run long builds in the background and read the log when notified.
