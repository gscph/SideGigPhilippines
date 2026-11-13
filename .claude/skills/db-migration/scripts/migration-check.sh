#!/usr/bin/env bash
# Checks the schema scripts are safe for the live database:
#   1. 001-schema.sql is unchanged (against HEAD, or against the ref given as $1, e.g. origin/main).
#   2. No two scripts share a number.
#   3. In every NNN-schema-*.sql after 001: each CREATE TABLE / ALTER TABLE / CREATE INDEX sits under a guard
#      (IF OBJECT_ID / COL_LENGTH / sys.indexes / sys.columns …), there's no DROP TABLE, and the script records itself
#      in dbo.SchemaChanges.
# Usage: bash .claude/skills/db-migration/scripts/migration-check.sh [base-ref]
set -uo pipefail

root="$(git rev-parse --show-toplevel)"
cd "$root"
sql="SideGigPh.AzureSQL/Sql"
base="${1:-HEAD}"
problems=0

fail() { echo "FAIL: $*"; problems=$((problems + 1)); }

if ! git diff --quiet "$base" -- "$sql/001-schema.sql"; then
  fail "$sql/001-schema.sql changed (vs $base). It's frozen: put the change in a new NNN-schema-*.sql (new-migration.sh)."
fi

dupes=$(ls "$sql" | grep -oE '^[0-9]{3}' | sort | uniq -d)
[[ -n "$dupes" ]] && fail "More than one script numbered: $dupes"

for file in "$sql"/[0-9][0-9][0-9]-schema-*.sql; do
  [[ -e "$file" ]] || continue
  name="$(basename "$file" .sql)"

  # A step is guarded when the nearest non-blank, non-comment line above it (or its own line) starts with IF,
  # or it sits inside an IF … BEGIN … END block.
  awk -v f="$file" '
    function trim(s) { sub(/^[ \t]+/, "", s); sub(/[ \t\r]+$/, "", s); return s }
    /^[ \t]*--/ { next }
    {
      line = trim($0)
      upper = toupper(line)
      if (upper ~ /^IF[ (]/ && upper !~ /BEGIN$/) { guardNext = 1 }
      if (upper ~ /^IF[ (]/ && upper ~ /BEGIN$/) { depth++ }
      else if (prevIf && upper == "BEGIN") { depth++ }
      else if (upper ~ /^END;?$/ && depth > 0) { depth-- }
      if (upper ~ /(CREATE TABLE|ALTER TABLE|CREATE (UNIQUE )?(CLUSTERED |NONCLUSTERED )?INDEX)/ && upper !~ /^IF[ (]/) {
        if (!(depth > 0 || guardNext || prevIf)) { printf "FAIL: %s:%d unguarded: %s\n", f, NR, line; bad = 1 }
      }
      if (upper ~ /DROP TABLE/) { printf "FAIL: %s:%d drops a table (loses live data): %s\n", f, NR, line; bad = 1 }
      if (line != "") { prevIf = (upper ~ /^IF[ (]/ && upper !~ /BEGIN$/); if (upper !~ /^IF[ (]/) guardNext = 0 }
    }
    END { exit bad }
  ' "$file" || problems=$((problems + 1))

  grep -q "INSERT dbo.SchemaChanges (Script) VALUES ('$name')" "$file" \
    || fail "$file doesn't record itself: INSERT dbo.SchemaChanges (Script) VALUES ('$name')"
done

if (( problems > 0 )); then
  echo "$problems problem(s). See .claude/skills/db-migration/SKILL.md."
  exit 1
fi
echo "Schema scripts OK: 001 unchanged, every later step guarded and recorded."
