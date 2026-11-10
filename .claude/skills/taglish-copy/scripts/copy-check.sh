#!/usr/bin/env bash
# Checks SideGigPh's Copy.cs against its use (read-only, no build).
#   ERROR: a Copy.X used in code but not defined; a {n} placeholder in one language but not the other.
#   WARN : a Copy key defined but never used (delete it: no dead code).
#   INFO : Taglish identical to English (fine for names/terms like "Inbox", "Hilot"; otherwise translate).
# Usage: copy-check.sh [--no-info]. Exit 1 on any ERROR.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
APP="$ROOT/SideGigPh.Maui"
COPY="$APP/Services/Localization/Copy.cs"
SHOW_INFO=1
[ "${1:-}" = "--no-info" ] && SHOW_INFO=0

errors=0
used="$(grep -rhoE 'Copy\.[A-Za-z0-9_]+' --include=*.razor --include=*.cs "$APP/Components" "$APP/Services" \
  | sed 's/^Copy\.//' | sort -u)"
defined="$(grep -oE 'LocalizedText [A-Za-z0-9_]+ =' "$COPY" | awk '{print $2}' | sort -u)"

for k in $used; do
  if ! grep -qx "$k" <<<"$defined"; then echo "ERROR missing: Copy.$k is used but not defined"; errors=$((errors + 1)); fi
done
for k in $defined; do
  grep -qx "$k" <<<"$used" || echo "WARN  unused: Copy.$k"
done

# Join each definition onto one line, then compare the two strings.
tr -d '\r' < "$COPY" | awk '
  /public static readonly LocalizedText/ { buf = $0; open = 1; if ($0 ~ /\);[[:space:]]*$/) { print buf; open = 0 }; next }
  open { buf = buf " " $0; if ($0 ~ /\);[[:space:]]*$/) { print buf; open = 0 } }
' | while IFS= read -r def; do
  key="$(sed -E 's/.*LocalizedText ([A-Za-z0-9_]+) =.*/\1/' <<<"$def")"
  mapfile -t strings < <(grep -oE '"([^"\\]|\\.)*"' <<<"$def")
  [ ${#strings[@]} -lt 2 ] && continue
  en="${strings[0]}"; tl="${strings[1]}"
  en_ph="$(grep -oE '\{[0-9]+\}' <<<"$en" | sort -u | tr '\n' ' ')"
  tl_ph="$(grep -oE '\{[0-9]+\}' <<<"$tl" | sort -u | tr '\n' ' ')"
  if [ "$en_ph" != "$tl_ph" ]; then
    echo "ERROR placeholders differ in Copy.$key: English [$en_ph] vs Taglish [$tl_ph]"
    echo x >> "${TMPDIR:-/tmp}/copy-check-errors.$$"
  fi
  if [ "$SHOW_INFO" -eq 1 ] && [ "$en" = "$tl" ]; then
    echo "INFO  same in both languages: Copy.$key = $en"
  fi
done

if [ -f "${TMPDIR:-/tmp}/copy-check-errors.$$" ]; then
  errors=$((errors + $(wc -l < "${TMPDIR:-/tmp}/copy-check-errors.$$")))
  rm -f "${TMPDIR:-/tmp}/copy-check-errors.$$"
fi

echo "--- $(wc -w <<<"$defined") keys defined, $(wc -w <<<"$used") used, $errors error(s)"
[ "$errors" -eq 0 ]
