#!/usr/bin/env bash
# Prints "true" when a group of files changed since a commit, else "false". The one list deploy-api.yml and
# database.yml decide from (their push "paths:" mirror it):
#   database   the SQL scripts and the migration tool (database.yml runs the scripts the live database hasn't had)
#   api        what deploy-api.yml builds and deploys: the API, its stores, the contracts, the operator site, the
#              legal pages it serves, the version files (not the SQL scripts)
# Usage: changed.sh <database|api> <since>
#   since = a commit (e.g. github.event.before; empty, all zeros or unknown compares with the commit before HEAD,
#   like the push "paths:" filter), or "live-api": the commit the live API runs, read from the X-Api-Version
#   header of its public GET config (0.4.2+9b830bc). When the live commit can't be known (API down, not in the
#   history) it prints "true": doing the work is the safe answer.
# Needs the full history (actions/checkout with fetch-depth: 0).
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

API_URL="${API_URL:-https://api.tarbaho.com/api/}"

case "${1:-}" in
  database)
    paths=(SideGigPh.AzureSQL/Sql SideGigPh.Migrate .github/workflows/database.yml) ;;
  api)
    paths=(SideGigPh.Api SideGigPh.AzureSQL ':(exclude)SideGigPh.AzureSQL/Sql' SideGigPh.Contracts SideGigPh.Admin
      docs/legal/privacy-policy.md docs/legal/terms-of-service.md docs/legal/data-handling.md
      Directory.Build.props Directory.Build.targets .github/workflows/deploy-api.yml) ;;
  *)
    echo "Usage: changed.sh <database|api> <commit|live-api>" >&2; exit 2 ;;
esac

since="${2:-}"
if [ "$since" = "live-api" ]; then
  since="$(curl -fsS -m 60 -D - -o /dev/null "${API_URL}config" 2> /dev/null \
    | tr -d '\r' | sed -n 's/^[Xx]-[Aa]pi-[Vv]ersion: .*+\([0-9a-f]\{7,40\}\)$/\1/p' | head -n 1 || true)"
  echo "The live API runs commit ${since:-unknown}." >&2
fi

if [ -z "$since" ] || ! git cat-file -e "$since^{commit}" 2> /dev/null; then
  if [ "${2:-}" = "live-api" ]; then
    echo "The live API's commit is unknown: assuming $1 changed." >&2
    echo true
    exit 0
  fi
  since="HEAD~1"
fi

if git diff --quiet "$since" HEAD -- "${paths[@]}"; then echo false; else echo true; fi
