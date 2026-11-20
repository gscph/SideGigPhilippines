#!/usr/bin/env bash
# Prints, as Markdown, what this build adds since the last successful run of the same workflow on an earlier commit
# of this branch: one line per PR squashed in, its title and a link (the PR says the rest). Used by build.yml for the
# run's summary page. Needs GH_TOKEN, GITHUB_REPOSITORY, GITHUB_WORKFLOW_REF and the full history (fetch-depth: 0).
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

workflow="$(basename "${GITHUB_WORKFLOW_REF%%@*}")"
head="$(git rev-parse HEAD)"

# The newest successful earlier build whose commit is in this one's history.
last_run="" last_sha=""
while IFS=' ' read -r number sha; do
  if [ "$sha" != "$head" ] && git merge-base --is-ancestor "$sha" "$head" 2> /dev/null; then
    last_run="$number" last_sha="$sha"
    break
  fi
done < <(gh run list --repo "$GITHUB_REPOSITORY" --workflow "$workflow" --status success --limit 50 \
  --json number,headSha --jq '.[] | "\(.number) \(.headSha)"' 2> /dev/null || true)

if [ -z "$last_sha" ]; then
  echo "No earlier successful build of this branch to compare with."
  exit 0
fi

echo "**New since build $last_run** (\`${last_sha::7}\`):"
echo
count=0
while IFS= read -r subject; do
  count=$((count + 1))
  if [[ "$subject" =~ ^(.*)\ \(#([0-9]+)\)$ ]]; then
    echo "- ${BASH_REMATCH[1]} ([#${BASH_REMATCH[2]}](https://github.com/$GITHUB_REPOSITORY/pull/${BASH_REMATCH[2]}))"
  else
    echo "- $subject"
  fi
done < <(git log --first-parent --format='%s' "$last_sha..$head")
[ "$count" -gt 0 ] || echo "- Nothing new: same code as build $last_run."
