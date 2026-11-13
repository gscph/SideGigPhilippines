#!/usr/bin/env bash
# Creates the next numbered schema change: SideGigPh.AzureSQL/Sql/NNN-schema-<what>.sql, from template.sql.
# Usage: bash .claude/skills/db-migration/scripts/new-migration.sh <short-what>   (e.g. boosts, chat-reports)
set -euo pipefail

what="${1:-}"
if [[ ! "$what" =~ ^[a-z0-9]+(-[a-z0-9]+)*$ ]]; then
  echo "Usage: new-migration.sh <short-what>  (lower-case words joined by '-', e.g. boosts)" >&2
  exit 2
fi

root="$(git rev-parse --show-toplevel)"
sql="$root/SideGigPh.AzureSQL/Sql"
skill="$(cd "$(dirname "$0")/.." && pwd)"

highest=$(ls "$sql" | grep -oE '^[0-9]{3}' | sort -n | tail -1)
next=$(printf '%03d' $((10#$highest + 1)))
script="$next-schema-$what"
file="$sql/$script.sql"

sed -e "s/__NUMBER__/$next/g" -e "s/__WHAT__/$what/g" -e "s/__SCRIPT__/$script/g" "$skill/template.sql" > "$file"
echo "Created ${file#$root/}"
echo "Next: write the guarded steps, mirror them in Rows.cs / SideGigPhDb.cs, then run migration-check.sh."
