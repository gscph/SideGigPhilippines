#!/usr/bin/env bash
# Look up MudBlazor API facts from the local NuGet cache (no network).
#   mud-api.sh params <Component>   parameters of a component, incl. common base classes
#   mud-api.sh enum   <Enum>        enum values (only enums that have XML docs)
#   mud-api.sh css    <class>       whether a utility CSS class exists, and its rule
#   mud-api.sh icon   <Name>        whether an icon constant name exists (any icon set)
# Env: MUD_VERSION overrides the version read from MobileAppTheme.csproj (the skeleton owns MudBlazor).
set -euo pipefail

mode="${1:-}"; name="${2:-}"
if [[ -z "$mode" || -z "$name" ]]; then
  sed -n '2,7p' "$0"; exit 2
fi

pkgs="${NUGET_PACKAGES:-$HOME/.nuget/packages}/mudblazor"
root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
csproj="$root/MobileAppTheme/MobileAppTheme.csproj"
ver="${MUD_VERSION:-}"
if [[ -z "$ver" && -f "$csproj" ]]; then
  ver="$(grep -o 'Include="MudBlazor" Version="[^"]*"' "$csproj" | sed 's/.*Version="//; s/"$//' || true)"
fi
dir="$pkgs/$ver"
if [[ -z "$ver" || ! -d "$dir" ]]; then
  echo "MudBlazor $ver not in $pkgs. Restore the project first (or set MUD_VERSION)." >&2; exit 1
fi
lib="$(ls -d "$dir"/lib/net* | sort -V | tail -1)"
xml="$lib/MudBlazor.xml"; dll="$lib/MudBlazor.dll"; css="$dir/staticwebassets/MudBlazor.min.css"
echo "# MudBlazor $ver ($(basename "$lib"))"

props() { grep -o "name=\"P:MudBlazor\.$1\.[A-Za-z0-9]*\"" "$xml" | sed "s/.*\.\([A-Za-z0-9]*\)\"$/\1/" | sort -u | tr '\n' ' ' || true; }

case "$mode" in
  params)
    own="$(props "$name"'\(`[0-9]\)\?')"
    [[ -n "$own" ]] && echo "$name: $own" || echo "$name: (no documented parameters found; check the spelling)"
    # Inherited parameters for the families used in this app.
    case "$name" in
      MudButton|MudIconButton|MudFab|MudToggleIconButton) bases="MudBaseButton";;
      MudTextField|MudNumericField|MudAutocomplete|MudSelect|MudDatePicker) bases='MudBaseInput`1 MudFormComponent`2';;
      MudCheckBox|MudSwitch|MudRadioGroup) bases='MudBooleanInput`1 MudFormComponent`2';;
      *) bases="";;
    esac
    for b in $bases; do
      echo "  inherited from $b: $(props "$b")"
    done
    echo "  every component also has: Class Style Tag UserAttributes (lowercase HTML attributes splat through)"
    ;;
  enum)
    vals="$(grep -o "name=\"F:MudBlazor\.$name\.[A-Za-z0-9]*\"" "$xml" | sed "s/.*\.\([A-Za-z0-9]*\)\"$/\1/" | tr '\n' ' ' || true)"
    [[ -n "$vals" ]] && echo "$name: $vals" || echo "$name: no XML docs. It may still exist (e.g. Severity, MaxWidth, InputType); let the build confirm."
    ;;
  css)
    cls="${name#.}"
    hits="$(grep -o "\.$cls{[^}]*}" "$css" | sort -u | head -5 || true)"
    [[ -n "$hits" ]] && echo "$hits" || echo ".$cls: NOT a MudBlazor class. Use a parameter, another utility, or a helper in app.css 'MudBlazor gaps'."
    ;;
  icon)
    if grep -aq "$name" "$dll"; then echo "$name: found (Icons.Material.Filled.$name / Outlined / Rounded ...)"; else echo "$name: not found"; fi
    ;;
  *) sed -n '2,7p' "$0"; exit 2;;
esac
