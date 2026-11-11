#!/usr/bin/env bash
# Boundary check for the MobileAppTheme skeleton and its vendor adapter projects
# (MobileAppTheme.Firebase, MobileAppTheme.<Vendor>…). Read-only, no build.
#   ERROR: the skeleton references any project, an adapter project references anything but the
#          skeleton, either references an app (namespace or the app's Copy class), names the
#          domain (DOMAIN_WORDS), hard-codes hex outside BrandStyle.cs, or a CommonCopy key is
#          used but not defined.
#   WARN : a CommonCopy key nothing uses (in the skeleton or any app).
# Usage: skeleton-check.sh            Exit 1 on any ERROR.
# Env:   APPS="SideGigPh.Maui Other.App"  app folders that use the skeleton (default: SideGigPh.Maui)
#        DOMAIN_WORDS  extended regex of words that must never appear in the skeleton
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
cd "$ROOT"
SK="MobileAppTheme"
APPS="${APPS:-SideGigPh.Maui}"
DOMAIN_WORDS="${DOMAIN_WORDS:-SideGigPh|Professional|Seeker|Tradesm[ae]n|Inquir(y|ies)|Listing|ProWorkspace|SeekerWorkspace|ReferenceData|Peso|₱|Angono|Antipolo|Binangonan|Taytay|GCash}"

errors=0
err()  { echo "ERROR $*"; errors=$((errors + 1)); }
warn() { echo "WARN  $*"; }

mapfile -t ADAPTERS < <(find . -maxdepth 1 -type d -name "$SK.*" | sed 's|^\./||' | sort)
mapfile -t SRC < <(find "$SK" "${ADAPTERS[@]}" \( -name '*.cs' -o -name '*.razor' -o -name '*.css' -o -name '*.csproj' \) \
  -not -path '*/obj/*' -not -path '*/bin/*' | sort)

# 1. No app dependency. The skeleton references no project; an adapter project only the skeleton.
hits="$(grep -n '<ProjectReference' "$SK"/*.csproj)"
[ -n "$hits" ] && { echo "$hits" | sed 's/^/ERROR /; s/$/  (the skeleton must not reference another project)/'; errors=$((errors + 1)); }
for a in "${ADAPTERS[@]}"; do
  hits="$(grep -n '<ProjectReference' "$a"/*.csproj | grep -v "\\\\$SK\\\\$SK.csproj\"")"
  [ -n "$hits" ] && { echo "$hits" | sed "s/^/ERROR $a: /; s/\$/  (an adapter project may reference only $SK)/"; errors=$((errors + 1)); }
done
for app in $APPS; do
  ns="${app//./\\.}"
  hits="$(grep -nE "\b$ns\b" "${SRC[@]}" 2>/dev/null)"
  [ -n "$hits" ] && { echo "$hits" | sed 's/^/ERROR /; s/$/  (app namespace in the skeleton)/'; errors=$((errors + 1)); }
done
hits="$(grep -nE '(^|[^A-Za-z])Copy\.[A-Z]' "${SRC[@]}" | grep -v 'CommonCopy\.')"
[ -n "$hits" ] && { echo "$hits" | sed 's/^/ERROR /; s/$/  (app Copy in the skeleton: use CommonCopy or a parameter)/'; errors=$((errors + 1)); }

# 2. No domain words (code, comments, copy).
hits="$(grep -nE "$DOMAIN_WORDS" "${SRC[@]}")"
[ -n "$hits" ] && { echo "$hits" | sed 's/^/ERROR /; s/$/  (domain word in the skeleton: take it as a parameter)/'; errors=$((errors + 1)); }

# 3. Hex only in BrandStyle defaults.
hits="$(grep -nE '#[0-9a-fA-F]{3,8}\b' "${SRC[@]}" | grep -v "$SK/Branding/BrandStyle.cs" | grep -v '#blazor-error-ui')"
[ -n "$hits" ] && { echo "$hits" | sed 's/^/ERROR /; s/$/  (hex outside BrandStyle.cs)/'; errors=$((errors + 1)); }

# 4. CommonCopy keys: defined vs used (skeleton + apps).
copy="$SK/Localization/CommonCopy.cs"
mapfile -t DEFINED < <(grep -oE 'LocalizedText [A-Za-z0-9]+ =' "$copy" | awk '{print $2}' | sort -u)
USE_PATHS=("$SK" "${ADAPTERS[@]}")
for app in $APPS; do USE_PATHS+=("$app"); done
mapfile -t USED < <(grep -rhoE 'CommonCopy\.[A-Za-z0-9]+' "${USE_PATHS[@]}" --include=*.cs --include=*.razor \
  | sed 's/CommonCopy\.//' | sort -u)
for k in "${USED[@]}"; do
  printf '%s\n' "${DEFINED[@]}" | grep -qx "$k" || err "CommonCopy.$k is used but not defined in $copy"
done
for k in "${DEFINED[@]}"; do
  printf '%s\n' "${USED[@]}" | grep -qx "$k" || warn "CommonCopy.$k is defined but unused (delete it, or it's for future apps?)"
done

# 5. Every skeleton .razor has its code-behind (ui-lint.sh checks the rest of the markup rules).
while IFS= read -r f; do
  case "$(basename "$f")" in _Imports.razor) continue ;; esac
  [ -f "$f.cs" ] || err "$f: no code-behind $(basename "$f").cs"
done < <(find "$SK" -name '*.razor' -not -path '*/obj/*' -not -path '*/bin/*')

echo "--- ${#SRC[@]} files in $SK ${ADAPTERS[*]} checked against apps: $APPS — $errors error(s)"
[ "$errors" -eq 0 ]
