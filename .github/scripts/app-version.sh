#!/usr/bin/env bash
# Prints the version (major.minor.patch) of the commit checked out, from the PRs squashed into main since the last
# commit that changed AppVersionBase (major.minor) in Directory.Build.props. For each merge, in order:
#   label "major", or a title like "feat!: ..."   major + 1, minor and patch to 0 (breaking change or milestone)
#   label "minor", or a title like "feat: ..."    minor + 1, patch to 0           (a feature people will notice)
#   anything else                                 patch + 1                       (fixes, polish, docs)
# Labels are read from GitHub (gh, with GH_TOKEN and GITHUB_REPOSITORY set, as in Actions); without them only the
# titles count. Editing AppVersionBase sets the version outright (x.y.0 at that commit).
# Needs the full history (actions/checkout with fetch-depth: 0).
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

base_commit="$(git log -1 --format=%H -G'<AppVersionBase>' -- Directory.Build.props)"
base="$(sed -n 's:.*<AppVersionBase>\([0-9]*\.[0-9]*\)</AppVersionBase>.*:\1:p' Directory.Build.props | head -n 1)"
major="${base%%.*}"
minor="${base#*.}"
patch=0

# The labels of the PR a commit came from, comma-separated; empty when GitHub can't be asked.
labels_of() {
  if [ -z "${GH_TOKEN:-}" ] || [ -z "${GITHUB_REPOSITORY:-}" ] || ! command -v gh > /dev/null; then
    return 0
  fi
  gh api "repos/$GITHUB_REPOSITORY/commits/$1/pulls" --jq '[.[].labels[].name] | join(",")' 2> /dev/null || true
}

if [ -n "$base_commit" ]; then
  while IFS=' ' read -r sha subject; do
    labels=",$(labels_of "$sha"),"
    if [[ "$labels" == *",major,"* || "$subject" =~ ^[a-z]+(\([^\)]*\))?!: ]]; then
      major=$((major + 1)); minor=0; patch=0
    elif [[ "$labels" == *",minor,"* || "$subject" =~ ^feat(\([^\)]*\))?: ]]; then
      minor=$((minor + 1)); patch=0
    else
      patch=$((patch + 1))
    fi
  done < <(git log --reverse --first-parent --format='%H %s' "$base_commit..HEAD")
fi

echo "$major.$minor.$patch"
