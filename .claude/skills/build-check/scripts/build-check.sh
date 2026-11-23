#!/usr/bin/env bash
# Compile-checks SideGigPh in a throwaway copy, so Visual Studio's open build in the repo is never touched.
#
#   build-check.sh            the Android app (SideGigPh.Maui, net10.0-android) and the API (SideGigPh.Api)
#   build-check.sh --app      only the Android app
#   build-check.sh --api      only the API
#   build-check.sh --site     only the landing page (SideGigPh.FrontDoor, Blazor WebAssembly)
#
# The copy lives in $BUILD_COPY (default: $TEMP/sidegigph-build). Source is refreshed on every run (bin/obj are
# kept there, so a second run is incremental). On Windows the app builds through a short drive alias (subst),
# because a long path makes aapt2 fail on the Firebase resources (APT2098 "failed to open file").
set -uo pipefail

repo="$(cd "$(dirname "$0")/../../../.." && pwd)"
copy="${BUILD_COPY:-${TEMP:-/tmp}/sidegigph-build}"
want_app=1; want_api=1; want_site=0
case "${1:-}" in
  --app) want_api=0 ;;
  --api) want_app=0 ;;
  --site) want_app=0; want_api=0; want_site=1 ;;
  "") ;;
  *) echo "usage: build-check.sh [--app|--api|--site]"; exit 2 ;;
esac

mkdir -p "$copy"
echo "Refreshing the source in $copy ..."
for project in MobileAppTheme MobileAppTheme.Firebase SideGigPh.Contracts SideGigPh.Maui SideGigPh.AzureSQL SideGigPh.Api SideGigPh.FrontDoor; do
  # Replace the sources but keep bin/obj for an incremental build.
  find "$copy/$project" -mindepth 1 -maxdepth 1 ! -name bin ! -name obj -exec rm -rf {} + 2>/dev/null
  mkdir -p "$copy/$project"
  (cd "$repo" && tar --exclude=bin --exclude=obj --exclude=.vs -cf - "$project") | tar -xf - -C "$copy"
done
# The API embeds the public legal pages (LegalFunctions).
rm -rf "$copy/docs/legal" && mkdir -p "$copy/docs" && cp -r "$repo/docs/legal" "$copy/docs/"
# The landing page links the store feature graphic (its Facebook preview) and two phone screenshots (its hero).
mkdir -p "$copy/docs/store-assets/phone" && cp "$repo/docs/store-assets/feature-graphic.png" "$repo/docs/store-assets/developer-page-icon.png" "$copy/docs/store-assets/" && cp "$repo"/docs/store-assets/phone/*.png "$copy/docs/store-assets/phone/"
# The Firebase settings files are git-ignored, so a worktree has none: take them from the main checkout. Without
# google-services.json the app still builds (the csproj's Condition skips it) but Firebase never starts, and sign-in
# fails on every QA install ("Default FirebaseApp is not initialized").
main="$(cd "$repo" && cd "$(git rev-parse --git-common-dir)/.." 2>/dev/null && pwd)"
for file in SideGigPh.Maui/Platforms/Android/google-services.json SideGigPh.Maui/Platforms/iOS/GoogleService-Info.plist; do
  if [ ! -f "$copy/$file" ] && [ -n "$main" ] && [ -f "$main/$file" ]; then cp "$main/$file" "$copy/$file"; fi
done
[ -f "$copy/SideGigPh.Maui/Platforms/Android/google-services.json" ] \
  || echo "build-check: warning: no google-services.json, so sign-in won't work on this build"
cp "$repo"/*.slnx "$copy/" 2>/dev/null
cp "$repo"/Directory.* "$copy/" 2>/dev/null

status=0

if [ "$want_api" = 1 ]; then
  echo "== SideGigPh.Api"
  output="$(cd "$copy/SideGigPh.Api" && dotnet build -nologo -v q 2>&1)"
  build=$?
  echo "$output" | grep -E " error |Build succeeded|Time Elapsed" | sort -u
  [ $build -eq 0 ] || status=1
fi

if [ "$want_site" = 1 ]; then
  echo "== SideGigPh.FrontDoor"
  output="$(cd "$copy/SideGigPh.FrontDoor" && dotnet build -nologo -v q 2>&1)"
  build=$?
  echo "$output" | grep -E " error | warning (CS|RZ|MUD)|Build succeeded|Time Elapsed" | sort -u
  [ $build -eq 0 ] || status=1
fi

if [ "$want_app" = 1 ]; then
  echo "== SideGigPh.Maui (net10.0-android)"
  dir="$copy/SideGigPh.Maui"
  drive=""
  if command -v subst >/dev/null 2>&1 && command -v cygpath >/dev/null 2>&1; then
    for letter in Q R S T U V W; do
      if [ ! -d "/${letter,,}" ]; then drive="$letter"; break; fi
    done
    if [ -n "$drive" ]; then
      subst "$drive:" "$(cygpath -w "$copy")"
      dir="/${drive,,}/SideGigPh.Maui"
    fi
  fi
  output="$(cd "$dir" && dotnet build -f net10.0-android -c Debug -nologo -v q 2>&1)"
  build=$?
  [ -n "$drive" ] && subst "$drive:" //D
  echo "$output" | grep -E " error |Build succeeded|Time Elapsed" | sort -u
  [ $build -eq 0 ] || status=1
fi

if [ $status -eq 0 ]; then echo "build-check: OK"; else echo "build-check: FAILED"; fi
exit $status
