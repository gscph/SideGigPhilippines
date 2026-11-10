#!/usr/bin/env bash
# House-rule lint for SideGigPh .razor files (read-only, no build).
#   ERROR: raw HTML tags, hex colors in components, @code blocks, a .razor without its .razor.cs,
#          a page (@page) with no h1.
#   WARN : text written straight into markup (should come from Copy.cs), a page with more than one
#          h1 in its source (fine only if they're in different @if branches).
# Usage: ui-lint.sh [path ...]   (default: SideGigPh.Maui/Components). Exit 1 on any ERROR.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
cd "$ROOT"
PATHS=("$@")
[ ${#PATHS[@]} -eq 0 ] && PATHS=("SideGigPh.Maui/Components")

errors=0
err()  { echo "ERROR $*"; errors=$((errors + 1)); }
warn() { echo "WARN  $*"; }

mapfile -t FILES < <(find "${PATHS[@]}" -name '*.razor' -not -path '*/obj/*' -not -path '*/bin/*' | sort)

# Brand name and typographic quotes are allowed as literal text.
ALLOWED_LITERAL='^(SideGigPh)$'

for f in "${FILES[@]}"; do
  base="$(basename "$f")"

  grep -nE '<(div|span|p|h[1-6]|button|input|label|section|header|footer|nav|ul|ol|li|svg|img|a|br|hr|table)([ >/]|$)' "$f" \
    | while IFS= read -r line; do echo "ERROR $f:$line  (raw HTML: use MudBlazor)"; done
  [ -n "$(grep -nE '<(div|span|p|h[1-6]|button|input|label|section|header|footer|nav|ul|ol|li|svg|img|a|br|hr|table)([ >/]|$)' "$f")" ] && errors=$((errors + 1))

  if grep -qnE '#[0-9a-fA-F]{3,8}\b' "$f"; then
    err "$f: hex color (use a Color parameter / AppTheme)"
  fi

  if grep -qE '^\s*@code\b' "$f"; then
    err "$f: @code block (move logic to $base.cs)"
  fi

  case "$base" in
    _Imports.razor|Routes.razor|MainLayout.razor) ;;  # framework files
    *) [ -f "$f.cs" ] || err "$f: no code-behind $base.cs" ;;
  esac

  # Text between tags that isn't Razor (@...), e.g. <MudText>Hello</MudText>.
  # LanguagePage is the one allowed exception: no language is chosen yet (taglish-copy skill).
  [ "$base" = "LanguagePage.razor" ] && skip_literal=1 || skip_literal=0
  [ "$skip_literal" -eq 1 ] || grep -noE '>[^<>@]*[A-Za-z]{2,}[^<>@]*<' "$f" | while IFS= read -r hit; do
    text="$(echo "$hit" | sed -E 's/^[0-9]+:>//; s/<$//; s/^[[:space:]]+|[[:space:]]+$//g')"
    [[ -z "$text" || "$text" =~ $ALLOWED_LITERAL ]] && continue
    warn "$f:${hit%%:*}  literal text \"$text\" (put it in Copy.cs)"
  done

  if grep -qE '^@page ' "$f"; then
    h1=$(grep -cE 'HtmlTag="h1"' "$f")
    uses_step=$(grep -c '<InterviewStep' "$f")
    renders_ui=$(grep -c '<Mud' "$f")
    [ "$base" = "LaunchRouter.razor" ] && renders_ui=0  # splash + redirect, no page title
    if [ "$h1" -eq 0 ] && [ "$uses_step" -eq 0 ] && [ "$renders_ui" -gt 0 ]; then
      err "$f: page has no HtmlTag=\"h1\" (FocusOnNavigate needs one)"
    elif [ "$h1" -gt 1 ]; then
      warn "$f: $h1 h1s in source; make sure only one renders at a time"
    fi
  fi
done

# Hex in app.css outside the splash fallbacks var(--mud-palette-x, #hex).
grep -nE '#[0-9a-fA-F]{3,8}\b' SideGigPh.Maui/wwwroot/app.css | grep -vE 'var\(--[a-z-]+, *#[0-9a-fA-F]+\)' | grep -v 'blazor-error-ui' \
  | while IFS= read -r line; do echo "WARN  app.css:$line  (hex outside a splash fallback)"; done

echo "--- ${#FILES[@]} .razor files checked, $errors error(s)"
[ "$errors" -eq 0 ]
