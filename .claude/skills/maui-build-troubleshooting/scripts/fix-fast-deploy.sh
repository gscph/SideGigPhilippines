#!/usr/bin/env bash
# Fix "No assemblies found ... Assuming this is part of Fast Deployment. Exiting..." on launch.
#
# Diagnoses the Debug app's Fast Deployment folder (files/.__override__) on the attached
# emulator/device and, only when the .NET DLLs are missing AND the runtime logged that abort,
# uninstalls the app so the next Visual Studio deploy pushes everything fresh.
# Never builds, installs, launches the emulator or kills the adb server (VS owns those).
#
# Usage: fix-fast-deploy.sh [--check] [--force]
#   --check  diagnose only, never uninstall
#   --force  uninstall when the DLLs are missing even if logcat has no abort line
# Pick a device with ANDROID_SERIAL=emulator-5554 when more than one is attached.
set -u

PACKAGE="com.sidegigph.app"
CHECK_ONLY=0
FORCE=0
for arg in "$@"; do
  case "$arg" in
    --check) CHECK_ONLY=1 ;;
    --force) FORCE=1 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

ADB="$(command -v adb 2>/dev/null || true)"
for candidate in \
  "/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe" \
  "$HOME/AppData/Local/Android/Sdk/platform-tools/adb.exe" \
  "${ANDROID_HOME:-/nonexistent}/platform-tools/adb.exe" \
  "${ANDROID_HOME:-/nonexistent}/platform-tools/adb"; do
  [ -z "$ADB" ] && [ -x "$candidate" ] && ADB="$candidate"
done
if [ -z "$ADB" ]; then
  echo "adb not found. Install the Android SDK platform-tools (VS installs them)." >&2
  exit 1
fi

# Don't start an adb server here: if none is running, VS isn't connected either.
devices="$("$ADB" devices 2>/dev/null | awk 'NR>1 && $2=="device" {print $1}')"
count="$(printf '%s\n' "$devices" | grep -c . || true)"
if [ "$count" -eq 0 ]; then
  echo "No ready device. Start the emulator from Visual Studio first." >&2
  exit 1
fi
if [ "$count" -gt 1 ] && [ -z "${ANDROID_SERIAL:-}" ]; then
  echo "More than one device attached; set ANDROID_SERIAL to one of:" >&2
  printf '  %s\n' $devices >&2
  exit 1
fi
adb_() { "$ADB" "$@"; }
echo "Device: ${ANDROID_SERIAL:-$devices}"

if ! adb_ shell pm path "$PACKAGE" 2>/dev/null | grep -q '^package:'; then
  echo "$PACKAGE is not installed. Nothing to fix: deploy from Visual Studio."
  exit 0
fi

abi="$(adb_ shell getprop ro.product.cpu.abi | tr -d '\r')"
listing="$(adb_ shell run-as "$PACKAGE" ls -R files/.__override__ 2>&1 | tr -d '\r')"
if printf '%s' "$listing" | grep -qi 'not debuggable\|unknown package'; then
  echo "run-as refused ($listing). This isn't a Debug build, so it isn't Fast Deployment." >&2
  exit 1
fi
dlls="$(printf '%s\n' "$listing" | grep -c '\.dll$' || true)"
aborted="$(adb_ logcat -d 2>/dev/null | grep -c 'No assemblies found in .*__override__' || true)"

echo "ABI: $abi"
echo "DLLs in files/.__override__: $dlls"
echo "Fast Deployment abort lines in logcat: $aborted"

if [ "$dlls" -gt 0 ]; then
  echo "The DLLs are there, so this isn't the Fast Deployment problem. Read the full Logcat for the real error."
  exit 0
fi
if [ "$aborted" -eq 0 ] && [ "$FORCE" -eq 0 ]; then
  echo "No DLLs, but no abort in logcat either (the app may just not have been deployed yet)."
  echo "Re-run with --force to uninstall anyway."
  exit 0
fi
if [ "$CHECK_ONLY" -eq 1 ]; then
  echo "DIAGNOSIS: Fast Deployment DLLs missing. Run without --check to uninstall $PACKAGE."
  exit 0
fi

echo "Uninstalling $PACKAGE (this also wipes its saved data)..."
adb_ uninstall "$PACKAGE"
if adb_ shell pm path "$PACKAGE" 2>/dev/null | grep -q '^package:'; then
  echo "Uninstall failed; the app is still installed." >&2
  exit 1
fi
echo "FIXED: $PACKAGE uninstalled. In Visual Studio: Build > Rebuild, then debug."
