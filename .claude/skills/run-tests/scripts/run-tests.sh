#!/usr/bin/env bash
# Runs SideGigPh.Tests in a throwaway copy of the repo, so Visual Studio's open build is never touched.
#
#   run-tests.sh               all tests (unit + integration)
#   run-tests.sh --unit        unit tests only (fast, no database)
#   run-tests.sh --coverage    all tests + line coverage of the core code, per file
#   run-tests.sh --changed     first checks what changed since the last commit; if nothing the tests can see
#                              changed (only CSS, images, docs, .razor markup), prints why and exits 0
#   run-tests.sh --changed=origin/main
#                              the same, but for a branch: everything since it left origin/main (committed or not);
#                              the pr-qa skill uses this, since a PR's changes are already committed
#   Any other argument goes to `dotnet test`, e.g. --filter "FullyQualifiedName~Search".
#
# The copy lives in $TEST_COPY (default: $TEMP/sidegigph-tests). Source is refreshed on every run; bin/obj are
# kept there so a second run only rebuilds what changed. Never builds inside the repo.
set -euo pipefail

scripts="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "$scripts/../../../.." && pwd)"
copy="${TEST_COPY:-${TEMP:-${TMPDIR:-/tmp}}/sidegigph-tests}"
projects=(MobileAppTheme SideGigPh.Contracts SideGigPh.AzureSQL SideGigPh.Api SideGigPh.Migrate SideGigPh.Maui SideGigPh.Tests)
filter=()
coverage=0
rest=()

# Paths the tests compile or read. A change anywhere else (CSS, images, .razor markup, docs) can't change a result.
test_relevant='^(SideGigPh\.Maui/(Models|Services)/|SideGigPh\.Maui/Components/.*Routes\.cs$|SideGigPh\.Maui/GlobalUsings\.cs$|SideGigPh\.Api/|SideGigPh\.AzureSQL/|SideGigPh\.Contracts/|SideGigPh\.Migrate/|SideGigPh\.Tests/|MobileAppTheme/.*\.cs$|.*\.csproj$|Directory\.[^/]*$|global\.json$|docs/legal/[^/]*-[^/]*\.md$)'

for arg in "$@"; do
  case "$arg" in
    --unit) filter=(--filter "Category!=Integration") ;;
    --coverage) coverage=1 ;;
    --changed|--changed=*)
      base="${arg#--changed}"; base="${base#=}"
      if [ -n "$base" ]; then
        committed="$(git -C "$repo" diff --name-only "$base...HEAD")"
        since="since $base"
      else
        committed=""
        since="since the last commit"
      fi
      changed="$( (echo "$committed"; git -C "$repo" diff --name-only HEAD; git -C "$repo" ls-files --others --exclude-standard) | sed '/^$/d' | sort -u)"
      relevant="$(echo "$changed" | grep -E "$test_relevant" | grep -vE '\.(css|scss|md)$' || true)"
      if [ -z "$relevant" ]; then
        echo "No test-relevant change $since (only styling, markup, images or docs): tests skipped."
        exit 0
      fi
      echo "Test-relevant changes:"; echo "$relevant" | sed 's/^/  /'
      ;;
    *) rest+=("$arg") ;;
  esac
done

echo "Refreshing the source in $copy ..."
mkdir -p "$copy"
for project in "${projects[@]}"; do
  if [ -d "$copy/$project" ]; then
    find "$copy/$project" -mindepth 1 -maxdepth 1 ! -name bin ! -name obj -exec rm -rf {} +
  fi
  tar -C "$repo" --exclude=bin --exclude=obj --exclude=.vs --exclude='*.user' --exclude=local.settings.json -cf - "$project" | tar -C "$copy" -xf -
done
cp "$repo"/Directory.*.props "$repo"/Directory.*.targets "$repo"/global.json "$copy"/ 2>/dev/null || true
# The API embeds the public legal pages (LegalFunctions).
rm -rf "$copy/docs/legal" && mkdir -p "$copy/docs" && cp -r "$repo/docs/legal" "$copy/docs/"

cd "$copy"
args=(test SideGigPh.Tests/SideGigPh.Tests.csproj --nologo -v q "${filter[@]}" "${rest[@]}")
if [ "$coverage" = 1 ]; then
  rm -rf SideGigPh.Tests/TestResults
  args+=(--collect "XPlat Code Coverage" --settings SideGigPh.Tests/coverage.runsettings)
fi

dotnet "${args[@]}"

if [ "$coverage" = 1 ]; then
  report="$(ls -t SideGigPh.Tests/TestResults/*/coverage.cobertura.xml | head -1)"
  echo "Coverage report: $copy/$report"
  echo "Line coverage per file (lowest first):"
  awk -f "$scripts/coverage-summary.awk" "$report" | sort -n
  echo "Lines the tests never ran:"
  awk -f "$scripts/uncovered-lines.awk" "$report" | sort -t: -k1,1 -k2,2n | while IFS=: read -r file line; do
    printf "  %s:%s  %s\n" "$file" "$line" "$(sed -n "${line}p" "$file" | sed 's/^ *//')"
  done
fi
